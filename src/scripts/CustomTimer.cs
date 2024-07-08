using System;
using Godot;

namespace PawsPlunder;

// TODO: custom timer could probably avoid being reallocated all the time 
// common problems include having to clean up timer (set it to null) at timeout
// having to check both null equality and IsStopped
// maybe do some extension method with a nullable parameter fuckery >:3
// extension methods can have ref parameters if they're extensioning structs >:3
public partial class CustomTimer : Timer 
{
    public static CustomTimer Start(Node node, float timeout)
    {
        CustomTimer timer = new();
        node.AddChild(timer);
        timer.Start(timeout);         

        return timer;
    }

    public new void Stop()
    {
        base.Stop();
        QueueFree();
    }

    private void OnTimeout()
    {
        QueueFree();
    }
}
