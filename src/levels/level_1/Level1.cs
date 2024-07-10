using Godot;

namespace PawsPlunder;

public partial class Level1 : Level
{
	[Export] private Player _player = null!;
	[Export] private Path3D _catapultPath = null!;
	[Export] private PathFollow3D _catapultPathFollow = null!;
	[Export] private CatapultOverlay _catapultOverlay = null!;

	[Export] private AudioStreamPlayer _musicGameplay = null!;
	[Export] private AudioStreamPlayer _musicDeath = null!;
	[Export] private AudioStreamPlayer _windPlayer = null!;
	[Export] private AudioStreamPlayer _cannonPlayer = null!;

	public override void _Ready()
	{
		_player.LockInPlace = true;
	}

	public override void OnStart()
	{
		Logger.Debug("Level1: OnStart() called");
		// TODO: lock movement, not moving camera
		_player.LockInPlace = true;
		_musicGameplay.Play();
		_player.Initialize();
		_SpawnPlayer();		
	}

	private void _SpawnPlayer()
	{
		_player.Position = Vector3.Zero;
		_player.DoomPortrait.SetAnimation(DoomPortraitType.Flying);
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(_catapultPathFollow, "progress_ratio", 1f, 1.5)
			.From(0f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_catapultOverlay.FadeOut(ref tween);
		tween.TweenProperty(_player, nameof(_player.LockInPlace), false, 1f); // lock player input until landing
		tween.TweenCallback(Callable.From(() => _player.DoomPortrait.SetAnimation(DoomPortraitType.Idle))).SetDelay(1.5f);

		_windPlayer.Play();
		_cannonPlayer.Play();		
		_cannonPlayer.FadeOut(1.1f);

		DiscardTimer.Start(this, 1.6f).Timeout += () => {
			_windPlayer.FadeOut(0.6f);
		};
	}

	private void OnPlayerDied()
	{
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
}










