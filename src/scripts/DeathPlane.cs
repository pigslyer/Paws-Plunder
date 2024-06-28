using Godot;

public interface IDeathPlaneEnterable
{
    void EnteredDeathPlane();
}

public class DeathPlane : Area
{
    public override void _Ready()
    {
        Connect("body_entered", this, nameof(OnBodyEntered));
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
