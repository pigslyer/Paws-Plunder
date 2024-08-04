using System;
using System.Collections.Generic;
using Godot;

namespace PawsPlunder;

public partial class TraderMouse : CharacterBody3D, 
    IDeathPlaneEnterable, 
    IBulletHittable, 
    IMeleeTargettable, 
    IMoveable
{
    public event Action? Died;
    private const float Speed = 30.0f;
    public Vector3 CenterOfMass => _centerOfMassNode.GlobalPosition;

    Vector3 IMoveable.FeetPosition => GlobalPosition;

    private static readonly Distro _footstepsDistro = (1.4f, 0.2f);
    private static readonly Distro _deathDistro = (1.6f, 0.2f);

    private Vector3[]? _path = null;
    private int _reachedIndex = 0;

    [Export] private NavigationAgent3D _agent = null!;
    [Export] private AnimatedSprite3D _sprite = null!;
    [Export] private Node3D _centerOfMassNode = null!;
    [Export] private TraderMouseSounds _sounds = null!;

    public override void _Ready()
    {
        _sprite.Play("Idle");
        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount() - 1);
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;
        FollowPath(fDelta);
    }

    private void FollowPath(float delta)
    {
        if (_path == null)
        {
            return;
        }

        Vector3 nextPosition = _path[_reachedIndex]; 
        GlobalPosition = GlobalPosition.MoveToward(nextPosition, Speed * delta);

        if (GlobalPosition.DistanceSquaredTo(nextPosition) < 0.0001f) {
            _reachedIndex += 1;

            if (_reachedIndex == _path.Length) {
                _path = null;

                _sounds.Footsteps.Stop();
                _sprite.Play("Idle");
            }
        }
    }

    public void GoTo(Vector3[] path)
    {
        _path = path;
        _reachedIndex = 0;

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
        _path = null;

        _sprite.Play("Death");
        _sounds.Death.PlayPitched(_deathDistro);
        _sounds.Footsteps.Stop();
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();

        QueueFree();
    }
}
