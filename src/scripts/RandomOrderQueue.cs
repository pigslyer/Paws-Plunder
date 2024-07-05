using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PawsPlunder;

public class RandomOrderQueue<T>(RandomNumberGenerator rng)
{
    private readonly RandomNumberGenerator _rng = rng;
    private List<T> _unretrievedElements = [];
    private List<T> _retrievedElements = [];

    public RandomOrderQueue(IEnumerable<T> elements, RandomNumberGenerator rng) : this(rng)
    {
        _unretrievedElements = elements.ToList();

        _rng.Shuffle(_unretrievedElements);
    }

    public void AddElement(T element)
    {
        int index = _rng.RandiRange(0, Math.Max(_unretrievedElements.Count - 1, 0));
        _unretrievedElements.Insert(index, element);
    }   

    public void RemoveElement(T element)
    {
        if (!_unretrievedElements.Remove(element))
        {
            _retrievedElements.Remove(element);
        }

        if (_unretrievedElements.Count == 0 && _retrievedElements.Count > 0)
        {
            (_unretrievedElements, _retrievedElements) = (_retrievedElements, _unretrievedElements);
            _rng.Shuffle(_unretrievedElements);
        }
    }   

    public T? NextElement()
    {
        if (_unretrievedElements.Count == 0)
        {
            return default;
        }
        
        T nextElement = _unretrievedElements.Pop();
        _retrievedElements.Add(nextElement);

        if (_unretrievedElements.Count == 0)
        {
            (_unretrievedElements, _retrievedElements) = (_retrievedElements, _unretrievedElements);
            _rng.Shuffle(_unretrievedElements);
        }   

        return nextElement;
    }  
}
