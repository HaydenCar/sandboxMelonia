using Sandbox;

public sealed class CameraMovement : Component
{
	// Properties
    [Property] public PlayerMovement Player { get; set; }
    [Property] public GameObject Body { get; set; }
    [Property] public GameObject Head { get; set; }
    [Property] public float Distance { get; set; } = 0f;

    // Variables
    public bool IsFirstPerson => Distance == 0f; // Helpful but not required. You could always just check if Distance == 0f
    private CameraComponent Camera;
    private ModelRenderer BodyRenderer;
	private Vector3 CurrentOffset = Vector3.Zero;
    public Angles EyeAngles;
    [Property] public Vector3 EyePosition { get; set; } 

    protected override void OnAwake()
    {
        Camera = Components.Get<CameraComponent>();
        BodyRenderer = Body.Components.Get<ModelRenderer>();
    }

	protected override void OnUpdate()
	{
		// Rotate the head based on mouse movement
        var eyeAngles = Head.Transform.Rotation.Angles();
        eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
        eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
        eyeAngles.roll = 0f;
        eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f ); // So we don't break our necks
        Head.Transform.Rotation = eyeAngles.ToRotation();

		// Set the current offset
        var targetOffset = Vector3.Zero;
        if(Player.IsCrouching) targetOffset += Vector3.Down * 32f;
        if(Player.IsSliding) targetOffset += Vector3.Down * 32f;
        CurrentOffset = Vector3.Lerp(CurrentOffset, targetOffset, Time.Delta * 10f);

        // Set the position of the camera
        if(Camera is not null)
        {
            var camPos = Head.Transform.Position + CurrentOffset;
            if(!IsFirstPerson)
            {
                // Perform a trace backwards to see where we can safely place the camera
                var camForward = eyeAngles.ToRotation().Forward;
                var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance))
                    .WithoutTags("player", "trigger")
                    .Run();
                if(camTrace.Hit)
                {
                    camPos = camTrace.HitPosition + camTrace.Normal;
                }
                else
                {
                    camPos = camTrace.EndPosition;
                }

                // Show the body if we're not in first person
                BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
            }
            else
            {
                // Hide the body if we're in first person
                BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
            }

            // Set the position of the camera to our calculated position
            Camera.Transform.Position = camPos;
            Camera.Transform.Rotation = eyeAngles.ToRotation();
        }
        EyeAngles = eyeAngles;
	}
}
