using Sandbox;

public sealed class MyPlayer : Component
{
	[Property] public GameObject Camera { get; set; }
	[Property] public Vector3 Pos { get; set; }

	private bool OwnerChech = false;
	protected override void OnUpdate()
	{
		if (Camera is null) return;
		// Pos = Transform.Position;
		if (Network.IsOwner && !OwnerChech)
		{
			OwnerChech = true;
			Camera.Enabled = true;
		}
		// Log.Info(Network.IsOwner);
	}


}