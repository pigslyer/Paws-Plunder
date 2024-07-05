using Godot;

namespace PawsPlunder;

public struct BulletHitInfo
{
    public Node3D Source;
}

public interface IBulletHittable
{
    void Hit(BulletHitInfo bulletHittable);
}

public partial class Bullet : CharacterBody3D 
{
    private Vector3 _velocity;
    private Node3D _source = null!;

    public void Initialize(Node3D source, Vector3 velocity, PhysicsLayers3D mask)
    {
        (_source, _velocity, CollisionMask) = (source, velocity, (uint)mask);   
    }

    public override void _PhysicsProcess(double delta)
    {
        KinematicCollision3D collision = MoveAndCollide(_velocity * (float)delta);

        if (collision != null)
        {
            if (collision.GetCollider() is IBulletHittable hittable)
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
