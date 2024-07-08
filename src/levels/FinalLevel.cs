using Godot;
using System;

namespace PawsPlunder;

public partial class FinalLevel : Node3D
{
	[Export] private Player _player = null!;
	[Export] private Path3D _catapultPath = null!;
	[Export] private PathFollow3D _catapultPathFollow = null!;

	[Export] private AudioStreamPlayer _musicMenu = null!;
	[Export] private AudioStreamPlayer _musicGameplay = null!;
	[Export] private AudioStreamPlayer _musicDeath = null!;
	[Export] private AudioStreamPlayer _windPlayer = null!;
	[Export] private AudioStreamPlayer _cannonPlayer = null!;

	public override void _Ready()
	{
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

		ColorRect catapultOverlay = GetNode<ColorRect>("%CatapultEffect");
		catapultOverlay.Visible = true;
		
		Tween tween = CreateTween().SetParallel();
		
		tween.TweenProperty(_catapultPathFollow, "unit_offset", 1f, 1.5)
			.From(0f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		tween.TweenProperty(catapultOverlay.Material, "shader_param/alpha", 0f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
		
		tween.TweenProperty(catapultOverlay.Material, "shader_param/inner_radius", 1f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.OutIn);
		
		tween.TweenProperty(catapultOverlay.Material, "shader_param/outer_radius", 1f, 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);

		tween.TweenProperty(_player, nameof(_player.LockInPlace), false, 1f);
		tween.TweenCallback(Callable.From(() => _player.DoomPortrait.SetAnimation(DoomPortraitType.Idle))).SetDelay(1.5f);

		
		_windPlayer.Play();
		_cannonPlayer.Play();		
		_cannonPlayer.FadeOut(1.1f);

		CustomTimer.Start(this, 1.6f).Timeout += () => {
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










