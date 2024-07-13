using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PawsPlunder;

public class DelayedQueue<T>
{
    private readonly List<T> _elements = [];
    private float[] _times = [];
    private float _currentTime;
    private int _startingIndex;

    public void AddElement(T element)
    {
        _elements.Add(element);    
    }

    public Span<T> PopElements(float delta)
    {
        _currentTime += delta;

        int nextTimeIndex = _times.AsSpan()[_startingIndex..].BinarySearch(_currentTime);

        if (~nextTimeIndex == _times.Length)
        {
            _startingIndex = _times.Length;
            return [];
        }

        if (nextTimeIndex < 0)
        {
            nextTimeIndex = ~nextTimeIndex;            
        }

        Span<T> slice = _elements.AsSpan()[_startingIndex..(_startingIndex + nextTimeIndex)];
        _startingIndex = nextTimeIndex;        

        return slice;
    }
    
    public void RedistributeOver(RandomNumberGenerator rng, float time)
    {
        _elements.RemoveRange(0, _startingIndex);
        _times = Enumerable.Range(0, _elements.Count)
            .Select(_ => rng.Randf() * time)
            .Order()
            .ToArray();

        _currentTime = 0;
        _startingIndex = 0;
    }

    public void Clear()
    {
        _elements.Clear();
        _times = [];
        _startingIndex = 0;
        _currentTime = 0;
    }
}
