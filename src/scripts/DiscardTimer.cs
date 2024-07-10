using System;
using Godot;

namespace PawsPlunder;

/// <summary>
/// Easily created timer to be used for one time special effects. Should not be stored  
/// anywhere, as it only remains valid for duration of timer.
/// </summary> 
public struct DiscardTimer
{
    public event Action Timeout
    {
        add => _backingTimer.Timeout += value;
        remove => _backingTimer.Timeout -= value;
    }

    private Timer _backingTimer; 

    public static DiscardTimer Start(Node node, float timeout)
    {
        Timer timer = new()
        {
            Autostart = false,
            OneShot = true,
        };
        timer.Timeout += timer.QueueFree;
        node.AddChild(timer);
        
        timer.Start(timeout);

        return new()
        {
            _backingTimer = timer,
        };
    }
}
