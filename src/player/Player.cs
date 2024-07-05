using System;
using System.Linq;
using Godot;

namespace PawsPlunder;

public partial class Player : CharacterBody3D, IBulletHittable, IDeathPlaneEnterable
{
	private enum GunTypes
	{
		Single,
		Quad,
	}

	[Signal]
	public delegate void RespawnPlayerEventHandler();

	[Signal]
	public delegate void PlayerDiedEventHandler();

	[Export] private bool _initializeOnStartup = false;
	[Export] private PackedScene _bulletScene = null!;
	[Export] private PackedScene _youWonScene = null!;

	private const float GravityConst = -120f;

	private Vector3 GravityAcceleration = new(0, GravityConst, 0);

	private Vector3 JumpVelocity => new(0, 40, 0);
	private float JumpHorizontalVelocityBoost => 0.25f;
	private float JumpVerticalFromX0zVelocityBoost => 0.01f;
	private const float MaxNaturalSpeed = 23;
	private const float MaxSprintSpeed = 23;
	private const float SensitivityMult = 8f;
	private const float InvulPeriod = 2.0f;
	private const float Acceleration = 100;
	private const float EnamoredByTreasureTime = 2.0f;
	private const float SingleBulletVelocity = 200.0f;
	private const float QuadBulletVelocity = 200.0f;

	private const float WalkPitchScale = 1.0f;
	private const float SprintPitchScale = 1.3f;
	private const int WinScoreCondition = 100000;

	private const int MaxHealth = 3;
	public int Health { get; private set; } = MaxHealth;
	private bool _hasEmittedDeathScreech = false;

	private GunTypes _currentGun = GunTypes.Single;
	private int _remainingAmmo = 1;

	private Vector2 _mouseMotion = Vector2.Zero;
	public bool LockInPlace = false;

	public bool JustFired { get; private set;} = false;
	public Vector3 CenterOfMass => _centerOfMassNode.GlobalPosition;

	[Export] private Node3D _head = null!;
	[Export] private Camera3D _camera = null!;
	[Export] private Sprite3D _gun = null!;
	[Export] private Label _debugLabel = null!;
	[Export] private TextureRect _crosshair = null!;
	[Export] private Area3D _meleeDetectionArea = null!;
	[Export] private Area3D _pickupDetectionArea = null!;
	[Export] private AnimationPlayer _bothHandsOrClawAnimationPlayer = null!;
	[Export] private AnimationPlayer _gunAnimationPlayer = null!;
	[Export] public DoomPortrait DoomPortrait = null!;
	[Export] public CombatLog LogControl = null!;
	[Export] private HealthContainer _healthContainer = null!;
	[Export] private Node3D _centerOfMassNode = null!;
	[Export] private ColorRect _damageEffect = null!;
	[Export] private PlayerSounds _sounds = null!;

	private DeathInfo? _deathInfo = null;

	private CustomTimer? _invulTimer;
	private CustomTimer? _enamoredTimer;

	// TODO: Remove both of these
	private bool _gameWon = false;
	private int _trackedScore = 0;

