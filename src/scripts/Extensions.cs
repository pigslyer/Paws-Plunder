using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public struct Distro
{
    public float Mean;
    public float Deviation;

    public Distro(float mean, float deviation)
    {
        (Mean, Deviation) = (mean, deviation);
    }

    public static implicit operator Distro((float mean, float deviation) vals)
    {
        return new Distro(vals.mean, vals.deviation);
    }
}

public struct Range
{
    public static Range Circle => new Range(0, Mathf.Tau);

    public float Min;
    public float Max;

    public Range(float min, float max)
    {
        (Min, Max) = (min, max);
    }

    public static implicit operator Range((float min, float max) vals)
    {
        return new Range(vals.min, vals.max);
    }
}

public static class Extensions {
    public static Vector3 x0z(this Vector3 vec) => new Vector3(vec.x, 0, vec.z); 
    public static Vector3 x00(this Vector3 vec) => new Vector3(vec.x, 0, 0); 
    public static float Sqr(this float f) => f * f;
    public static List<V> GetList<K, V>(this Dictionary<K, List<V>> dic, K key) 
    {
        if (dic.TryGetValue(key, out var existing))
        {
            return existing;
        }

        List<V> values = new List<V>();
        dic[key] = values;

        return values;
    }

    // ripped from https://stackoverflow.com/questions/273313/randomize-a-listt
    public static void Shuffle<T>(this RandomNumberGenerator rng, List<T> list)
    {
        int n = list.Count;  

        while (n > 1) {  
            n -= 1;  
            int k = rng.RandiRange(0, n - 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }

    public static float Randfn(this RandomNumberGenerator rng, Distro distro)
    {
        return rng.Randfn(distro.Mean, distro.Deviation);
    }

    public static float RandfRange(this RandomNumberGenerator rng, Range range)
    {
        return rng.RandfRange(range.Min, range.Max);
    }

    public static T RandEl<T>(this RandomNumberGenerator rng, IReadOnlyList<T> list)
    {
        return list[rng.RandiRange(0, list.Count - 1)];
    }

    public static IEnumerable<T> RandEls<T>(this RandomNumberGenerator rng, IReadOnlyList<T> list, int count)
    {
        List<int> ints = new List<int>(list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            ints.Add(i);
        }

        rng.Shuffle(ints);

        for (int i = 0; i < Math.Min(count, list.Count); i++)
        {
            yield return list[ints[i]];
        }
    }
    
    public static Vector3 DirectionTo(this Vector3 fromPoint, Vector3 toPoint)
    {
        return (toPoint - fromPoint).Normalized();
    }

    public static int FrameCount(this AnimatedSprite3D sprite)
    {
        return sprite.Frames.GetFrameCount(sprite.Animation);
    }
}
