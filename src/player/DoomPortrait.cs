using Godot;
using System;

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
public class DoomPortrait : Node
{
    private static readonly (int frame, float secondsPerFrame, int yOffset)[][] _animations = {
        // Idle
        new (int, float, int)[] {
            (1, 0.5f, 0), (2, 0.3f, 0), (1, 0.5f, 0), (0, 0.3f, 0),
        },
        // Treasure
        new (int, float, int)[] {
            (3, 0.2f, 0), (4, 0.2f, 0), (5, 0.2f, 0),
        },
        // Pain
        new (int, float, int)[] {
            (6, 0.3f, 0), (-1, 0.3f, 0),
        },
        // Death
        new (int, float, int)[] {
            (7, 0.5f, 0), (-1, 0.5f, 0),
        },
        // Flying
        new (int, float, int)[] {
            (9, 0.1f, 0), (10, 0.1f, -1), (11, 0.1f, 0),
        },

    };

    private static readonly Vector2 Seperation = new Vector2(4, 5);
    private static readonly Vector2 FrameSize = new Vector2(49, 46);
    private const int FramesPerRow = 3;

    private AtlasTexture _portraitAtlas;
    [Export] private DoomPortraitType _activePortrait = DoomPortraitType.Idle;
    private int _currentIndex;
    private float _timer;

    public override void _Ready()
    {
        _portraitAtlas = (AtlasTexture)GetNode<TextureRect>("MarginContainer/TextureRect").Texture;

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

    public override void _Process(float delta)
    {
        (int frame, float secondsPerFrame, int yOffset)[] activeAnimation = _animations[(int)_activePortrait];

        _timer += delta;

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
