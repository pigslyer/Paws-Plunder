using System;
using Godot;

public class Gunner : KinematicBody, IBulletHittable, IMeleeTargettable, IDeathPlaneEnterable, IPlayerAttacker, IMoveable
{
    public event Action Died;

    private const int ParallelShots = 5;
    private static readonly float TotalSpreadRad = Mathf.Deg2Rad(30);
    private const float BulletSpeed = 30.0f;
    private const int ShootFrame = 1;

    public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;
    
    private const float WalkSpeed = 8.0f;
    [Export] private PackedScene _bulletScene;
    [Export] private PackedScene _droppedGunScene;

    private AnimatedSprite3D _sprite;
    private NavigationAgent _agent;
    private Spatial _centerOfMassNode;
    private bool _hasMoveOrder = false;

    private Vector3? _queuedAttackDirection = null;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        _centerOfMassNode = GetNode<Spatial>("CenterOfMass");

        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount());
        _sprite.Playing = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        FollowPath(delta);
    }

    private void FollowPath(float delta)
    {
        if (!_hasMoveOrder)
        {
            return;
        }

        if (_queuedAttackDirection != null)
        {
            return;
        }

        Vector3 nextPosition = _agent.GetNextLocation();
        GlobalTranslation = GlobalTranslation.MoveToward(nextPosition, WalkSpeed * delta);

        _hasMoveOrder = !_agent.IsNavigationFinished();
    }

    public void GoTo(Vector3 point)
    {
        _agent.SetTargetLocation(point);
        _hasMoveOrder = true;
    }

    public void AttackTarget(Player player)
    {
        Vector3 forwardBulletVelocity = CenterOfMass.DirectionTo(player.CenterOfMass);
        
        _queuedAttackDirection = forwardBulletVelocity;
        _sprite.Play("Shoot");
    }

    private void OnAnimationFrameChanged()
    {
        if (_sprite.Animation == "Shoot" && _sprite.Frame == ShootFrame && _queuedAttackDirection != null)
        {
            FireBlunderbuss(_queuedAttackDirection.Value);
        }
    }

    private void OnAnimationFinished()
    {
        if (_sprite.Animation == "Shoot")
        {
            _queuedAttackDirection = null;            
            _sprite.Play("Idle");
        }
    }

    private void FireBlunderbuss(Vector3 direction)
    {
        foreach (Vector3 velocity in Globals.CalculateShotgunDirections(direction, TotalSpreadRad, ParallelShots, BulletSpeed))
        {
            ShootBulletWithVelocity(velocity);
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
        _hasMoveOrder = false;
    
        _sprite.Play("Death");

        Spatial droppedGun = _droppedGunScene.Instance<Spatial>();
        GetParent().AddChild(droppedGun);
        droppedGun.GlobalTranslation = CenterOfMass;
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();
        QueueFree();
    }
}
