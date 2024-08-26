using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.Citizen;


public sealed class PlayerMovement : Component
{
	// Variables
	[Property] public float Health { get; set; } = 100f;
	[Property] public float MaxHealth { get; set; } = 100f;
	public TimeSince TimeAlive { get; set; } = 0f;

	[Property]
	public List<string> Inventory { get; set; } = new List<string>{
		"Weapon_Pistol"
	};

	public int ActiveSlot = 0;
	public int Slots => 9;

	private int JumpCount = 0;

	  // Movement Properties
    [Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 50f;
	[Property] public float Speed { get; set; } = 160f;
	[Property] public float RunSpeed { get; set; } = 290f;
	[Property] public float WalkSpeed { get; set; } = 90f;
	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float SlideSpeed { get; set; } = 150f;
	[Property] public float JumpForce { get; set; } = 400f;

	// How much damage your punch deals
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 5f, 0.1f )]
	public float PunchStrength { get; set; } = 1f;

	// How many seconds before you can punch again
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2f, 0.1f )]
	public float PunchCooldown { get; set; } = 0.5f;

	// How far away you can punch in Hammer Units
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 5f )]
	public float PunchRange { get; set; } = 50f;


	 // Object References
    [Property] public GameObject Head { get; set; }
    [Property] public GameObject Body { get; set; }
	[Property] public SkinnedModelRenderer BodyTarget { get; set; }
	// Member Variables
	
	public Vector3 WishVelocity = Vector3.Zero;
	public bool IsCrouching = false;
	public bool IsSprinting = false;
	public bool IsRolling = false;
	public bool IsSliding = false;
	private CharacterController characterController;
	private CitizenAnimationHelper animationHelper;

	private int SlideTimer = 0;
	private TimeSince _lastPunch;

	//FOR TRACING ATTACKS
	public Angles EyeAngles { get; set; }
	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( Head.Transform.LocalPosition );

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		var draw = Gizmo.Draw;

		draw.LineSphere( Head.Transform.LocalPosition, 10f );
		draw.LineCylinder(Head.Transform.LocalPosition, Head.Transform.LocalPosition + Head.Transform.LocalRotation.Forward * PunchRange, 5f, 5f, 10 );
	}


	protected override void OnStart(){
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();

		// FOR SOME REASON GAME WANNA BE NAKED
		if (Components.TryGet<SkinnedModelRenderer>(out var model)) {
    		var clothing = ClothingContainer.CreateFromLocalUser();
    		clothing.Apply(model);
			Log.Info("DO I WORK?");
		}
	}


	protected override void OnUpdate(){
		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -80f, 80f ) );
		// Set movement states
        UpdateCrouch();
		UpdateSlide();
		//UpdateRoll();		
        UpdateSprint();
		if(Input.Pressed("Jump")) Jump();
		if ( Input.Pressed( "attack1" ) && _lastPunch >= PunchCooldown ) Punch();
		DrawGizmos();
		GetActiveSlot();
		RotateBody();
		UpdateAnimation();

	}

	protected override void OnFixedUpdate(){
		BuildWishVelocity();
        Move();
	}

	void BuildWishVelocity(){
        WishVelocity = 0;

        var rot = Head.Transform.Rotation;
		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

        WishVelocity = WishVelocity.WithZ( 0 );

        if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

        if(IsCrouching) WishVelocity *= CrouchSpeed; // Crouching takes presedence over sliding
		else if(IsSliding) WishVelocity *= SlideSpeed; // Sliding takes presedence over sprinting
        else if(IsSprinting) WishVelocity *= RunSpeed; // Sprinting takes presedence over walking
        else WishVelocity *= Speed;
    }

	 void Move(){
		// Get gravity from our scene
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( characterController.IsOnGround ){
			// Apply Friction/Acceleration
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else{
			// Apply Air Control / Gravity
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( MaxForce ) );
			characterController.ApplyFriction( AirControl );
			}

		// Move the character controller
		characterController.Move();

		// Apply the second half of gravity after movement
		if ( !characterController.IsOnGround ){
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			}
	}

	void RotateBody(){
        if(Body is null) return;

        var targetAngle = new Angles(0, Head.Transform.Rotation.Yaw(), 0).ToRotation();
        float rotateDifference = Body.Transform.Rotation.Distance(targetAngle);

        // Lerp body rotation if we're moving or rotating far enough
        if(rotateDifference > 10f || characterController.Velocity.Length > 10f){
            Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 10f);
        }
    }

	void Jump(){
		if (!characterController.IsOnGround){
			if(JumpCount == 1) {
			
			characterController.Punch(Vector3.Up * JumpForce / 2);
			animationHelper?.TriggerJump();
			JumpCount = 0;
			}
			Log.Info("Jump Count: " + JumpCount);
			return;
		}
		
		if(IsCrouching) return;
		JumpCount = 0;
        characterController.Punch(Vector3.Up * JumpForce);
		JumpCount++;
        animationHelper?.TriggerJump(); // Trigger our jump animation if we have one
		Log.Info("Jump Count: " + JumpCount);
    }

	  void UpdateAnimation(){
        if(animationHelper is null) return;

        animationHelper.WithWishVelocity(WishVelocity);
        animationHelper.WithVelocity(characterController.Velocity);
        animationHelper.AimAngle = Head.Transform.Rotation;
        animationHelper.IsGrounded = characterController.IsOnGround;
        animationHelper.WithLook(Head.Transform.Rotation.Forward, 1, 0.75f, 0.5f);
        animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
        animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
		
		//NOT IN USE RIGHT NOW TBA
		//if(IsRolling){
		//	animationHelper.Target.Set( "roll_forward", true );
		//	animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.Roll;
		//} else animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.None;

		if(IsSliding){
			animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.Slide;
		} else animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.None;
		
    }

	void UpdateCrouch(){
        if(characterController is null) return;
		if(!characterController.IsOnGround) return;

        if(Input.Pressed("Crouch") && !IsCrouching){
            IsCrouching = true;
            characterController.Height /= 1.5f; // Reduce the height of our character controller
        }

        if(Input.Released("Crouch") && IsCrouching){
			var ctrace = characterController.TraceDirection( Vector3.Up * 32);

			if (ctrace.Hit) return;

            IsCrouching = false;
            characterController.Height *= 1.5f; // Return the height of our character controller to normal
        }
    }

	//NOT IN USE RIGHT NOW TBA ALSO BAD CODE FIX
	//void UpdateRoll(){
	//	if(!characterController.IsOnGround) return;
	//	if(IsCrouching) return;
