using Sandbox;

public enum UnitType{
	// Environmental units or resources
	[Icon( "check_box_outline_blank" )]
	None,
	[Icon( "nordic_walking" )]
	Player,
	[Icon( "filter_drama" )]
	Enemy
}

[Icon( "psychology" )]
public sealed class UnitInfo : Component
{
	[Property]
	public UnitType Team { get; set; }

	// Max health of the unit, clamps health from 0 to MaxHealth
	[Property]
	[Range( 0.1f, 10f, 0.1f )]
	public float MaxHealth { get; set; } = 10f;

	// How many HP are regenerated each second out of combat
	[Property]
	[Range( 0f, 2f, 0.1f )]
	public float HealthRegenAmount { get; set; } = 0.5f;

	// How many seconds out of combat before you start regenerating
	[Property]
	[Range( 1f, 5f, 1f )]
	public float HealthRegenTimer { get; set; } = 3f;

	public float Health { get; private set; }

	public bool Alive { get; private set; } = true;
	TimeSince _lastDamage;
	TimeUntil _nextHeal;
	protected override void OnUpdate()
	{
		if ( _lastDamage >= HealthRegenTimer && Health != MaxHealth && Alive )
		{
			if ( _nextHeal )
			{
				Damage( -HealthRegenAmount );
				_nextHeal = 1f;
				
			}
		}		
	}

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	// Damage the unit, clamped, heal if set to negative
	// <param name="damage"></param>
	public void Damage( float damage )
	{
		if ( !Alive ) Kill();

		Health = Math.Clamp( Health - damage, 0f, MaxHealth );

		if ( damage > 0 )
			_lastDamage = 0f;

		if ( Health <= 0 )
			Kill();
	}

	// Set the HP to 0 and Alive to false, then destroys it
	public void Kill()
	{
		Health = 0f;
		Alive = false;

		GameObject.Destroy();
	}
	
}
