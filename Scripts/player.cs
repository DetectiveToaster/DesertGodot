using Godot;

public partial class Player : CharacterBody3D
{
    private const float SPEED = 8f;
    private const float JUMP_VELOCITY = 4.5f;
    private float _footstepTimer = 0f;
    private float _footstepInterval = 0.8f;

    private float _gravityScale = 1.0f;
    private bool _isSurfing = false;
    private float SURF_SPEED_THRESHOLD = 5.0f;

    private Node3D _neck;
    private Camera3D _camera;
    private AudioStreamPlayer3D _footstepSound;
    private AudioStreamPlayer3D _humming;

    public override void _Ready()
    {
        _neck = GetNode<Node3D>("Neck");
        _camera = _neck.GetNode<Camera3D>("Camera");
        _footstepSound = GetNode<AudioStreamPlayer3D>("Steps");
        _humming = GetNode<AudioStreamPlayer3D>("Humming");
    }

    private bool IsMovingDownhill()
    {
        if (IsOnFloor())
        {
            Vector3 floorNormal = GetFloorNormal();
            double floorAngle = Mathf.RadToDeg(Mathf.Acos(floorNormal.Dot(Vector3.Up)));
            double minDownhillAngle = 15.0;
            if (floorAngle >= minDownhillAngle)
            {
                Vector3 slopeDirection = floorNormal.Cross(Vector3.Right);
                Vector3 movementDirection = Velocity.Normalized();
                double angleBetween = Mathf.RadToDeg(Mathf.Acos(slopeDirection.Dot(movementDirection)));
                if (angleBetween < 90.0)
                    return true;
            }
        }
        return false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            Input.SetMouseMode(Input.MouseMode.Captured);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.SetMouseMode(Input.MouseMode.Visible);
        }

        if (Input.GetMouseMode() == Input.MouseMode.Captured && @event is InputEventMouseMotion motion)
        {
            _neck.RotateY(-motion.Relative.X * 0.005f);
            _camera.RotateX(-motion.Relative.Y * 0.005f);
            Vector3 camRot = _camera.Rotation;
            camRot.X = Mathf.Clamp(camRot.X, Mathf.DegToRad(-60), Mathf.DegToRad(80));
            _camera.Rotation = camRot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsOnFloor())
            Velocity += GetGravity() * (float)delta;

        if (Input.IsActionJustPressed("hum"))
            _humming.Play();
        if (!_humming.Playing && Input.IsActionJustReleased("hum"))
            _humming.Stop();

        HandleNormalMovement((float)delta);

        MoveAndSlide();
    }

    private void HandleNormalMovement(float delta)
    {
        Vector2 inputDir = Input.GetVector("left", "right", "forward", "back");
        Vector3 direction = (_neck.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            Velocity = new Vector3(direction.X * SPEED, Velocity.Y, direction.Z * SPEED);

            _footstepTimer += delta;
            if (_footstepTimer >= _footstepInterval)
            {
                _footstepSound.Play();
                _footstepTimer = 0f;
            }
        }
        else
        {
            float x = Mathf.MoveToward(Velocity.X, 0, SPEED);
            float z = Mathf.MoveToward(Velocity.Z, 0, SPEED);
            Velocity = new Vector3(x, Velocity.Y, z);
            _footstepTimer = 0f;
        }
    }

    private void HandleSurfing(float delta)
    {
        Vector2 strafeInput = Input.GetVector("strafe left", "strafe right", "accelerate", "decelerate");
        Vector3 strafeDirection = _neck.Transform.Basis.X * strafeInput.X;
        Velocity = new Vector3(strafeDirection.X * SPEED * 1.5f, Velocity.Y, strafeDirection.Z * SPEED * 1.5f);
        Velocity += GetGravity() * _gravityScale * delta;
    }

    private void EnterSurfMode()
    {
        _isSurfing = true;
        _gravityScale = 0.5f;
    }

    private void ExitSurfMode()
    {
        _isSurfing = false;
        _gravityScale = 1.0f;
    }
}
