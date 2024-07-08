using Godot;

namespace PawsPlunder;

public interface IDeathPlaneEnterable
{
    void EnteredDeathPlane();
}

public partial class DeathPlane : Area3D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        // something like the player that wants to be aware of when it has entered a death plane
        if (body is IDeathPlaneEnterable enterable)
        {
            enterable.EnteredDeathPlane();
        }
        // something like a bullet that just shit itself and should die once going OOB
        else
        {
            body.QueueFree();
        }
    }
}