	public override void _Ready()
	{
		// TODO: move this somewhere else!
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// TODO: these probably shouldn't be here?
		GetNode<CanvasLayer>("CanvasLayer").Visible = false;
		GetNode<Sprite3D>("%Camera/Claw").Visible = false;
		GetNode<Sprite3D>("%Camera/Gun").Visible = false;

		if (_initializeOnStartup)
		{
			Initialize();
		}

		GlobalSignals.GetInstance().AddToPlayerScore += OnScoreAdded;
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

	private void OnScoreAdded(int score)
	{
		bool hadWon = HasWon();

		_trackedScore += score;		

		if (!hadWon && HasWon())
		{
			LogControl.PushMsg("You have pillaged enough goods! Find a cannon and press [E] to escape!");

			GetNode<CanvasItem>("%EscapeText").Show();
			GetNode<ScoreDisplay>("%ScoreDisplay").Modulate = Colors.Yellow;

			GetTree().CallGroup("Cannons", "EnableEscape");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta;
		JustFired = false;

		bool isAlive = Health > 0;

		if (!LockInPlace)
		{
			ApplyMovement(fDelta, hasControls: isAlive);
		}

		if (isAlive)
		{
			if (!LockInPlace)
			{
				MouseRotateCamera(fDelta);
				MeleeAttack();
				ShootAttack();

				PickUpItems();

				EscapeShip();
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
		GravityAcceleration.Y = enable ? GravityConst : 0;	
	}

	private void ApplyMovement(float fDelta, bool hasControls)
	{
		Vector3 newVelocity = Vector3.Zero;

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
			// TODO: use actual math here
			Basis cameraBasis = _camera.GlobalTransform.Basis;

			Vector3 right = cameraBasis.X;
			right.Y = 0;
			right = right.Normalized();

			Vector3 forward = cameraBasis.Z;
			forward.Y = 0;
			forward = forward.Normalized();

			Vector3 globalizedInput = inputVector.Y * forward + inputVector.X * right;
			Vector3 velocityXz = _previousVelocity.X0Z();

			float currentNaturalMaxSpeed = sprint ? MaxSprintSpeed : MaxNaturalSpeed;

			float currentSpeed = velocityXz.Length();

			if (currentSpeed > currentNaturalMaxSpeed)
			{
				if (IsOnFloor())
				{
					// TODO: remove hardcoded value
					currentNaturalMaxSpeed = Mathf.MoveToward(currentSpeed, currentNaturalMaxSpeed, 20 * fDelta);
				}
				else
				{
					currentNaturalMaxSpeed = currentSpeed;
				}
			}

			velocityXz += globalizedInput * Acceleration * fDelta;
			velocityXz = velocityXz.LimitLength(currentNaturalMaxSpeed);

			newVelocity += velocityXz;
		}


		// TODO: Move this somewhere else?
		// camera offset
		{
			Vector3 headOffset = Vector3.Zero;
			Vector3 headRotation = new(_head.Rotation.X, _head.Rotation.Y, float.Clamp(_head.Rotation.Z, -0.05f, 0.05f));

			float bobOffset = float.Sin(fDelta);
			headOffset.Y = bobOffset;
		
			float swayRad = float.Sign(inputVector.X) * -0.05f;
			headRotation.Z = Mathf.LerpAngle(headRotation.Z, swayRad, 0.05f);
			
			_head.Position = headOffset;
			_head.Rotation = headRotation;
		}

		// gravity
		{
			newVelocity += new Vector3(0, _previousVelocity.Y, 0);
			newVelocity += GravityAcceleration * fDelta;
		}

		// jumping
		{

			if (IsOnFloor() && jump)
			{
				Vector3 jumpVelocity = JumpVelocity;

				if (newVelocity.X0Z().Length() > MaxNaturalSpeed)
				{
					//jumpVelocity += jumpVelocity * (velocity.x0z().Length() - MaxNaturalSpeed) * JumpVerticalFromX0zVelocityBoost;
				}				

				newVelocity += jumpVelocity;
				newVelocity += newVelocity.X0Z() * JumpHorizontalVelocityBoost;

				_sounds.Jumping.Play();
				_sounds.Landing.FadeOut(0.2f);
			}
		}

		// deaccel
		{
			if (IsOnFloor() && inputVector.Length() < 0.01f)
			{
				newVelocity -= newVelocity.X0Z();
				//velocity += -velocity.x0z() + velocity.x0z() * 0.8f;
			}
		}

		bool wasOnFloor = IsOnFloor();
		Velocity = newVelocity;
		MoveAndSlide();
		_debugLabel.Text = $"Velocity: {newVelocity}\nSpeed: {newVelocity.Length()}\nSpeed xz: {newVelocity.X0Z().Length()}";

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
		rotation += new Vector3(-_mouseMotion.Y, -_mouseMotion.X, 0) * SensitivityMult * delta * Globals.MouseSensitivity;

		rotation.X = float.Clamp(rotation.X, -85.0f, 85.0f);
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

		void PickupItem(IItem item)
		{
			if (item.AssociatedScore != 0)
			{
				GlobalSignals.AddScore(item.AssociatedScore);
			}

			LogControl.PushMsg($"Picked up a {item.DisplayName}!");
			((Node)item).QueueFree();
		}

		foreach (IItem item in pickupables.OfType<IItem>())
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
	
	private void EscapeShip()
	{
		if (!HasWon())
		{
			return;
		}		

		bool use = Input.IsActionJustPressed("plr_use");

		if (!use)
		{
			return;
		}

		// get all cannons (there should be only one, but just in case...
		Godot.Collections.Array cannons = GetTree().GetNodesInGroup("Cannons");
		// get closest cannon
		Cannon closestCannon = cannons.OfType<Cannon>().OrderBy(
			c => c.GlobalTransform.origin.DistanceTo(GlobalTransform.origin)).FirstOrDefault();
		
		if (Mathf.Abs(closestCannon.GlobalTransform.origin.DistanceTo(GlobalTransform.origin)) < 12.0f)
		{
			GetTree().ChangeSceneTo(_youWonScene);
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

	private bool HasWon()
	{
		return _trackedScore >= WinScoreCondition;
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

	private void KillWithCameraUpPan()
	{
		Health = 0;
		UpdateHealthDisplays();

		_bothHandsOrClawAnimationPlayer.Play("Death");
		LockInPlace = true;
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
			LogControl.PushMsg($"{Globals.ProtagonistName} has died!");
			_sounds.Death.Play();
			EmitSignal(nameof(PlayerDied));
			_hasEmittedDeathScreech = true;
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
