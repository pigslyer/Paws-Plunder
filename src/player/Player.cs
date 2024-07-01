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

	[Signal]
	public delegate void RespawnPlayer();

	[Signal]
	public delegate void PlayerDied();

	[Export] private bool _initializeOnStartup = false;
	[Export] private PackedScene _bulletScene;

	private const float GravityConst = -120f;

	private Vector3 GravityAcceleration = new Vector3(0, GravityConst, 0);

	private Vector3 JumpVelocity => new Vector3(0, 40, 0);
	private float JumpHorizontalVelocityBoost => 0.25f;
	private float JumpVerticalFromX0zVelocityBoost => 0.01f;
	private const float MaxNaturalSpeed = 23;
	private const float MaxSprintSpeed = 23;
	private const float SensitivityMult = 4f;
	private const float InvulPeriod = 2.0f;
	private const float Acceleration = 100;
	private const float EnamoredByTreasureTime = 2.0f;
	private const float SingleBulletVelocity = 200.0f;
	private const float QuadBulletVelocity = 200.0f;

	private const float WalkPitchScale = 1.0f;
	private const float SprintPitchScale = 1.3f;
	private const int WinScoreCondition = 20000;

	private const int MaxHealth = 3;
	public int Health { get; private set; } = MaxHealth;
	private bool _hasEmittedDeathScreech = false;

	private GunTypes _currentGun = GunTypes.Single;
	private int _remainingAmmo = 1;

	private Vector3 _previousVelocity = Vector3.Zero;
	private Vector2 _mouseMotion = Vector2.Zero;
	public bool LockInPlace = false;

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
	private AnimationPlayer _bothHandsOrClawAnimationPlayer;
	private AnimationPlayer _gunAnimationPlayer;
	public DoomPortrait DoomPortrait;
	public CombatLogControl LogControl;
	private HealthContainer _healthContainer;
	private Spatial _centerOfMassNode;
	private ColorRect _damageEffect;
	private PlayerSounds _sounds;

	private DeathInfo _deathInfo = null;

	private CustomTimer _invulTimer;
	private CustomTimer _enamoredTimer;
	private bool _gameWon = false;

	public override void _Ready()
	{
		_head = GetNode<Spatial>("Head");
		_camera = GetNode<Camera>("%Camera");
		_gun = GetNode<Sprite3D>("%Camera/Gun");
		_debugLabel = GetNode<Label>("CanvasLayer/DebugContainer/PanelContainer/Label");
		_crosshair = GetNode<TextureRect>("%Crosshair");
		_meleeDetectionArea = GetNode<Area>("%Camera/MeleeTargetDetection");
		_pickupDetectionArea = GetNode<Area>("PickupArea");
		_bothHandsOrClawAnimationPlayer = GetNode<AnimationPlayer>("BothHandsOrClaw");
		_gunAnimationPlayer = GetNode<AnimationPlayer>("Gun");
		DoomPortrait = GetNode<DoomPortrait>("CanvasLayer/DoomPortrait");
		LogControl = GetNode<CombatLogControl>("CanvasLayer/DebugContainer/CombatLogControl");
		_healthContainer = GetNode<HealthContainer>("CanvasLayer/HealthContainer");
		_centerOfMassNode = GetNode<Spatial>("CenterOfMass");
		_damageEffect = GetNode<ColorRect>("%DamageEffect");
		_sounds = GetNode<PlayerSounds>("Sounds");

		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<CanvasLayer>("CanvasLayer").Visible = false;
		GetNode<Sprite3D>("%Camera/Claw").Visible = false;
		GetNode<Sprite3D>("%Camera/Gun").Visible = false;

		if (_initializeOnStartup)
		{
			Initialize();
		}

		GlobalSignals.GetInstance().Connect(nameof(GlobalSignals.AddToPlayerScore), this, "_OnAddScore");
	}

	public void Initialize()
	{
		GetNode<CanvasLayer>("CanvasLayer").Visible = true;
		GetNode<Sprite3D>("%Camera/Claw").Visible = true;
		GetNode<Sprite3D>("%Camera/Gun").Visible = true;

		Health = MaxHealth;
		_previousVelocity = Vector3.Zero;

		_remainingAmmo = 1;
		_currentGun = GunTypes.Single;
		_hasEmittedDeathScreech = false;

		UpdateHealthDisplays();
		_camera.RotationDegrees = Vector3.Zero;
		DoomPortrait.SetAnimation(DoomPortraitType.Idle);
		_bothHandsOrClawAnimationPlayer.Play("RESET");
	}

	private void _OnAddScore(int score)
	{
		GD.Print($"Player has scored {score} points!");
		if (score >= WinScoreCondition)
		{
			GD.Print("Player has won!");
			LogControl.SetMsg("You have pillaged enough goods! Find a cannon and press [E] to escape!", 100f);
			GetTree().CallGroup("Cannons", "EnableEscape");
			_gameWon = true;
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		JustFired = false;

		bool isAlive = Health > 0;

		if (!LockInPlace)
		{
			ApplyMovement(delta, hasControls: isAlive);
		}

		if (isAlive)
		{
			MouseRotateCamera(delta);

			if (!LockInPlace)
			{
				MeleeAttack();
				ShootAttack();

				PickUpItems();
			}	
		}
		else if (_deathInfo != null)
		{
			RotateCameraTowards(_deathInfo.GetKillerPosition());
		}

		if (!LockInPlace)
		{
			KillIfBelowWorld();
		}
		
		if (!LockInPlace || Health <= 0)
		{
			RestartOnRequest();
		}

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

			if (Input.IsActionPressed("plr_use"))
			{
				if (_gameWon)
				{
					// get all cannons (there should be only one, but just in case...
					var cannons = GetTree().GetNodesInGroup("Cannons");
					// get closest cannon
					var closestCannon = cannons.OfType<Cannon>().OrderBy(
						c => c.GlobalTransform.origin.DistanceTo(GlobalTransform.origin)).FirstOrDefault();
					if (Mathf.Abs(closestCannon.GlobalTransform.origin.DistanceTo(GlobalTransform.origin)) < 5.0f)
					{
						GD.Print("Player has escaped!");
						KillSelf();
						var deathLabel = GetNode<Label>("%DeathLabel");
						
						deathLabel.Visible = true;
						deathLabel.Text = "You have escaped with the loot!";
					}
				}
			}
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
			_head.Rotation.x,
			_head.Rotation.y,
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

				_sounds.Jumping.Play();
				_sounds.Landing.FadeOut(0.2f);
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

		bool wasOnFloor = IsOnFloor();
		_previousVelocity = MoveAndSlideWithSnap(velocity, snap, Vector3.Up, true);
		_debugLabel.Text = $"Velocity: {velocity}\nSpeed: {velocity.Length()}\nSpeed xz: {velocity.x0z().Length()}";

		_sounds.Footsteps.SetPlaying(IsOnFloor() && hasControls && inputVector.LengthSquared() > 0.001f);	
		_sounds.Footsteps.PitchScale = sprint ? SprintPitchScale : WalkPitchScale;

		if (!wasOnFloor && IsOnFloor() && !_sounds.Landing.Playing)
		{
			_sounds.Landing.Play();
		}
	}

	private void MouseRotateCamera(float delta)
	{
		Vector3 rotation = _camera.RotationDegrees;
		rotation += new Vector3(-_mouseMotion.y, -_mouseMotion.x, 0) * SensitivityMult * delta * Globals.MouseSensitivity;

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

		_bothHandsOrClawAnimationPlayer.Play("ClawAttack");

		JustFired = true;
		Godot.Collections.Array bodies = _meleeDetectionArea.GetOverlappingBodies();

		if (bodies.Count == 0) 
		{
			_sounds.MeleeMiss.Play();
			return;
		} 
		else
		{
			_sounds.Melee.Play();
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

		if (_currentGun == GunTypes.Single)
		{
			_gunAnimationPlayer.Play("ShootSingle");
			_sounds.ShootSingle.Play();

			FireBullet(shotDirection * SingleBulletVelocity);
		}
		else if (_currentGun == GunTypes.Quad)
		{
			_gunAnimationPlayer.Play("ShootQuad");
			_sounds.ShootQuad.Play();

			foreach (Vector3 bulletVelocity in Globals.CalculateShotgunDirections(shotDirection, Mathf.Deg2Rad(30), 5, QuadBulletVelocity))
			{
				FireBullet(bulletVelocity);
			}
		}

		if (_remainingAmmo == 0)
		{
			_gunAnimationPlayer.Connect("animation_finished", this, nameof(OnGunAnimationFinished), flags: (uint)ConnectFlags.Oneshot);
		}
	}

	private void OnGunAnimationFinished(string _)
	{
		if (_remainingAmmo == 0)
		{
			_gunAnimationPlayer.Play("Drop");
		}

		GetTree().CallGroup("MUZZLE", "hide");
	}

	private void FireBullet(Vector3 velocity)
	{
		Bullet bullet = _bulletScene.Instance<Bullet>();
		GetParent().AddChild(bullet);
		bullet.GlobalTranslation = CenterOfMass;

		bullet.Initialize(this, velocity, PhysicsLayers3D.World | PhysicsLayers3D.Enemy);
	}

	private void PickUpItems()
	{
		Godot.Collections.Array pickupables = _pickupDetectionArea.GetOverlappingBodies();

		void PickupItem(Item item)
		{
			if (item.AssociatedScore != 0)
			{
				GlobalSignals.AddScore(item.AssociatedScore);
			}

			LogControl.SetMsg($"Picked up a {item.DisplayName}!");
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
					_gun.Frame = 1;

					_sounds.PickupQuad.Play();
					_gunAnimationPlayer.PlayBackwards("Drop");

					PickupItem(item);
				}
				break;

				case "GruntGun":
				{
					if (_remainingAmmo > 0)
					{
						continue;
					}

					_remainingAmmo = 1;
					_currentGun = GunTypes.Single;
					_gun.Frame = 0;

					_sounds.PickupSingle.Play();
					_gunAnimationPlayer.PlayBackwards("Drop");

					PickupItem(item);
				}
				break;

				case "Treasure":
				{
					DoomPortrait.SetAnimation(DoomPortraitType.Treasure);

					if (Health < MaxHealth)
					{
						Health += 1;
						UpdateHealthDisplays();
					}

					if (_enamoredTimer != null)
					{
						_enamoredTimer.Start(EnamoredByTreasureTime);
					}
					else
					{
						_enamoredTimer = CustomTimer.Start(this, EnamoredByTreasureTime);
						_enamoredTimer.Timeout += () => {
							_enamoredTimer = null;
							DoomPortrait.SetAnimation(DoomPortraitType.Idle);
						};
						_sounds.PickupTreasure.Play();
					}

					PickupItem(item);
				}
				break;
			}
		}
	}
	
	private void KillIfBelowWorld()
	{
		if (Health > 0 && GlobalTranslation.y < -200)
		{
			KillWithCameraUpPan();
		}
	}

	private void RestartOnRequest()
	{
		if (Input.IsActionJustPressed("plr_restart"))
		{
			EmitSignal("RespawnPlayer");
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
		if (Health <= 0)
		{
			return;
		}

		if (_invulTimer != null)
		{
			return;
		}

		Health -= 1;

		_enamoredTimer?.Stop();
		_enamoredTimer = null;

		UpdateHealthDisplays();

		if (Health > 0)
		{
			DoomPortrait.SetAnimation(DoomPortraitType.Pain);
			_damageEffect.Material.Set("shader_param/enable", true);
			_invulTimer = CustomTimer.Start(this, InvulPeriod);
			_sounds.Hurt.Play();

			_invulTimer.Timeout += () => {
				_invulTimer = null;
				_damageEffect.Material.Set("shader_param/enable", false);
				DoomPortrait.SetAnimation(DoomPortraitType.Idle);
			};
		}
		else
		{
			UpdateHealthDisplays();
			_deathInfo = new DeathInfo(info.Source);
		}
	}

	void IDeathPlaneEnterable.EnteredDeathPlane()
	{
		KillWithCameraUpPan();
	}

	private void KillSelf()
	{
		Health = 0;
		UpdateHealthDisplays();
	}

	private void UpdateHealthDisplays()
	{
		bool isDead = Health <= 0;

		_healthContainer.SetHealth(Health);
		DoomPortrait.SetAnimation(isDead ? DoomPortraitType.Death : DoomPortraitType.Idle);
		_damageEffect.Material.Set("shader_param/enable", isDead);
		GetNode<Label>("%DeathLabel").Visible = isDead;
		_crosshair.Visible = !isDead;

		if (isDead && !_hasEmittedDeathScreech)
		{
			LogControl.SetMsg($"{Globals.ProtagonistName} has died!");
			_sounds.Death.Play();
			EmitSignal(nameof(PlayerDied));
			_hasEmittedDeathScreech = true;
		}
	}

	private void KillWithCameraUpPan()
	{
		KillSelf();

		_bothHandsOrClawAnimationPlayer.Play("Death");
		LockInPlace = true;
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
