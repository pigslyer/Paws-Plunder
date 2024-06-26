using Godot;
using System;

public class Player : KinematicBody
{
	private Vector3 GravityAcceleration => new Vector3(0, -98.1f, 0);
	private Vector3 JumpVelocity => new Vector3(0, 30, 0);
	private float JumpHorizontalVelocityBoost => 0.1f;
	private const float MaxNaturalSpeed = 10;
	private const float MaxSprintSpeed = 20;
	private const float SensitivityMult = 4f;

	private float Sensitivity => 1.0f;


	private float _speed = 100;
	private Vector3 _previousVelocity = Vector3.Zero;
	private Vector2 _mouseMotion = Vector2.Zero;

	private Camera _camera;
	private Label _debugLabel;

	public override void _Ready()
	{
		_camera = GetNode<Camera>("Camera");
		_debugLabel = GetNode<Label>("CanvasLayer/PanelContainer/Label");

		// move me
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(float delta)
	{
		ApplyMovement(delta);
		RotateCamera(delta);

		_mouseMotion = Vector2.Zero;
	}

	private void ApplyMovement(float delta)
	{
		Vector3 velocity = Vector3.Zero;

		Vector2 inputVector = Input.GetVector("plr_left", "plr_right", "plr_forward", "plr_back");
		bool jump = Input.IsActionJustPressed("plr_jump");
		bool sprint = Input.IsActionPressed("plr_sprint");

		// xz speed		
		{
			Basis cameraBasis = _camera.GlobalTransform.basis;

			Vector3 right = cameraBasis.x;
			right.y = 0;
			right = right.Normalized();

			Vector3 forward = cameraBasis.z;
			forward.y = 0;
			forward = forward.Normalized();

			Vector3 globalizedInput = inputVector.y * forward + inputVector.x * right;

			Vector3 velocityXz = _previousVelocity.xz();
			float maxXzSpeed = Math.Max(sprint ? MaxSprintSpeed : MaxNaturalSpeed, velocityXz.Length());

			velocityXz += globalizedInput * _speed * delta;
			velocityXz = velocityXz.LimitLength(maxXzSpeed);

			velocity += velocityXz;
		}

		// gravity
		{
			velocity += new Vector3(0, _previousVelocity.y, 0);
			velocity += GravityAcceleration * delta;
		}

		// jumping
		{

			if (IsOnFloor() && jump)
			{
				velocity += JumpVelocity;

				velocity += velocity.xz() * JumpHorizontalVelocityBoost;
			}
		}

		// deaccel
		{
			if (IsOnFloor() && inputVector.Length() < 0.01f)
			{
				velocity += -velocity.xz() + velocity.xz() * 0.8f;
			}
		}

		_previousVelocity = MoveAndSlide(velocity, Vector3.Up, true);
		_debugLabel.Text = $"Velocity: {velocity}\nSpeed: {velocity.Length()}\nSpeed xz: {velocity.xz().Length()}";
	}

	private void RotateCamera(float delta)
	{
		Vector3 rotation = _camera.RotationDegrees;
		rotation += new Vector3(-_mouseMotion.y, -_mouseMotion.x, 0) * SensitivityMult * delta * Sensitivity;
		_camera.RotationDegrees = rotation;
	}

	public override void _UnhandledInput(InputEvent ev)
	{
		if (ev is InputEventMouseMotion motion)
		{
			_mouseMotion = motion.Relative;
		}
	}
}
