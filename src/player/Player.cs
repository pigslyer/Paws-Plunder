using System;
using System.Linq;
using Godot;

public class Player : KinematicBody, IBulletHittable, IDeathPlaneEnterable
{
	private enum GunTypes
	{
		Single,
		Quad,
	}

	[Export] private bool _initializeOnStartup = false;
	[Export] private PackedScene _bulletScene;

	private const float GravityConst = -120f;

	private Vector3 GravityAcceleration = new Vector3(0, GravityConst, 0);

	private Vector3 JumpVelocity => new Vector3(0, 40, 0);
	private float JumpHorizontalVelocityBoost => 0.25f;
	private float JumpVerticalFromX0zVelocityBoost => 0.01f;
	private const float MaxNaturalSpeed = 15;
	private const float MaxSprintSpeed = 20;
	private const float SensitivityMult = 4f;
	private const float InvulPeriod = 2.0f;
	private const float Acceleration = 100;
	private const float EnamoredByTreasureTime = 2.0f;
	private const float SingleBulletVelocity = 200.0f;
	private const float QuadBulletVelocity = 200.0f;

	private float Sensitivity => 1.0f;

	private const int MaxHealth = 3;
	private int _health = MaxHealth;

	private GunTypes _currentGun = GunTypes.Single;
	private int _remainingAmmo = 1;

	private Vector3 _previousVelocity = Vector3.Zero;
	private Vector2 _mouseMotion = Vector2.Zero;

	public Vector3 Velocity => _previousVelocity;
	public bool JustFired { get; private set;} = false;
	public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;
	private Spatial _head;
	private Camera _camera;
	private Sprite3D _gun;
	private Label _debugLabel;
	private TextureRect _crosshair;
	private Area _meleeDetectionArea;
	private Area _pickupDetectionArea;
	private DoomPortrait _doomPortrait;
	private CombatLogControl _logControl;
	private HealthContainer _healthContainer;
	private Spatial _centerOfMassNode;

	private DeathInfo _deathInfo = null;

	private CustomTimer _invulTimer;
	private CustomTimer _enamoredTimer;

	public override void _Ready()
	{
		_head = GetNode<Spatial>("Head");
		_camera = GetNode<Camera>("%Camera");
		_gun = GetNode<Sprite3D>("%Camera/Gun");
		_debugLabel = GetNode<Label>("CanvasLayer/DebugContainer/PanelContainer/Label");
		_meleeDetectionArea = GetNode<Area>("%Camera/MeleeTargetDetection");
		_pickupDetectionArea = GetNode<Area>("PickupArea");
		_doomPortrait = GetNode<DoomPortrait>("CanvasLayer/DoomPortrait");
		_logControl = GetNode<CombatLogControl>("CanvasLayer/DebugContainer/CombatLogControl");
		_healthContainer = GetNode<HealthContainer>("CanvasLayer/HealthContainer");
		_centerOfMassNode = GetNode<Spatial>("CenterOfMass");

		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<CanvasLayer>("CanvasLayer").Visible = false;
		GetNode<Sprite3D>("%Camera/Claw").Visible = false;
		GetNode<Sprite3D>("%Camera/Gun").Visible = false;

		if (_initializeOnStartup)
		{
			Initialize();
		}
	}

	public void Initialize()
	{
		GetNode<CanvasLayer>("CanvasLayer").Visible = true;
		GetNode<Sprite3D>("%Camera/Claw").Visible = true;
		GetNode<Sprite3D>("%Camera/Gun").Visible = true;
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
		
			PickUpItems();
		}
		else if (_deathInfo != null)
		{
			RotateCameraTowards(_deathInfo.GetKillerPosition());
		}

		RestartOnRequest();
		KillIfBelowWorld();

