using System;
using System.Collections.Generic;
using Godot;

namespace PawsPlunder;

public class DelayedQueue<T>
{
    private readonly List<T> _elements = [];
    private readonly List<float> _times = [];
    private float _currentTime;
    private int _startingIndex;

    public void AddElement(T element)
    {
        _elements.Add(element);
    }

    public Span<T> PopElements(float delta)
    {
        _currentTime += delta;
        int startIndex = _startingIndex;

        int lastIndex = _startingIndex;

        while (true)
        {
            if (!(lastIndex < _times.Count))
            {
                break;
            }

            if (!(_currentTime < _times[lastIndex]))
            {
                break;
            }

            lastIndex += 1;
        }

        _startingIndex = lastIndex;
        return _elements.AsSpan()[startIndex..lastIndex];
    }
    
    public void RedistributeOver(RandomNumberGenerator rng, float time)
    {
        _currentTime = 0;

        _elements.RemoveRange(0, _startingIndex);
        _times.Clear();

        for (int i = 0; i < _elements.Count; i++)
        {
            _times.Add(rng.Randf() * time);
        }
        
        rng.Shuffle(_times.AsSpan());
    }

    public void Clear()
    {
        _elements.Clear();
        _times.Clear();   
    }
}
