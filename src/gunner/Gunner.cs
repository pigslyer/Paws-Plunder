using System;
using Godot;

namespace PawsPlunder;

public partial class Gunner : CharacterBody3D, 
    IBulletHittable, 
    IMeleeTargettable, 
    IDeathPlaneEnterable, 
    IPlayerAttacker, 
    IMoveable
{
    public event Action? Died;

    private const int ParallelShots = 5;
    private static readonly float TotalSpreadRad = Mathf.DegToRad(30);
    private const float BulletSpeed = 30.0f;
    private const int ShootFrame = 1;
    private static readonly Distro _footstepsDistro = (0.7f, 0.2f);
    private static readonly Distro _attackBarkDistro = (0.7f, 0.2f);
    private static readonly Distro _blunderbussDistro = (0.7f, 0.2f);
    private static readonly Distro _deathDistro = (0.7f, 0.2f);

    public Vector3 CenterOfMass => _centerOfMassNode.GlobalPosition;
    Vector3 IMoveable.FeetPosition => GlobalPosition;
    
    private const float WalkSpeed = 8.0f;
    [Export] private PackedScene _bulletScene = null!;
    [Export] private PackedScene _droppedGunScene = null!;

    [Export] private AnimatedSprite3D _sprite = null!;
    [Export] private Node3D _centerOfMassNode = null!;
    [Export] private GunnerSounds _sounds = null!;

    private Vector3[]? _path = null;
    private int _reachedIndex = 0;

    private Vector3? _queuedAttackDirection = null;

    public override void _Ready()
    {
        _sprite.Play("Idle");
        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount());
    }

    public override void _PhysicsProcess(double delta)
    {
        FollowPath((float)delta);
    }

    private void FollowPath(float delta)
    {
        if (_path == null)
        {
            _sounds.Footsteps.Stop();
            return;
        }

        if (_queuedAttackDirection != null)
        {
            _sounds.Footsteps.Stop();
            return;
        }

        Vector3 nextPosition = _path[_reachedIndex];
        GlobalPosition = GlobalPosition.MoveToward(nextPosition, WalkSpeed * delta);

        if (GlobalPosition.DistanceSquaredTo(nextPosition) < 0.0001f)
        {
            _reachedIndex += 1;

            if (_reachedIndex == _path.Length)
            {
                _path = null;

                _sprite.Play("Idle");
                _sounds.Footsteps.Stop();
            }
        }
    }

    public void GoTo(Vector3[] path)
    {
        _path = path;
        _reachedIndex = 0;
        
        _sprite.Play("Walk");
        _sounds.Footsteps.PlayPitched(_footstepsDistro);
    }

    public void AttackTarget(Player player)
    {
        Vector3 forwardBulletVelocity = CenterOfMass.DirectionTo(player.CenterOfMass);
        
        _queuedAttackDirection = forwardBulletVelocity;

        _sprite.Play("Shoot");
        _sounds.Footsteps.Stop();
        _sounds.AttackBark.PlayPitched(_attackBarkDistro);
    }

    private void OnAnimationFrameChanged()
    {
        if (_sprite.Animation == "Shoot" && _sprite.Frame == ShootFrame && _queuedAttackDirection != null)
        {
            FireBlunderbuss(_queuedAttackDirection.Value);

            _sounds.Blunderbuss.PlayPitched(_blunderbussDistro);
        }
    }

    private void OnAnimationFinished()
    {
        if (_sprite.Animation == "Shoot")
        {
            _queuedAttackDirection = null;            
            if (_path != null)
            {
                _sprite.Play("Walk");
                _sounds.Footsteps.PlayPitched(_footstepsDistro);
            }
            else
            {
                _sprite.Play("Idle");
            }
        }
    }

    private void FireBlunderbuss(Vector3 direction)
    {
        Span<Vector3> shotVelocities = stackalloc Vector3[ParallelShots]; 

        Globals.CalculateShotgunDirections(direction, TotalSpreadRad, BulletSpeed, shotVelocities);
        foreach (Vector3 velocity in shotVelocities)
        {
            ShootBulletWithVelocity(velocity);
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
    
        _sprite.Play("Death");
        _sounds.Death.PlayPitched(_deathDistro);
        _sounds.Footsteps.Stop();

        Node3D droppedGun = _droppedGunScene.Instantiate<Node3D>();
        GetParent().AddChild(droppedGun);
        droppedGun.GlobalPosition = CenterOfMass;
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();
        QueueFree();
    }
}
