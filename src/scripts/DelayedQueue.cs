using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PawsPlunder;

public class DelayedQueue<T>
{
    private readonly List<T> _elements = new List<T>();
    private readonly List<float> _times = new List<float>();
    private float _currentTime;

    public void AddElement(T element)
    {
        _elements.Add(element);
    }

    public IReadOnlyList<T> PopElements(float delta)
    {
        Debug($"count: {_times.Count}, target time: {_times.FirstOrDefault()}");
        _currentTime += delta;

        if (_times.Count == 0 || _times[0] > _currentTime)
        {
            return []; 
        }

        List<T> ret = new List<T>();
        int poppedCount = 0;
        while (poppedCount < _times.Count && _times[poppedCount] < _currentTime)            
        {
            ret.Add(_elements[poppedCount]);
            poppedCount += 1;
        }

        _elements.RemoveRange(0, poppedCount);
        _times.RemoveRange(0, poppedCount);

        return ret;
    }
    
    public void RedistributeOver(RandomNumberGenerator rng, float time)
    {
        _currentTime = 0.0f;
        _times.Clear();

        for (int i = 0; i < _elements.Count; i++)
        {
            _times.Add(rng.Randf() * time);
        }

        _times.Sort();
    }

    public void Clear()
    {
        _elements.Clear();
        _times.Clear();   
    }
}
