using Sandbox;

public enum GunType{
	// Environmental units or resources
	[Icon( "handshake" )]
	Hands,
	[Icon( "minimize" )]
	Pistol,
	[Icon( "drag_handle" )]
	Shotgun,
	[Icon( "dehaze" )]
	Assault_Rifle
}

public sealed class PlayerWeapons : Component
{
	[Property] public GunType TypeOfGun { get; set; }

	//GRAB CLASS REFERENCES
	[Property] public SkinnedModelRenderer Pistol {get; set;}
	[Property] public SkinnedModelRenderer Shotgun {get; set;}
	[Property] public SkinnedModelRenderer AssaultRifle {get; set;}
	[Property] public CameraMovement Camera {get; set;}
	[Property] public PlayerMovement Player {get; set;}

	
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
			Log.Info(Player.ActiveSlot);
			Pistol.Enabled = false;
		}
	}

	protected override void OnUpdate(){
	
	}

	public void ChooseWeapon(){
		if (Player.ActiveSlot == 0) Pistol.Enabled = false;
		if (Player.ActiveSlot == 1) {
			Pistol.Enabled = true;
			Pistol.Set("reloading", false);
			Pistol.Set("fire", false);
			Pistol.Set("deploy", true);
			Log.Info(Player.ActiveSlot);
		}
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
		if (!Pistol.Enabled) return; 
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
		_pistollastShot = -3.2f;
		ReloadAnim();
	}
}
