using Godot;

public class Player : KinematicBody, IBulletHittable
{
	[Export] private PackedScene _bulletScene;

	private Vector3 GravityAcceleration => new Vector3(0, -115f, 0);
	private Vector3 JumpVelocity => new Vector3(0, 45, 0);
	private float JumpHorizontalVelocityBoost => 0.25f;
	private float JumpVerticalFromX0zVelocityBoost => 0.01f;
	private const float MaxNaturalSpeed = 15;
	private const float MaxSprintSpeed = 20;
	private const float SensitivityMult = 4f;
	private const float InvulPeriod = 2.0f;

	private float Sensitivity => 1.0f;

	private const int MaxHealth = 3;
	private int _health = MaxHealth;

	private float _speed = 100;
	private Vector3 _previousVelocity = Vector3.Zero;
	private Vector2 _mouseMotion = Vector2.Zero;

	public Vector3 Velocity => _previousVelocity;
	public bool JustFired { get; private set;} = false;

	private Camera _camera;
	private Label _debugLabel;
	private Area _meleeDetectionArea;
	private DoomPortrait _doomPortrait;
	private CombatLogControl _logControl;
	private HealthContainer _healthContainer;

	private DeathInfo _deathInfo = null;

	private CustomTimer _invulTimer;

	public override void _Ready()
	{
		_camera = GetNode<Camera>("Camera");
		_debugLabel = GetNode<Label>("CanvasLayer/DebugContainer/PanelContainer/Label");
		_meleeDetectionArea = GetNode<Area>("Camera/MeleeTargetDetection");
		_doomPortrait = GetNode<DoomPortrait>("CanvasLayer/DoomPortrait");
		_logControl = GetNode<CombatLogControl>("CanvasLayer/DebugContainer/CombatLogControl");
		_healthContainer = GetNode<HealthContainer>("CanvasLayer/HealthContainer");
		
		_doomPortrait.SetAnimation(DoomPortraitType.Idle);
	}

	public override void _PhysicsProcess(float delta)
	{
		JustFired = false;

		bool isAlive = _health > 0;

		ApplyMovement(delta, hasControls: isAlive);

		if (isAlive)
		{
			MouseRotateCamera(delta);
			
			MeleeAttack();
			ShootAttack();
		}
		else if (_deathInfo != null)
		{
			RotateCameraTowards(_deathInfo.GetKillerPosition());
		}

		RestartOnRequest();

		_mouseMotion = Vector2.Zero;
	}

	private void ApplyMovement(float delta, bool hasControls)
	{
		Vector3 velocity = Vector3.Zero;
		Vector3 snap = Vector3.Down;

		Vector2 inputVector = Vector2.Zero;
		bool jump = false; 
		bool sprint = false;

		if (hasControls)
		{
			inputVector = new Vector2(Input.GetActionStrength("plr_right") - Input.GetActionStrength("plr_left"), Input.GetActionStrength("plr_back") - Input.GetActionStrength("plr_forward")).Normalized();
			jump = Input.IsActionPressed("plr_jump");
			sprint = Input.IsActionPressed("plr_sprint");
		}

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
			Vector3 velocityXz = _previousVelocity.x0z();

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
				Vector3 jumpVelocity = JumpVelocity;

				if (velocity.x0z().Length() > MaxNaturalSpeed)
				{
					//jumpVelocity += jumpVelocity * (velocity.x0z().Length() - MaxNaturalSpeed) * JumpVerticalFromX0zVelocityBoost;
				}				

				velocity += jumpVelocity;
				velocity += velocity.x0z() * JumpHorizontalVelocityBoost;
				snap = Vector3.Zero;
			}
		}

		// deaccel
		{
			if (IsOnFloor() && inputVector.Length() < 0.01f)
			{
				velocity -= velocity.x0z();
				//velocity += -velocity.x0z() + velocity.x0z() * 0.8f;
			}
		}

		_previousVelocity = MoveAndSlideWithSnap(velocity, snap, Vector3.Up, true);
		_debugLabel.Text = $"Velocity: {velocity}\nSpeed: {velocity.Length()}\nSpeed xz: {velocity.x0z().Length()}";
	}

	private void MouseRotateCamera(float delta)
	{
		Vector3 rotation = _camera.RotationDegrees;
		rotation += new Vector3(-_mouseMotion.y, -_mouseMotion.x, 0) * SensitivityMult * delta * Sensitivity;

		rotation.x = Mathf.Clamp(rotation.x, -85.0f, 85.0f);
		_camera.RotationDegrees = rotation;
	}

	private void RotateCameraTowards(Vector3 targetPosition)
	{
		_camera.LookAt(targetPosition, Vector3.Up);
	}

	private void MeleeAttack()
	{
		bool meleeAttack = Input.IsActionJustPressed("plr_melee");

		if (!meleeAttack) 
		{
			return;
		}

		JustFired = true;
		Godot.Collections.Array bodies = _meleeDetectionArea.GetOverlappingBodies();

		if (bodies.Count == 0) 
		{
			return;
		} 

		// determine targetting heuristic based on closeness to crosshair and distance?
		IMeleeTargettable target = bodies[0] as IMeleeTargettable;

		if (target == null)
		{
			return;
		}

		target.Target(default);			
	}

	private void ShootAttack()
	{
		bool shootAttack = Input.IsActionJustPressed("plr_shoot");

		if (!shootAttack)
		{
			return;
		}
		
		JustFired = true;

		Bullet bullet = _bulletScene.Instance<Bullet>();
		GetTree().Root.AddChild(bullet);
		bullet.GlobalTranslation = GlobalTranslation;

		bullet.Initialize(this, -_camera.GlobalTransform.basis.z * 20, PhysicsLayers3D.World | PhysicsLayers3D.Enemy);
	}

	private void RestartOnRequest()
	{
		if (Input.IsActionJustPressed("plr_restart"))
		{
			GetTree().ReloadCurrentScene();
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


	void IBulletHittable.Hit(BulletHitInfo info)
	{
		if (_health <= 0)
		{
			return;
		}

		if (_invulTimer != null)
		{
			return;
		}

		_health -= 1;
		GD.Print($"new health {_health}");
		_logControl.SetMsg($"The Player is now at {_health} health!");

		_healthContainer.SetHealth(_health);

		if (_health > 0)
		{
			_doomPortrait.SetAnimation(DoomPortraitType.Pain);
			_invulTimer = CustomTimer.Start(this, InvulPeriod);

			_invulTimer.Timeout += () => {
				_invulTimer = null;
				_doomPortrait.SetAnimation(DoomPortraitType.Idle);
			};
		}
		else
		{
			_doomPortrait.SetAnimation(DoomPortraitType.Death);

			_deathInfo = new DeathInfo(info.Source);
		}
	}

	private class DeathInfo
	{
		private Spatial _killer;
		private Vector3 _lastKillerPosition;

		public DeathInfo(Spatial killer)
		{
			(_killer, _lastKillerPosition) = (killer, killer.GlobalTranslation);
		}

		public Vector3 GetKillerPosition()
		{
			if (IsInstanceValid(_killer))
			{
				_lastKillerPosition = _killer.GlobalTranslation;
			}

			return _lastKillerPosition;
		}
	}
}
