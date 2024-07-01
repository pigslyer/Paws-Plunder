using System;
using Godot;

public class RatGrunt : KinematicBody, IMeleeTargettable, IBulletHittable, IDeathPlaneEnterable, IPlayerAttacker, IMoveable
{
    public event Action Died;

    [Export] private PackedScene _bulletScene;
    [Export] private PackedScene _droppedGunScene;

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

    private NavigationAgent _agent;
    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMass;
    private RatGruntSounds _sounds;

    public Vector3 CenterOfMass => _centerOfMass.GlobalTranslation;
    private bool _hasMoveTarget = false;
    private Vector3? _queuedAttackDirection = null;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _centerOfMass = GetNode<Spatial>("CenterOfMass");
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        _sounds = GetNode<RatGruntSounds>("Sounds");

        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount() - 1);
        _sprite.Playing = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        FollowPath(delta);
    }

    private void FollowPath(float delta)
    {
        if (!_hasMoveTarget)
        {
            return;
        }

        if (_queuedAttackDirection != null)
        {
            return;
        }

        Vector3 nextPosition = _agent.GetNextLocation();
        Vector3 targetPosition = _agent.GetTargetLocation();

        bool isNearby = GlobalTranslation.DistanceTo(targetPosition) < WhatIsNearby;

        float speed = isNearby ? NearbySpeed : FarAwaySpeed; 
        GlobalTranslation = GlobalTranslation.MoveToward(nextPosition, speed * delta);

        _hasMoveTarget = !_agent.IsNavigationFinished();

        if (!_hasMoveTarget)
        {
            _sounds.Footsteps.Stop();
            _sprite.Play("Idle");
        }
    }

    public void GoTo(Vector3 point)
    {
        _hasMoveTarget = true;
        _agent.SetTargetLocation(point);

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

            if (_hasMoveTarget)
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
        Bullet bullet = _bulletScene.Instance<Bullet>();        
        GetParent().AddChild(bullet);
        bullet.GlobalTranslation = CenterOfMass;
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
        _hasMoveTarget = false;
        _queuedAttackDirection = null;

        Spatial droppedGun = _droppedGunScene.Instance<Spatial>();
        GetParent().AddChild(droppedGun);
        droppedGun.GlobalTranslation = CenterOfMass;
        
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
