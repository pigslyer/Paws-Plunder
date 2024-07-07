using Godot;
using System;

namespace PawsPlunder;
public partial class Cannon : StaticBody3D
{
	private Node3D _arrow = null!;
	public override void _Ready()
	{
		_arrow = GetNode<Node3D>("%Arrow");
	}

	public void EnableEscape()
	{
		_arrow.Visible = true;
	}
}
