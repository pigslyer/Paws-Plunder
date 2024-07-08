using Godot;
using System;

namespace PawsPlunder;

public enum DoomPortraitType
{
	Idle,
	Treasure,
	Pain,
	Death,
	Flying
}

// reason for this class: animated sprites don't fit into controls well,
// rather just set it up manually
// this also makes it easier to fine tune animations compared to adding 4 of the same frame
public partial class DoomPortrait : Node
{
	private static readonly (int frame, float secondsPerFrame, int yOffset)[][] _animations = [
		// Idle
		[
			(1, 0.5f, 0), (2, 0.3f, 0), (1, 0.5f, 0), (0, 0.3f, 0),
		],
		// Treasure
		[
			(3, 0.2f, 0), (4, 0.2f, 0), (5, 0.2f, 0),
		],
		// Pain
		[
			(6, 0.3f, 0), (-1, 0.3f, 0),
		],
		// Death
		[
			(7, 2.0f, 0), (-1, 2.0f, 0),
		],
		// Flying
		[
			(9, 0.1f, 0), (10, 0.1f, -1), (11, 0.1f, 0),
		],

	];

	private static readonly Vector2 Seperation = new(4, 5);
	private static readonly Vector2 FrameSize = new(49, 46);
	private const int FramesPerRow = 3;

	[Export] private DoomPortraitType _activePortrait = DoomPortraitType.Idle;
	private int _currentIndex;
	private float _timer;

	[Export] private AtlasTexture _portraitAtlas = null!;

	public override void _Ready()
	{
		UpdateAtlas();
	}

	public void SetAnimation(DoomPortraitType type)
	{
		if (_activePortrait == type)
		{
			return;
		}

		_activePortrait = type;

		(_currentIndex, _timer) = (0, 0);
		UpdateAtlas();
	}

	public override void _Process(double delta)
	{
		float fDelta = (float)delta;
		(int frame, float secondsPerFrame, int yOffset)[] activeAnimation = _animations[(int)_activePortrait];

		_timer += fDelta;

		bool didAnimationUpdate = false;

		while (activeAnimation[_currentIndex].secondsPerFrame < _timer)
		{
			_timer -= activeAnimation[_currentIndex].secondsPerFrame;
			_currentIndex = (_currentIndex + 1) % activeAnimation.Length;
			didAnimationUpdate = true;
		}

		if (didAnimationUpdate)
		{
			UpdateAtlas();
		}
	}

	private void UpdateAtlas()
	{
		(int frame, _, int yOffset) = _animations[(int)_activePortrait][_currentIndex];

		if (frame < 0)
		{
			_portraitAtlas.Region = new Rect2(0, 0, 0, 0);
		}
		else
		{
			int row = frame % FramesPerRow;
			int column = frame / FramesPerRow;

			Vector2 position = Seperation + (Seperation + FrameSize) * new Vector2(row, column) + new Vector2(0, yOffset);

			_portraitAtlas.Region = new Rect2(position, FrameSize);
		}
	}
}
