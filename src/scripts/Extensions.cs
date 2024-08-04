using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

namespace PawsPlunder;

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
    public static Range Circle => new(0, float.Tau);

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

public static class Extensions 
{
    public static string StrJoin<T>(this IEnumerable<T> seq, string sep = ", ")
        => string.Join(sep, seq);

    // swizzles
    public static Vector3 X0Z(this Vector3 vec) => new(vec.X, 0, vec.Z); 
    public static Vector3 X00(this Vector3 vec) => new(vec.X, 0, 0);

    /// <summary>
    /// Note: This method is a banger that makes the rest of the extensions and APIs work super well, HOWEVER.
    /// <para>NEVER USE THE RETUREND SPAN AFTER MODIFYING THE LIST IN ANY WAY BUT THE INDEXER</para>
    /// Going contrary to that will cause undefined behaviour 
    /// (you'll access the possibly outdated backing array or, heaven forefend, 
    /// garbage memory that won't be treated as out of bounds despite being out of bounds) 
    /// </summary>
    /// <param name="list"></param>
    /// <returns>The backing span of a list.</returns>
    public static Span<T> AsSpan<T>(this List<T> list)
    {
        return CollectionsMarshal.AsSpan(list);
    }

    // ripped from https://stackoverflow.com/questions/273313/randomize-a-listt [sic]
    public static void Shuffle<T>(this RandomNumberGenerator rng, Span<T> list)
    {
        int n = list.Length;  

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

    public static T RandEl<T>(this RandomNumberGenerator rng, ReadOnlySpan<T> list)
    {
        return list[rng.RandiRange(0, list.Length - 1)];
    }

    public static List<T> RandEls<T>(this RandomNumberGenerator rng, IReadOnlyList<T> elements, int count, Predicate<T>? allowedElements = null)
    {
        int[] naturals = GetNaturalNumbers(elements.Count).ToArray(); 

        rng.Shuffle(naturals.AsSpan());
        int naturalsIndex = 0;

        List<T> randomElements = [];
        while (true)
        {
            if (naturalsIndex >= naturals.Length)
            {
                break;
            }   

            if (randomElements.Count >= count)
            {
                break;
            }

            T currentElement = elements[naturals[naturalsIndex]];

            if (allowedElements?.Invoke(currentElement) == false)
            {
                naturalsIndex += 1;
                continue;
            }

            randomElements.Add(currentElement);
            naturalsIndex += 1;
        }

        return randomElements;
    }

    public static int FrameCount(this AnimatedSprite3D sprite)
    {
        return sprite.SpriteFrames.GetFrameCount(sprite.Animation);
    }

    public static T Pop<T>(this List<T> list)
    {
        T element = list.Last();
        list.RemoveAt(list.Count - 1);
        return element;
    }

    public static void FadeOut(this AudioStreamPlayer player, float duration)
    {
        float currentVolumeDb = player.VolumeDb;

        Tween tween = player.CreateTween();

        tween.TweenProperty(player, "volume_db", -60, duration).SetTrans(Tween.TransitionType.Expo);
        // is this dangerous due to GC stuff?
        tween.TweenCallback(Callable.From(() => {
            player.Stop();
            player.VolumeDb = currentVolumeDb;
        }));
    }

    public static void SetPlaying(this AudioStreamPlayer player, bool playing, float? fadeOut = null)
    {
        if (playing == player.Playing)
        {
            return;
        }

        if (playing)
        {
            player.Play();
        }
        else
        {
            if (fadeOut.HasValue)
            {
                player.FadeOut(fadeOut.Value); 
            }
            else
            {
                player.Stop();
            }
        }
    }

    public static void PlayPitched(this AudioStreamPlayer player, Distro distribution, RandomNumberGenerator? rng = null)
    {
        if (player.Playing)
        {
            return;
        }

        rng ??= Globals.Rng;

        player.PitchScale = rng.Randfn(distribution);
        player.Play();
    }

    public static void PlayPitched(this AudioStreamPlayer3D player, Distro distribution, RandomNumberGenerator? rng = null)
    {
        if (player.Playing)
        {
            return;
        }

        rng ??= Globals.Rng;

        player.PitchScale = rng.Randfn(distribution);
        player.Play();
    }
    
    private static int[] _naturalNumbers;
    
    static Extensions()
    {
        const int StoredNaturalNumbers = 1000;

        _naturalNumbers = Enumerable.Range(0, StoredNaturalNumbers).ToArray();
    }

    private static ReadOnlySpan<int> GetNaturalNumbers(int count)
    {
        if (_naturalNumbers.Length < count)
        {
            _naturalNumbers = Enumerable.Range(0, count).ToArray();
            GD.PushWarning($"Required {count} natural numbers, increase static allocation in {nameof(Extensions)}!");
        }

        return _naturalNumbers.AsSpan()[0..count];
    }

    public static void ForEach<T>(this IEnumerable<T> seq, Action<T> action)
    {
        foreach (T el in seq)
        {
            action.Invoke(el);
        }
    }
}
