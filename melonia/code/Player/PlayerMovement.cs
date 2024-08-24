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
	[Property] public float JumpForce { get; set; } = 400f;

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
		// Set our sprinting and crouching states
        UpdateCrouch();
		UpdateSlide();
		UpdateRoll();
		
        IsSprinting = Input.Down("Run");
		if(Input.Pressed("Jump")) Jump();

		if (Input.MouseWheel.y != 0){
			ActiveSlot = (ActiveSlot + Math.Sign(Input.MouseWheel.y)) % Slots;
		}

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

        if(IsCrouching) WishVelocity *= CrouchSpeed; // Crouching takes presedence over sprinting
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
		
		if(IsRolling){
			animationHelper.Target.Set( "roll_forward", true );
			animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.Roll;
		} else animationHelper.SpecialMove = CitizenAnimationHelper.SpecialMoveStyle.None;

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
            IsCrouching = false;
            characterController.Height *= 1.5f; // Return the height of our character controller to normal
        }
    }

	void UpdateRoll(){
		if(!characterController.IsOnGround) return;
		if(IsCrouching) return;

        if(Input.Pressed("attack1"))
        {
			Log.Info("DO A BARREL ROLL");
			animationHelper.Target.Set( "roll_forward", true );
			IsRolling = true;
        } else{
			animationHelper.Target.Set( "roll_forward", true );
			IsRolling = false;
		  } 
    }

	void UpdateSlide(){
		if(!characterController.IsOnGround) return;
		if(IsCrouching) return;

        if(Input.Pressed("attack2")){
			Log.Info("Slide in, slide in, Would you ride? Baby, would you ride with me?");
			IsSliding = true;
			characterController.Height /= 2f;

			
        } else{
			IsSliding = false;
		  } 

		if (Input.Released("attack2") && !IsSliding){
			characterController.Height *= 2f;
		}
    }

}
