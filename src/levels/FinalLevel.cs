using Godot;

namespace PawsPlunder;

public partial class FinalLevel : Node3D
{
    private Player _player;
    private Path3D _catapultPath;
    private PathFollow3D _catapultPathFollow;

    private AudioStreamPlayer _musicMenu;
    private AudioStreamPlayer _musicGameplay;
    private AudioStreamPlayer _musicDeath;
    private AudioStreamPlayer _windPlayer;
    private AudioStreamPlayer _cannonPlayer;

    public override void _Ready()
    {
        _player = GetNode<Player>("%Player");
        _catapultPath = GetNode<Path3D>("CatapultPath");
        _catapultPathFollow = GetNode<PathFollow3D>("CatapultPath/CatapultPathFollow");
        _musicMenu = GetNode<AudioStreamPlayer>("MusicMenu");
        _musicGameplay = GetNode<AudioStreamPlayer>("MusicGameplay");
        _musicDeath = GetNode<AudioStreamPlayer>("MusicDeath");
        _windPlayer = GetNode<AudioStreamPlayer>("Wind");
        _cannonPlayer = GetNode<AudioStreamPlayer>("Cannon");

        ColorRect catapultOverlay = GetNode<ColorRect>("%CatapultEffect");
        catapultOverlay.Material.Set("shader_param/alpha", 1f);
        catapultOverlay.Material.Set("shader_param/inner_radius", 0.9f);
        catapultOverlay.Material.Set("shader_param/outer_radius", 0.9f);

        _musicMenu.Play();

        _player.LockInPlace = true;
    }

    private void _on_MainMenu_StartGame()
    {
        _player.LockInPlace = true;

        _musicMenu.Stop();
        _musicGameplay.Play();

        GetNode<CanvasLayer>("%MainMenu").Visible = false;
        _player.Initialize();
        _SpawnPlayer();
    }

    private void _SpawnPlayer()
    {
        _musicMenu.Stop();

        //_player.ToggleGravity(false);
        _player.Position = Vector3.Zero;
        _player.DoomPortrait.SetAnimation(DoomPortraitType.Flying);

        var catapultOverlay = GetNode<ColorRect>("%CatapultEffect");
        catapultOverlay.Visible = true;

        var tween = GetNode<Tween>("%CatapultTween");

        tween.TweenProperty(_catapultPathFollow, "unit_offset", 1.0f, 1.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenProperty(catapultOverlay.Material, "shader_param/alpha", 0.0f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(catapultOverlay.Material, "shader_param/inner_radius", 1.0f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(catapultOverlay.Material, "shader_param/outer_radius", 1.0f, 0.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_player, "lock_in_place", false, 1.0f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);

        // TODO: Tween callback
        // tween.InterpolateCallback(_player.DoomPortrait, 1.5f, nameof(_player.DoomPortrait.SetAnimation), DoomPortraitType.Idle);

        tween.Play();
        _windPlayer.Play();
        _cannonPlayer.Play();
        _cannonPlayer.FadeOut(1.1f);

        CustomTimer.Start(this, 1.6f).Timeout += () =>
        {
            _windPlayer.FadeOut(0.6f);
        };
    }

    private void OnPlayerDied()
    {
        _musicMenu.Stop();
        _musicGameplay.Stop();
        _musicDeath.Play();
    }

    private void _on_Player_RespawnPlayer()
    {
        Globals.RandomizeProtag();

        _player.LockInPlace = true;
        _player.Initialize();
        _SpawnPlayer();

        _player.LogControl.PushMsg($"Good luck {Globals.ProtagonistName}!");
        GlobalSignals.AddScore(-1000);

        _musicDeath.Stop();
        _musicGameplay.Play();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_quit"))
        {
            //			GetTree().Quit();
        }
    }
}










