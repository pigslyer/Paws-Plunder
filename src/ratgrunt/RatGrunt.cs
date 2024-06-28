using System;
using Godot;

public class RatGrunt : KinematicBody, IMeleeTargettable, IBulletHittable
{
    public event Action Died;

    [Export] private PackedScene _bulletScene;
    private float _bulletSpeed = 30.0f;

    private const float FarAwaySpeed = 12.0f;
    private const float NearbySpeed = 4.0f;
    private const float WhatIsNearby = 5.0f;

    private NavigationAgent _agent;
    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMass;

    public Vector3 CenterOfMass => _centerOfMass.GlobalTranslation;
    bool _hasMoveTarget = false;

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
        ShootBulletWithVelocity(CenterOfMass.DirectionTo(target.GlobalTranslation) * _bulletSpeed);
    }

    private void ShootBulletWithVelocity(Vector3 velocity)
    {
        Bullet bullet = _bulletScene.Instance<Bullet>();        
        GetTree().Root.AddChild(bullet);
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
        QueueFree();
    }
}
