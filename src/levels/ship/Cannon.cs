using Godot;
using System;

public class Cannon : StaticBody
{
	private Spatial _arrow;
	public override void _Ready()
	{
		Spatial _arrow = GetNode<Spatial>("Arrow");
	}

	public void EnableEscape()
	{
		_arrow.Visible = true;
	}
}
