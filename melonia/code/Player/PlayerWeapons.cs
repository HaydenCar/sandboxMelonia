using Sandbox;

public sealed class PlayerWeapons : Component
{
	[Property] public UnitInfo Info {get; set;}
	[Property] public SkinnedModelRenderer Gun {get; set;}
	[Property] public CameraMovement Camera {get; set;}

	public float BulletDamage {get; set;} = 2;

	public float AmmoInClip = 5;
	public float MaxAmmo = 5;

	protected override void OnStart()
	{
		if (Gun != null){
			Log.Info(Gun);
		}
	}

	protected override void OnUpdate(){
	}

	//Takes in a float to check if can shoot again
	public void ShootAnim(){
		Gun.Set("fire" , true);
		Log.Info("SHOOT");
	}

	//works if use any other animation?????????
	public void ReloadAnim(){
		Gun.Set("reload" , true);
		Log.Info("RELOADING");
	}
}
