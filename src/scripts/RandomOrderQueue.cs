using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RandomOrderQueue<T>
{
    private readonly RandomNumberGenerator _rng;
    private List<T> _unretrievedElements = new List<T>();
    private List<T> _retrievedElements = new List<T>();

    public RandomOrderQueue(RandomNumberGenerator rng)
    {
        _rng = rng;
    }

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

    public T NextElement()
    {
        if (_unretrievedElements.Count == 0)
        {
            return default;
        }
        
        T returnedElement = _unretrievedElements.Last();
        _unretrievedElements.RemoveAt(_unretrievedElements.Count - 1);
        _retrievedElements.Add(returnedElement);

        if (_unretrievedElements.Count == 0)
        {
            (_unretrievedElements, _retrievedElements) = (_retrievedElements, _unretrievedElements);
            _rng.Shuffle(_unretrievedElements);
        }   

        return returnedElement;
    }  
}
