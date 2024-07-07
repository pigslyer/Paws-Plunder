using Godot;
using System;

namespace PawsPlunder;

public partial class ScoreDisplay : Control
{
    private Label _label;
    private int _shownScore;
    private int _shownScore0;
    private float _lerp = 1.0F;

    public override void _Ready()
    {
        // TODO: check if callable syntax ok
        _label = GetNode<Label>("Number");
        GlobalSignals.GetInstance().Connect(
            nameof(GlobalSignals.AddToPlayerScore),
            new Godot.Callable(this, nameof(GlobalSignals.AddToPlayerScoreEventHandler)),
            (uint)ConnectFlags.OneShot);
        // TODO: idk which flags?
    }

    public void SetScore(int score)
    {
        _shownScore0 = GetScoreLerp();
        _shownScore = score;
        _lerp = 0.0F;
    }

    public void AddScore(int score)
    {
        SetScore(GetScore() + score);
    }

    public int GetScore()
    {
        return _shownScore;
    }

    public int GetScoreLerp()
    {
        return _shownScore0 + (int)Math.Floor(_lerp * (_shownScore - _shownScore0 - 1)) + (_lerp > 0.0F ? 1 : 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        _lerp = Math.Min(1.0F, _lerp + (float)delta);
        _label.Text = " " + GetScoreLerp().ToString();
    }
}
