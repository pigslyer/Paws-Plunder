using Godot;
using System;

public class FinalLevel : Spatial
{
	private Player _player;
	private Path _catapultPath;
	private PathFollow _catapultPathFollow;

	public override void _Ready()
	{
		_player = GetNode<Player>("%Player");
		_catapultPath = GetNode<Path>("CatapultPath");
		_catapultPathFollow = GetNode<PathFollow>("CatapultPath/CatapultPathFollow");

		ColorRect catapultOverlay = GetNode<ColorRect>("%CatapultEffect");
		catapultOverlay.Material.Set("shader_param/alpha", 1f);
		catapultOverlay.Material.Set("shader_param/inner_radius", 0.9f);
		catapultOverlay.Material.Set("shader_param/outer_radius", 0.9f);

		_player.LockInPlace = true;
	}

	private void _on_MainMenu_StartGame()
	{
		_player.LockInPlace = true;

		GetNode<CanvasLayer>("%MainMenu").Visible = false;
		_player.Initialize();
		_SpawnPlayer();		
	}

	private void _SpawnPlayer()
	{
		//_player.ToggleGravity(false);
		_player.Translation = Vector3.Zero;
		var catapultOverlay = GetNode<ColorRect>("%CatapultEffect");
		catapultOverlay.Visible = true;
		var tween = GetNode<Tween>("%CatapultTween");
		tween.InterpolateProperty(_catapultPathFollow, "unit_offset", 0f, 1f, 1.5f, Tween.TransitionType.Quad, Tween.EaseType.Out);
		tween.InterpolateProperty(catapultOverlay.Material, "shader_param/alpha", 1f, 0f, 0.5f, Tween.TransitionType.Quad, Tween.EaseType.InOut);
		tween.InterpolateProperty(catapultOverlay.Material, "shader_param/inner_radius", 0.9f, 1f, 0.5f, Tween.TransitionType.Quad, Tween.EaseType.InOut);
		tween.InterpolateProperty(catapultOverlay.Material, "shader_param/outer_radius", 0.9f, 1f, 0.5f, Tween.TransitionType.Quad, Tween.EaseType.InOut);
		tween.InterpolateProperty(_player, "LockInPlace", true, false, 1f);
		tween.Start();
	}
	
	private void _on_Player_RespawnPlayer()
	{	
		_on_MainMenu_StartGame();
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_quit"))
		{
			GetTree().Quit();
		}
	}
}










