using System.Diagnostics;
using Sandbox;
using Sandbox.Citizen;

public sealed class MyPlayer : Component
{
	/// <summary>
	/// Initial core vars
	/// </summary>
	[Property]
	[Category("Components")]
	public GameObject Camera{get; set;}

	[Property]
	[Category("Components")]
	public CharacterController Controller{get; set;}

	[Property]
	[Category("Components")]
	public CitizenAnimationHelper Animator{get; set;}

	/// <summary>
	/// movement vars
	/// </summary> 
	[Property]
	[Category("Stats")]
	[Range(0f, 400f, 1f)]
	public float WalkSpeed{get; set;} = 120f;

	[Property]
	[Category("Stats")]
	[Range(0f, 800f, 1f)]
	public float RunSpeed{get; set;} = 250f;

	[Property]
	[Category("Stats")]
	[Range(0f, 1000f, 10f)]
	public float JumpStrength{get; set;} = 400f;

	public Angles EyeAngles{get; set;}

	protected override void OnUpdate()
	{
		EyeAngles += Input.AnalogLook;
		Transform.Rotation = Rotation.FromYaw(EyeAngles.yaw);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (Controller == null) return;

		var wishSpeed = Input.Down("Run") ? RunSpeed : WalkSpeed;
		var wishVelocity = Input.AnalogMove * wishSpeed * Transform.Rotation;

		Controller.Accelerate(wishVelocity);

		if (Controller.IsOnGround)
		{
			Controller.ApplyFriction( 5f, 10f );

			if (Input.Pressed("Jump")){
				Controller.Punch(Vector3.Up * JumpStrength);
			}

			if (Animator != null){
				Animator.TriggerJump();
			}
		}
		else 
		{
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		Controller.Move();

		if (Animator != null){
			Animator.IsGrounded = Controller.IsOnGround;
			Animator.WithVelocity(Controller.Velocity);
		}
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}
}
