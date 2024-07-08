using Godot;
using System;

namespace PawsPlunder;

public partial class Sniper : CharacterBody3D, 
    IBulletHittable, 
    IDeathPlaneEnterable, 
    IPlayerAttacker, 
    IMeleeTargettable
{
    public event Action? Died;
    public Vector3 CenterOfMass => _centerOfMassNode.GlobalPosition;

    // frame of "Shoot" animation in which the shot is actually taken
    private const int ShootFrame = 6;
    private const float ProjectileVelocity = 30.0f;
    private static readonly Distro _deathDistro = (1.1f, 0.2f);
    private static readonly Distro _shootDistro = (1.1f, 0.2f);
    private static readonly Distro _attackFrontDistro = (1.1f, 0.2f);
    private static readonly Distro _attackBackDistro = (1.1f, 0.2f);

    [Export] private PackedScene _bulletScene = null!;

    [Export] private AnimatedSprite3D _sprite = null!;
    [Export] private Node3D _centerOfMassNode = null!;
    [Export] private SniperSounds _sounds = null!;

    private Vector3 _nextShotVelocity;

    public override void _Ready()
    {
        _sprite.Play("Idle");
        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount());
    }

    public void AttackTarget(Player target)
    {
        if (_sprite.Animation == "Shoot")
        {
            // unlikely, but still
            return;
        }

        float delayUntilShot = (float)((ShootFrame - 1) / _sprite.SpriteFrames.GetAnimationSpeed("Shoot"));

        Vector3 bulletVelocity = Globals.GetProjectileVelocity(
            target.CenterOfMass, 
            target.Velocity.X0Z(), 
            CenterOfMass, 
            ProjectileVelocity,
            delayUntilShot
        );

        _nextShotVelocity = bulletVelocity;
        _sprite.Play("Shoot");
        
        // TODO: Implement front/back face tracking for snipers
        bool isFront = true;
        if (isFront)
        {
            _sounds.AttackBarkFront.PlayPitched(_attackFrontDistro);
        }
        else
        {
            _sounds.AttackBarkBack.PlayPitched(_attackBackDistro);
        }
    }
    
    private void ShootBulletWithVelocity(Vector3 velocity)
    {
        Bullet bullet = _bulletScene.Instantiate<Bullet>();        
        GetParent().AddChild(bullet);
        bullet.GlobalPosition = CenterOfMass;
        bullet.Initialize(this, velocity, PhysicsLayers3D.World | PhysicsLayers3D.Player);
    }

    void IBulletHittable.Hit(BulletHitInfo info)
    {
        DestroyModel();
    }

    private void OnAnimationFrameChanged()
    {
        if (_sprite.Animation == "Shoot" && _sprite.Frame == ShootFrame)
        {
            ShootBulletWithVelocity(_nextShotVelocity);

            _sounds.Shoot.PlayPitched(_shootDistro);
        }
    }

    private void OnAnimationEnded()
    {
        if (_sprite.Animation == "Shoot")
        {
            _sprite.Play("Idle");
        }
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

        CollisionLayer = 0;
        CollisionMask = 0;

        QueueFree();
    }
}
