using System.Diagnostics;
using Sandbox;

public sealed class CameraMovment : Component
{
	[Property] public PlayerMovment Player { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Head { get; set; }
	// [Property] public GameObject CameraObj { get; set; }
	[Property] public float Distance { get; set; } = 0f;
	[Property] public float MouseYSens { get; set; } = 0.1f;
	[Property] public float MouseXSens { get; set; } = 0.1f;

	public bool IsFirstPerson => Distance == 0f;

	private CameraComponent Camera;

	private ModelRenderer BodyRenderer;
	protected override void OnEnabled()
	{
		if ( IsProxy ) return;

		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;


		var eyeAngle = Head.Transform.Rotation.Angles();

		RotateEyes(eyeAngle);


		if (Camera is not null)
		{
			var camPos = Head.Transform.Position;
			if (!IsFirstPerson)
			{
				var camForward = eyeAngle.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * Distance) )
					.WithoutTags( "player", "trigger")
					.Run();

				if (camTrace.Hit)
				{
					camPos = camTrace.HitPosition + camTrace.Normal;						
				}
				else
				{
					camPos = camTrace.EndPosition;
				}

				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else 
			{
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngle.ToRotation();
		}
	}

	[Broadcast]
	void RotateEyes( Angles eyeAngle )
	{
		eyeAngle.pitch += Input.MouseDelta.y * MouseYSens;
		eyeAngle.yaw -= Input.MouseDelta.x * MouseXSens;
		eyeAngle.roll += 0f;
		eyeAngle.pitch = eyeAngle.pitch.Clamp( -89.9f, 89.9f );

		Head.Transform.Rotation = Rotation.From( eyeAngle );
	}
}
