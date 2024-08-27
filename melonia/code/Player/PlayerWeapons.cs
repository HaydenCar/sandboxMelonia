using Sandbox;

public sealed class PlayerWeapons : Component
{
	//GRAB CLASS REFERENCES
	[Property] public SkinnedModelRenderer Pistol {get; set;}
	[Property] public CameraMovement Camera {get; set;}
	public PlayerMovement Player {get; set;}

	
	//CREATE VARIABLES
	public float BulletDamage {get; set;} = 2;
	public int AmmoInClip = 5;
	public int MaxAmmoInClip = 5;
	public int MaxAmmo = 20;
	private TimeSince _pistollastShot;
	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( Camera.EyePosition );


	protected override void OnStart()
	{
		if (Pistol != null){
			Log.Info(Pistol);
		}
	}

	protected override void OnUpdate(){
	}

	public void ShootAnim(){
		Pistol.Set("fire" , true);
		Log.Info("SHOOT");
	}

	public async void ReloadAnim(){
		await Task.DelaySeconds(0.1f);
		Pistol.Set("reload" , true);
		Log.Info("RELOADING");
	}

	//HEAVYLIFTING
	public void Shoot(){
		if ( _pistollastShot < 0.3f ) return;

		ShootAnim();

		var shootTrace = Scene.Trace
			.FromTo( EyeWorldPosition, EyeWorldPosition + Camera.EyeAngles.Forward * 1000f )
			.Size( 5f )
			.WithoutTags( "player")
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( shootTrace.Hit ){
			if ( shootTrace.GameObject.Components.TryGet<UnitInfo>( out var unitInfo ) ){
				unitInfo.Damage( BulletDamage ); 
				Log.Info("Health: " + unitInfo.Health);
				_pistollastShot = 0f;
			}
		}

		AmmoInClip -= 1;
	}

	public void CheckWeapon(){
		if(AmmoInClip == 0) Reload();
		Shoot();
	}

	public void Reload(){
		AmmoInClip = MaxAmmoInClip;
		_pistollastShot = -2f;
		ReloadAnim();
	}
}
