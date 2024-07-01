using System;
using Godot;

public class TraderMouse : KinematicBody, IDeathPlaneEnterable, IBulletHittable, IMeleeTargettable, IMoveable
{
    public event Action Died;
    private const float Speed = 30.0f;
    public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;
    private static readonly Distro _footstepsDistro = (1.4f, 0.2f);
    private static readonly Distro _deathDistro = (1.6f, 0.2f);

    private bool _hasMoveTarget = false;

    private NavigationAgent _agent;
    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMassNode;
    private TraderMouseSounds _sounds;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent>("NavigationAgent");
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");
        _centerOfMassNode = GetNode<Spatial>("CenterOfMass");
        _sounds = GetNode<TraderMouseSounds>("Sounds");

        _sprite.Play("Idle");
        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount() - 1);
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
        GlobalTranslation = GlobalTranslation.MoveToward(nextPosition, Speed * delta);

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

        _sounds.Footsteps.PlayPitched(_footstepsDistro);
        _sprite.Play("Walk");
    }

    void IBulletHittable.Hit(BulletHitInfo info)
    {
        DestroyModel();
    }

    void IMeleeTargettable.Target(MeleeHitInfo info)
    {
        DestroyModel();
    }

    private void DestroyModel()
    {
        Died?.Invoke();

        CollisionLayer = 0;
        CollisionMask = 0;

        _sprite.Play("Death");
        _sounds.Death.PlayPitched(_deathDistro);
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();

        QueueFree();
    }
}
