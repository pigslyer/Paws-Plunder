using Godot;
using System;

public class TwoShips : Spatial
{
	private Player _player;
	private PathFollow _catapultPathFollow;
	public override void _Ready()
	{
		_player = GetNode<Player>("Level/Player");
		_catapultPathFollow = GetNode<PathFollow>("Level/CatapultPath/CatapultPathFollow");
	}

	public override void _Process(float delta)
	{
	}
	
	private void _on_MainMenu_StartGame()
	{
		_player.Initialize();
		_SpawnPlayer();		
	}

	private void _SpawnPlayer()
	{
		GetNode<Spatial>("Level").RemoveChild(_player);
		_catapultPathFollow.AddChild(_player);
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_quit"))
		{
			GetTree().Quit();
		}
	}
}


