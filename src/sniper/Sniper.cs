using Godot;
using System;

public class Sniper : KinematicBody, IBulletHittable, IDeathPlaneEnterable, IPlayerAttacker
{
    public event Action Died;
    public Vector3 CenterOfMass => _centerOfMassNode.GlobalTranslation;

    // frame of "Shoot" animation in which the shot is actually taken
    private const int ShootFrame = 6;
    private const float ProjectileVelocity = 30.0f;

    [Export] private PackedScene _bulletScene;

    private AnimatedSprite3D _sprite;
    private Spatial _centerOfMassNode;
    private Vector3 _nextShotVelocity;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite3D>("AnimatedSprite3D");        
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
    }

    void IDeathPlaneEnterable.EnteredDeathPlane()
    {
        Died?.Invoke();

        CollisionLayer = 0;
        CollisionMask = 0;

        QueueFree();
    }
}
