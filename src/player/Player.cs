using Godot;
using System;

public class Player : KinematicBody, IBulletHittable
{
	[Export] private PackedScene _bulletScene;

	private Vector3 GravityAcceleration => new Vector3(0, -120f, 0);
	private Vector3 JumpVelocity => new Vector3(0, 40, 0);
	private float JumpHorizontalVelocityBoost => 0.25f;
	private const float MaxNaturalSpeed = 10;
	private const float MaxSprintSpeed = 20;
	private const float SensitivityMult = 4f;

	private float Sensitivity => 1.0f;

	private const int MaxHealth = 3;
	private int _health = MaxHealth;

	private float _speed = 100;
	private Vector3 _previousVelocity = Vector3.Zero;
	private Vector2 _mouseMotion = Vector2.Zero;

	private Camera _camera;
	private Label _debugLabel;
	private Area _meleeDetectionArea;
	private AnimatedSprite _doomPortrait;
	private CombatLogControl _logControl;

	public override void _Ready()
	{
		_camera = GetNode<Camera>("Camera");
		_debugLabel = GetNode<Label>("CanvasLayer/DebugContainer/PanelContainer/Label");
		_meleeDetectionArea = GetNode<Area>("Camera/MeleeTargetDetection");
		_doomPortrait = GetNode<AnimatedSprite>("CanvasLayer/CharacterPortrait/MarginContainer/WrappingControl/AnimatedSprite");
		_logControl = GetNode<CombatLogControl>("CanvasLayer/DebugContainer/CombatLogControl");
		_doomPortrait.Play("Idle");
	}

	public override void _PhysicsProcess(float delta)
	{
		if (_health > 0)
		{
			ApplyMovement(delta);
			RotateCamera(delta);
			
			MeleeAttack();
			ShootAttack();
		}

		UpdateDoomPortrait();		

		_mouseMotion = Vector2.Zero;
	}

	private void ApplyMovement(float delta)
	{
		Vector3 velocity = Vector3.Zero;

		Vector2 inputVector = new Vector2(Input.GetActionStrength("plr_right") - Input.GetActionStrength("plr_left"), Input.GetActionStrength("plr_back") - Input.GetActionStrength("plr_forward")).Normalized();
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

			float currentNaturalMaxSpeed = sprint ? MaxSprintSpeed : MaxNaturalSpeed;

			float currentSpeed = velocityXz.Length();

			if (currentSpeed > currentNaturalMaxSpeed)
			{
				if (IsOnFloor())
				{
					currentNaturalMaxSpeed = Mathf.MoveToward(currentSpeed, currentNaturalMaxSpeed, 20 * delta);
				}
				else
				{
					currentNaturalMaxSpeed = currentSpeed;
				}
			}

			velocityXz += globalizedInput * _speed * delta;
			velocityXz = velocityXz.LimitLength(currentNaturalMaxSpeed);

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
		_debugLabel.Text = $"Velocity: {velocity}\nSpeed: {velocity.Length()}\nSpeed xz: {velocity.xz().Length()}\nw: {Input.GetActionStrength("plr_forward")}";
	}

	private void RotateCamera(float delta)
	{
		Vector3 rotation = _camera.RotationDegrees;
		rotation += new Vector3(-_mouseMotion.y, -_mouseMotion.x, 0) * SensitivityMult * delta * Sensitivity;
		_camera.RotationDegrees = rotation;
	}

	private void MeleeAttack()
	{
		bool meleeAttack = Input.IsActionJustPressed("plr_melee");

		if (!meleeAttack) {
			return;
		}

		Godot.Collections.Array bodies = _meleeDetectionArea.GetOverlappingBodies();

		if (bodies.Count == 0) {
			return;
		} 

		// determine targetting heuristic based on closeness to crosshair and distance?
		IMeleeTargettable target = (IMeleeTargettable)bodies[0];

		target.Target(default);			
	}

	private void ShootAttack()
	{
		bool shootAttack = Input.IsActionJustPressed("plr_shoot");

		if (!shootAttack)
		{
			return;
		}

		Bullet bullet = _bulletScene.Instance<Bullet>();
		GetTree().Root.AddChild(bullet);
		bullet.SetGlobalPosition(this.GetGlobalPosition());

		bullet.Initialize(-_camera.GlobalTransform.basis.z * 20, PhysicsLayers3D.World | PhysicsLayers3D.Enemy);
	}

	private void UpdateDoomPortrait()
	{
		if (_doomPortrait.Animation != "Idle" && _doomPortrait.Frames.GetAnimationLoop(_doomPortrait.Animation) && _doomPortrait.Frame < _doomPortrait.Frames.GetFrameCount(_doomPortrait.Animation))
		{
			return;
		}

		if (_health <= 0)
		{
			_doomPortrait.Play("Dead");
		}
		else
		{
			_doomPortrait.Play("Idle");
		}
	}

	public override void _UnhandledInput(InputEvent ev)
	{
		if (ev is InputEventMouseMotion motion)
		{
			_mouseMotion = motion.Relative;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}


	public void Hit()
	{
		_health -= 1;
		GD.Print($"new health {_health}");
		_logControl.SetMsg($"The Player is now at {_health} health!");
	}

}