		_mouseMotion = Vector2.Zero;
	}

	public void ToggleGravity(bool enable)
	{
		GravityAcceleration.y = enable ? GravityConst : 0;	
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

			velocityXz += globalizedInput * Acceleration * delta;
			velocityXz = velocityXz.LimitLength(currentNaturalMaxSpeed);

			velocity += velocityXz;
		}
		// camera offset
		Vector3 headOffset = Vector3.Zero;
		Vector3 headRotation = new Vector3(
			_head.RotationDegrees.x,
			_head.RotationDegrees.y,
			Mathf.Clamp(_head.Rotation.z, -0.05f, 0.05f));
		{
			float bobOffset = Mathf.Sin(delta) * 1f;
			headOffset.y = bobOffset;
		}
		{
			float swayRad = Mathf.Sign(inputVector.x) * -0.05f;
			headRotation.z = Mathf.LerpAngle(headRotation.z, swayRad, 0.05f);
		}
		_head.Translation = headOffset;
		_head.Rotation = headRotation;
		GD.Print(_head.RotationDegrees.z);

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
		if (_remainingAmmo <= 0)
		{
			return;
		}

		bool shootAttack = Input.IsActionJustPressed("plr_shoot");

		if (!shootAttack)
		{
			return;
		}

		_remainingAmmo -= 1;

		JustFired = true;

		Vector3 shotDirection = -_camera.GlobalTransform.basis.z;

		// include muzzle flash?
		if (_currentGun == GunTypes.Single)
		{
			FireBullet(shotDirection * SingleBulletVelocity);
		}
		else if (_currentGun == GunTypes.Quad)
		{
			foreach (Vector3 bulletVelocity in Globals.CalculateShotgunDirections(shotDirection, Mathf.Deg2Rad(30), 4, QuadBulletVelocity))
			{
				FireBullet(bulletVelocity);
			}
		}

		if (_remainingAmmo == 0)
		{
			// include actual animation here
			_gun.Visible = false;
		}
	}

	private void FireBullet(Vector3 velocity)
	{
		Bullet bullet = _bulletScene.Instance<Bullet>();
		GetParent().AddChild(bullet);
		bullet.GlobalTranslation = CenterOfMass;

		bullet.Initialize(this, velocity, PhysicsLayers3D.World | PhysicsLayers3D.Enemy);
		GlobalSignals.GetInstance().EmitSignal(nameof(GlobalSignals.AddToPlayerScore), 100);
	}

	private void PickUpItems()
	{
		Godot.Collections.Array pickupables = _pickupDetectionArea.GetOverlappingBodies();

		void PickupItem(Item item)
		{
			// include add score
			_logControl.SetMsg($"Picked up a {item.DisplayName}!");
			item.QueueFree();
		}

		foreach (Item item in pickupables.OfType<Item>())
		{
			switch (item.ItemName)
			{
				case "GunnerGun":
				{
					if (_remainingAmmo > 0)
					{
						continue;
					}

					_remainingAmmo = 1;
					_currentGun = GunTypes.Quad;
					_gun.Visible = true;
					_gun.Frame = 1;

					PickupItem(item);
				}
				break;

				case "Treasure":
				{
					_doomPortrait.SetAnimation(DoomPortraitType.Treasure);

					if (_enamoredTimer != null)
					{
						_enamoredTimer.Start(EnamoredByTreasureTime);
					}
					else
					{
						_enamoredTimer = CustomTimer.Start(this, EnamoredByTreasureTime);
						_enamoredTimer.Timeout += () => {
							_enamoredTimer = null;
							_doomPortrait.SetAnimation(DoomPortraitType.Idle);
						};
					}

					PickupItem(item);
				}
				break;
			}
		}
	}
	private void Restart()
	{
		GetTree().ReloadCurrentScene();
		Initialize();
	}

	private void KillIfBelowWorld()
	{
		if (GlobalTranslation.y < -200)
		{
			KillWithCameraUpPan();
		}
	}

	private void RestartOnRequest()
	{
		if (Input.IsActionJustPressed("plr_restart"))
		{
			Restart();
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
		_enamoredTimer?.Stop();
		_enamoredTimer = null;

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
			_crosshair.Visible = false;
		}
	}

	void IDeathPlaneEnterable.EnteredDeathPlane()
	{
		KillWithCameraUpPan();
	}

	private void KillWithCameraUpPan()
	{
		Restart();
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
