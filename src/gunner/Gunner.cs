using System;
using System.Threading.Tasks;
using Godot;

public class Gunner : KinematicBody, IBulletHittable, IMeleeTargettable, IDeathPlaneEnterable
{
    public event Action Died;

    private const int ParallelShots = 4;
    private static readonly float TotalSpreadRad = Mathf.Deg2Rad(30);
    private const float BulletSpeed = 30.0f;

    public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;
    
    private const float WalkSpeed = 8.0f;
    [Export] private PackedScene _bulletScene;

    private AnimatedSprite3D _sprite;
    private NavigationAgent _agent;
    private Spatial _centerOfMassNode;
    private bool _hasMoveOrder = false;

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
        Vector3 forwardBulletVelocity = CenterOfMass.DirectionTo(player.GlobalTranslation) * BulletSpeed;
        
        for (int i = 0; i < ParallelShots; i++)
        {
            float currentAngleOffset = -TotalSpreadRad / 2 + i * (TotalSpreadRad / (ParallelShots - 1));
            Vector3 currentVelocity = forwardBulletVelocity.Rotated(Vector3.Up, currentAngleOffset);

            ShootBulletWithVelocity(currentVelocity);
        }
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

        CollisionLayer = 0;
        CollisionMask = 0;

        // swap with animation change
        QueueFree();
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();
        QueueFree();
    }
}
