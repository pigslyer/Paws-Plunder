using Godot;
using System;

public class Sniper : KinematicBody, IBulletHittable, IDeathPlaneEnterable, IPlayerAttacker
{
    public event Action Died;
    public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;

    // frame of "Shoot" animation in which the shot is actually taken
    private const int ShootFrame = 6;
    private const float ProjectileVelocity = 30.0f;
    private static readonly Distro _deathDistro = (1.1f, 0.2f);
    private static readonly Distro _shootDistro = (1.1f, 0.2f);
    private static readonly Distro _attackFrontDistro = (1.1f, 0.2f);
    private static readonly Distro _attackBackDistro = (1.1f, 0.2f);

    [Export] private PackedScene _bulletScene;

    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMassNode;
    private SniperSounds _sounds;

    private Vector3 _nextShotVelocity;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");        
        _sounds = GetNode<SniperSounds>("Sounds");
        _centerOfMassNode = GetNode<Spatial>("CenterOfMass");

        _sprite.Frame = Globals.Rng.RandiRange(0, _sprite.FrameCount());
        _sprite.Playing = true;
    }

    public void AttackTarget(Player target)
    {
        if (_sprite.Animation == "Shoot")
        {
            // unlikely, but still
            return;
        }

        float delayUntilShot = (ShootFrame - 1) / _sprite.Frames.GetAnimationSpeed("Shoot");

        Vector3 bulletVelocity = SquadController.GetProjectileVelocity(
            target.CenterOfMass, 
            target.Velocity.x0z(), 
            CenterOfMass, 
            ProjectileVelocity,
            delayUntilShot
        );

        _nextShotVelocity = bulletVelocity;

        _sprite.Play("Shoot");
        
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
        Bullet bullet = _bulletScene.Instance<Bullet>();        
        GetParent().AddChild(bullet);
        bullet.GlobalTranslation = CenterOfMass;
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
