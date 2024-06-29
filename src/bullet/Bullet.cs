using Godot;

public struct BulletHitInfo
{
    public Spatial Source;
}

public interface IBulletHittable
{
    void Hit(BulletHitInfo bulletHittable);
}

public class Bullet : KinematicBody
{
    private Vector3 _velocity;
    private Spatial _source;

    public void Initialize(Spatial source, Vector3 velocity, PhysicsLayers3D mask)
    {
        (_source, _velocity, CollisionMask) = (source, velocity, (uint)mask);   
    }

    public override void _PhysicsProcess(float delta)
    {
        KinematicCollision collision = MoveAndCollide(_velocity * delta);

        if (collision != null)
        {
            if (collision.Collider is IBulletHittable hittable)
            {
                BulletHitInfo info = new BulletHitInfo();

                if (_source != null && IsInstanceValid(_source))
                {
                    info.Source = _source;
                }

                hittable.Hit(info);
            }

            QueueFree();
        }        
    }
}
