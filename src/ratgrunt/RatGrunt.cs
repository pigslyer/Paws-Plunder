using System;
using System.Linq;
using Godot;

namespace PawsPlunder;

public partial class RatGrunt : CharacterBody3D, 
	IMeleeTargettable, 
	IBulletHittable, 
	IDeathPlaneEnterable, 
	IPlayerAttacker, 
	IMoveable
{
	public event Action? Died;

	[Export] private PackedScene _bulletScene = null!;
	[Export] private PackedScene _droppedGunScene = null!;

	private const float BulletSpeed = 30.0f;

	private const float FarAwaySpeed = 12.0f;
	private const float NearbySpeed = 4.0f;
	private const float WhatIsNearby = 5.0f;
	private const float ChanceForIdleSwing = 0.2f;
	private const int ShootFrame = 1;
	private static readonly Distro _footstepsPitchDistro = (1.0f, 0.2f);
	private static readonly Distro _attackBarkPitchDistro = (1.0f, 0.2f);
	private static readonly Distro _firePitchDistro = (1.0f, 0.2f);
	private static readonly Distro _deathPitchDistro = (1.0f, 0.2f);

	[Export] private AnimatedSprite3D _sprite = null!;
	[Export] private Node3D _centerOfMass = null!;
	[Export] private RatGruntSounds _sounds = null!;

	public Vector3 CenterOfMass => _centerOfMass.GlobalPosition;
	Vector3 IMoveable.FeetPosition => GlobalPosition;

	private Vector3[]? _path = null;
	private int _reachedIndex = 0;
	
	private Vector3? _queuedAttackDirection;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_sprite.Play("Idle");
		_sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount() - 1);
	}

	public override void _PhysicsProcess(double delta)
	{
		FollowPath(delta);
	}

	private void FollowPath(double delta)
	{
		if (_path == null)
		{
			return;
		}

		if (_queuedAttackDirection != null)
		{
			return;
		}

		Vector3 nextPosition = _path[_reachedIndex];  
		Vector3 targetPosition = _path[^1];

		bool isNearby = GlobalPosition.DistanceTo(targetPosition) < WhatIsNearby;

		float speed = isNearby ? NearbySpeed : FarAwaySpeed; 
		GlobalPosition = GlobalPosition.MoveToward(nextPosition, speed * (float)delta);

		Debug(GlobalPosition.DistanceTo(nextPosition));
		if (GlobalPosition.DistanceSquaredTo(nextPosition) < 1)
		{
			Debug($"hi there 2 {nextPosition}");
			_reachedIndex += 1;

			if (_reachedIndex == _path.Length) {
				Debug($"hi there uwu ");
				_path = null;

				_sounds.Footsteps.Stop();
				_sprite.Play("Idle");
			}
		}
	}

	public void GoTo(Vector3[] path)
	{
		if (path.Length == 0)
		{
			return;
		}
		
		_path = path[1..];
		_reachedIndex = 0; 

		_sounds.Footsteps.PlayPitched(_footstepsPitchDistro);
		_sprite.Play("Walk");
	}

	public void AttackTarget(Player target)
	{
		_queuedAttackDirection = CenterOfMass.DirectionTo(target.CenterOfMass) * BulletSpeed;
		
		_sprite.Play("Shoot");
		_sounds.AttackBark.PlayPitched(_attackBarkPitchDistro);
	}

	private void OnAnimationFrameChanged()
	{
		if (_sprite.Animation == "Shoot" && _sprite.Frame == ShootFrame && _queuedAttackDirection != null)
		{
			ShootBulletWithVelocity(_queuedAttackDirection.Value);

			_sounds.Fire.PlayPitched(_firePitchDistro);
		}
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "Idle")
		{
			float chanceToSwing = Globals.Rng.Randf();

			if (chanceToSwing < ChanceForIdleSwing)
			{
				_sprite.Play("IdleSwordSwing");                
			}
		}
		else if (_sprite.Animation == "IdleSwordSwing")
		{
			_sprite.Play("Idle");
		}
		else if (_sprite.Animation == "Shoot")
		{
			_queuedAttackDirection = null;

			if (_path != null)
			{
				_sprite.Play("Walk");
				_sounds.Footsteps.PlayPitched(_footstepsPitchDistro);
			}
			else
			{
				_sprite.Play("Idle");
			}
		}
	}

	private void ShootBulletWithVelocity(Vector3 velocity)
	{
		Bullet bullet = _bulletScene.Instantiate<Bullet>();        
		GetParent().AddChild(bullet);
		bullet.GlobalPosition = CenterOfMass;
		bullet.Initialize(this, velocity, PhysicsLayers3D.World | PhysicsLayers3D.Player);
	}

	void IMeleeTargettable.Target(MeleeHitInfo info)
	{
		DestroyModel();
	}

	void IBulletHittable.Hit(BulletHitInfo info)
	{
		DestroyModel();
	}

	private void DestroyModel()
	{
		Died?.Invoke();

		CollisionLayer = 0;
		CollisionMask = 0;
		_path = null;
		_queuedAttackDirection = null;

		Node3D droppedGun = _droppedGunScene.Instantiate<Node3D>();
		GetParent().AddChild(droppedGun);
		droppedGun.GlobalPosition = CenterOfMass;
		
		_sprite.Play("Death");
		_sounds.Death.PlayPitched(_deathPitchDistro);
		_sounds.Footsteps.Stop();
	}

	void IDeathPlaneEnterable.EnteredDeathPlane()
	{
		Died?.Invoke();
		QueueFree();
	}
}
