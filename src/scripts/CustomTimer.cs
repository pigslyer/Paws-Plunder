using System;
using Godot;

public class CustomTimer : Timer 
{
    public event Action Timeout;

    public static CustomTimer Start(Node node, float timeout)
    {
        CustomTimer timer = new CustomTimer();
        node.AddChild(timer);
        timer.Connect("timeout", timer, nameof(OnTimeout));
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
