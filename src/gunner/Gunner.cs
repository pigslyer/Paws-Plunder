using System;
using Godot;

public class Gunner : KinematicBody, IBulletHittable, IMeleeTargettable
{
    public event Action Died;

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

    public void MoveTo(Vector3 point)
    {
        _agent.SetTargetLocation(point);
        _hasMoveOrder = true;
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
