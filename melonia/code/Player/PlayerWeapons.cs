using Sandbox;

public sealed class PlayerWeapons : Component
{
	[Property] public UnitInfo Info {get; set;}
	[Property] public SkinnedModelRenderer Gun {get; set;}
	[Property] public CameraMovement Camera {get; set;}

	protected override void OnStart()
	{
		if (Gun != null){
			Log.Info(Gun);
		}

	}

	protected override void OnUpdate(){
		if (Input.Down("use"))
		{
			Log.Info("BOOM");
			Gun.Set("fire" , true);
		}
	}
}
