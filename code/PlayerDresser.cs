using Sandbox;
using Sandbox.Citizen;
using System.Linq;

[Group( "Walker" )]
[Title( "Walker - Player Dresser" )]
public sealed class PlayerDresser : Component, Component.INetworkSpawn
{
	[Property]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	public void OnNetworkSpawn( Connection owner )
	{
		Log.Info("test");
		var clothing = new ClothingContainer();
		clothing.Deserialize( owner.GetUserData( "avatar" ) );
		clothing.Apply( BodyRenderer );
	}
}
