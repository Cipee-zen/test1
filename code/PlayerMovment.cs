using System;
using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovment : Component
{
	// Movement Properties
	[Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 50f;
	[Property] public float Speed { get; set; } = 160f;
	[Property] public float RunSpeed { get; set; } = 290f;
	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 400f;
	[Property] public float RotateVelocity { get; set; } = 5f;
	[Property] public float TraceCrouchDistance { get; set; } = 30f;
	[Property] public float HeadStandUpSize { get; set; } = 17f;
	[Property] public float CrouchSpeedCam { get; set; } = 5f;
	// Object References
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public bool EnableDebug { get; set; } = false;
	// Member Variables
	public Vector3 WishVelocity = Vector3.Zero;
	public bool IsCrouching = false;
	public bool IsSprinting = false;
	public CharacterController characterController;
	public CitizenAnimationHelper animationHelper;

	private float defaultHeadUp;

	private bool forceCrouch = false;

	private Vector3 crouchingCamPos; 

	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
		defaultHeadUp = Head.Transform.LocalPosition[2];
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || Head is null)
			return;
		

		IsSprinting = Input.Down( "Run" );

		RotateBody(Head.Transform.Rotation.Yaw(), characterController.Velocity.Length);
		UpdateAnimations(IsCrouching);
		UpdateCrouch();

		if (EnableDebug) Debugging();
		if (forceCrouch) UpdateForceCrouch();
		if ( Input.Pressed( "Jump" ) ) Jump();
		if (IsCrouching) 
		{
			crouchingCamPos = new Vector3(0f, 0f, defaultHeadUp / 2);
		}
		else {
			crouchingCamPos = new Vector3(0f, 0f, defaultHeadUp);
		}

		Head.Transform.LocalPosition = Vector3.Lerp(Head.Transform.LocalPosition, crouchingCamPos,Time.Delta * CrouchSpeedCam);
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;
			
		BuildWishVelocity();
		Move();
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;
		var rot = Head.Transform.Rotation;
		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );
		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( IsCrouching ) WishVelocity *= CrouchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		var gravity = Scene.PhysicsWorld.Gravity;
		if (characterController.IsOnGround)
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( MaxForce ) );
			characterController.ApplyFriction( AirControl );
		}

		characterController.Move();

		if(!characterController.IsOnGround)
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	[Broadcast]
	void RotateBody(float yaw, float velocityLenght )
	{
		if (Body is null) return;

		var targetAngle = new Angles(0,yaw, 0).ToRotation();

		float rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

		if (rotateDifference > 1f || velocityLenght > 10f)
		{
			Body.Transform.Rotation	= Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * RotateVelocity);
		}
	}

	void Jump()
	{
		if (!characterController.IsOnGround) return;

		characterController.Punch(Vector3.Up * JumpForce);
		animationHelper?.TriggerJump();
	}

	[Broadcast]
	void UpdateAnimations(bool crouch)
	{
		if (animationHelper is null) return;

		animationHelper.WithWishVelocity( WishVelocity );
		animationHelper.WithVelocity(characterController.Velocity);
		animationHelper.AimAngle = Head.Transform.Rotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
		animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		// Log.Info(IsCrouching);
		animationHelper.DuckLevel = crouch ? 1f : 0f;
	}

	void RemoveCrouch()
	{
		IsCrouching = false;
		characterController.Height *= 2f;
		// Head.Transform.LocalPosition += (Vector3.Up * (defaultHeadUp / 2));
	}

	void EnableCrouch()
	{
		forceCrouch = false;
		IsCrouching = true;
		characterController.Height /= 2f;
		// Head.Transform.LocalPosition -= (Vector3.Up * (defaultHeadUp / 2));
	}

	bool VerifyCanStandUp()
	{
		var headPos = Head.Transform.Position;
		var crouchTrace = Scene.Trace.Ray( headPos, headPos + (Vector3.Up * TraceCrouchDistance) )
			.Size(HeadStandUpSize)
			.WithoutTags( "player", "trigger")
			.Run();
		return crouchTrace.Hit;
	}

	void UpdateCrouch()
	{
		if(characterController is null) return;
		var crouched = Input.Pressed("Crouch");

		if (crouched) forceCrouch = false; 
		if(crouched && !IsCrouching)
		{
			EnableCrouch();
		}

		if(Input.Released("Crouch") && IsCrouching)
		{
			if (VerifyCanStandUp()) 
			{
				forceCrouch = true;
				return;
			}

			RemoveCrouch();
		}

	}

	void UpdateForceCrouch()
	{
		if (!VerifyCanStandUp()) {
			forceCrouch = false;
			RemoveCrouch();
		}
	}

	void Debugging()
	{
		var headPos = Head.Transform.Position;
	
		Gizmo.Draw.Line(headPos, headPos + (Vector3.Up * TraceCrouchDistance));
	}
}
