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

    public static IEnumerable<T> RandEls<T>(this RandomNumberGenerator rng, IReadOnlyList<T> list, int count, Predicate<T> allowedElements = null)
    {
        List<int> ints = new List<int>(list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            ints.Add(i);
        }

        rng.Shuffle(ints);

        int takenElements = 0;
        int index = 0;
        while (true)
        {
            if (index >= list.Count)
            {
                break;
            }   

            if (takenElements >= count)
            {
                break;
            }

            if (allowedElements?.Invoke(list[index]) == false)
            {
                index += 1;
                continue;
            }

            yield return list[index];
            
            takenElements += 1;
            index += 1;
        }
    }

    public static int FrameCount(this AnimatedSprite3D sprite)
    {
        return sprite.Frames.GetFrameCount(sprite.Animation);
    }

    public static T Pop<T>(this List<T> list)
    {
        T element = list.Last();
        list.RemoveAt(list.Count - 1);
        return element;
    }

    public static void FadeOut(this AudioStreamPlayer player, float duration)
    {
        SceneTreeTween tween = player.CreateTween();

        tween.TweenProperty(player, "volume_db", -60, duration).SetTrans(Tween.TransitionType.Expo);
        tween.TweenCallback(player, "stop");
        tween.TweenCallback(player, "set", new Godot.Collections.Array(new object[]{"volume_db", player.VolumeDb}));
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

    public static void PlayPitched(this AudioStreamPlayer3D player, Distro distribution, RandomNumberGenerator rng = null)
    {
        if (player.Playing)
        {
            return;
        }

        if (rng == null)
        {
            rng = Globals.Rng;
        }

        player.PitchScale = rng.Randfn(distribution);
        player.Play();
    }
}
