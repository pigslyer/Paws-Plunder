using Godot;
using System;

namespace PawsPlunder;

public partial class CustomTimer : Timer
{
    public event Action Timeout;

    public static CustomTimer Start(Node node, float timeout)
    {
        CustomTimer timer = new CustomTimer();
        node.AddChild(timer);
        // TODO: check if callable syntax ok
        timer.Connect(
            "timeout",
            new Godot.Callable(timer, nameof(OnTimeout)),
            (uint)ConnectFlags.OneShot);
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
        Timeout?.Invoke();
        QueueFree();
    }
}