//
    //    if(Input.Pressed("attack1"))
    //    {
	//		Log.Info("DO A BARREL ROLL");
	//		animationHelper.Target.Set( "roll_forward", true );
	//		IsRolling = true;
    //    } else{
	//		animationHelper.Target.Set( "roll_forward", true );
	//		IsRolling = false;
	//	  } 
    //}

	void UpdateSlide(){
		if(!characterController.IsOnGround || IsCrouching) return;

        if (Input.Pressed("attack2") && !IsSliding){
			Log.Info("Slide in, slide in, Would you ride? Baby, would you ride with me?");
			IsSliding = true;
			characterController.Height /= 2f;	
        } 

		if (Input.Released("attack2") && IsSliding){
			var strace = characterController.TraceDirection( Vector3.Up * 32);

			if (strace.Hit) {
				Input.Released("attack2");
				return;
			}

			IsSliding = false;
			characterController.Height *= 2f;
		}

		SlideFix();
    }

	// Stops player from unsliding when under objects
	void SlideFix(){
		if (IsSliding){
			var hi = characterController.TraceDirection( Vector3.Up * 32);
			SlideTimer++;
			if (SlideTimer > 200 && !hi.Hit)
			{
				IsSliding = false;
				characterController.Height *= 2f;
			}
		} else SlideTimer = 0;
	}

	void GetActiveSlot(){
		if (Input.MouseWheel.y != 0){
			ActiveSlot = (ActiveSlot + Math.Sign(Input.MouseWheel.y)) % Slots;
		}
	}

	void UpdateSprint(){
		IsSprinting = Input.Down("Run");
	}

	public void Punch()
	{

		if ( animationHelper != null )
		{
			animationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
			animationHelper.Target.Set( "b_attack", true );
		}

		var punchTrace = Scene.Trace
			.FromTo( EyeWorldPosition, EyeWorldPosition + EyeAngles.Forward * PunchRange )
			.Size( 20f )
			.WithoutTags( "player")
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( punchTrace.Hit ){
			Log.Info("WHY");
			if ( punchTrace.GameObject.Components.TryGet<UnitInfo>( out var unitInfo ) ){
				unitInfo.Damage( PunchStrength ); 
				Log.Info(unitInfo.Health);
			}
		}
		_lastPunch = 0f;
	}
}
