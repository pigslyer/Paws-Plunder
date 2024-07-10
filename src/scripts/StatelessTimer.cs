using System;
using Godot;

namespace PawsPlunder;

/// <summary>
/// Represents timer which has no persistent state. Designed to be light
/// on mental load when scripting behaviour. 
/// </summary>
public class StatelessTimer
{
    private Timer _backingTimer;
    private Action? _nextTimeoutAction;

    public StatelessTimer(Node onNode)
    {
        _backingTimer = new()
        {
            OneShot = true,
            Autostart = false,
        };
        _backingTimer.Timeout += OnTimerStopped;

        onNode.AddChild(_backingTimer);
    }

    public void Start(float timeout, Action onTimeout)
    {
        _backingTimer.Start(timeout);        
        _nextTimeoutAction = onTimeout;
    }

    public void Stop()
    {
        if (_nextTimeoutAction != null)
        {
            _backingTimer.Stop();
            _nextTimeoutAction = null;
        }
    }

    public bool IsRunning()
    {
        return _nextTimeoutAction != null;
    }

    private void OnTimerStopped()
    {
        _nextTimeoutAction?.Invoke();
        _nextTimeoutAction = null;
    }
}
