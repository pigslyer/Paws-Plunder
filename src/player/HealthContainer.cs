using System;
using System.Linq;
using Godot;

namespace PawsPlunder;

public partial class HealthContainer : Control 
{
	private TextureRect[] _health = [];

	public override void _Ready()
	{
		_health = GetChildren().OfType<TextureRect>().ToArray();
	}

	public void SetHealth(int health)
	{
		health = int.Clamp(health, 0, _health.Length - 1);

		foreach (TextureRect aliveHeart in _health.AsSpan()[..health])
		{
			aliveHeart.SelfModulate = Colors.White;
		}

		foreach (TextureRect deadHeart in _health.AsSpan()[health..])
		{
			deadHeart.SelfModulate = new Color(0.2f, 0.2f, 0.2f);
		}
	}
}
