using Godot;
using System;

namespace PawsPlunder;

public partial class Cannon : StaticBody3D
{
	[Export] private Node3D _arrow = null!;

	public void EnableEscape()
	{
		_arrow.Visible = true;
	}
}
