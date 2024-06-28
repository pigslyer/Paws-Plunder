using Godot;

public interface IBulletHittable
{
    void Hit();
}

public class Bullet : KinematicBody
{
    private Vector3 _velocity;

    public void Initialize(Vector3 velocity, PhysicsLayers3D mask)
    {
        (_velocity, CollisionMask) = (velocity, (uint)mask);   
    }

    public override void _PhysicsProcess(float delta)
    {
        KinematicCollision collision = MoveAndCollide(_velocity * delta);

        if (collision != null)
        {
            if (collision.Collider is IBulletHittable hittable)
            {
                hittable.Hit();
            }

            QueueFree();
        }        
    }
}
