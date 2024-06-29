using System;
using Godot;

public class RatGrunt : KinematicBody, IMeleeTargettable, IBulletHittable, IDeathPlaneEnterable, IPlayerAttacker, IMoveable
{
    public event Action Died;

    [Export] private PackedScene _bulletScene;
    private const float BulletSpeed = 30.0f;

    private const float FarAwaySpeed = 12.0f;
    private const float NearbySpeed = 4.0f;
    private const float WhatIsNearby = 5.0f;
    private const float ChanceForIdleSwing = 0.2f;
    private const int ShootFrame = 1;

    private NavigationAgent _agent;
    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMass;

    public Vector3 CenterOfMass => _centerOfMass.GlobalTranslation;
    private bool _hasMoveTarget = false;
    private Vector3? _queuedAttackDirection = null;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _centerOfMass = GetNode<Spatial>("CenterOfMass");
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");

        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount());
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
    }

    public void GoTo(Vector3 point)
    {
        _hasMoveTarget = true;
        _agent.SetTargetLocation(point);
    }

    public void AttackTarget(Player target)
    {
        _queuedAttackDirection = CenterOfMass.DirectionTo(target.GlobalTranslation) * BulletSpeed;
        _sprite.Play("Shoot");
    }

    private void OnAnimationFrameChanged()
    {
        if (_sprite.Animation == "Shoot" && _sprite.Frame == ShootFrame && _queuedAttackDirection != null)
        {
            ShootBulletWithVelocity(_queuedAttackDirection.Value);
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
            _sprite.Play("Idle");
            _queuedAttackDirection = null;
        }
    }

    private void ShootBulletWithVelocity(Vector3 velocity)
    {
        Bullet bullet = _bulletScene.Instance<Bullet>();        
        GetParent().AddChild(bullet);
        bullet.GlobalTranslation = CenterOfMass;
        bullet.Initialize(velocity, PhysicsLayers3D.World | PhysicsLayers3D.Player);
    }

    void IMeleeTargettable.Target(MeleeTargetInfo info)
    {
        DestroyModel();
    }

    void IBulletHittable.Hit()
    {
        DestroyModel();
    }

    private void DestroyModel()
    {
        Died?.Invoke();

        CollisionLayer = 0;
        CollisionMask = 0;
        _hasMoveTarget = false;

        // replace with death
        QueueFree();
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();
        QueueFree();
    }
}
