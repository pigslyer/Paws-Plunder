using System.Collections.Generic;
using Godot;


public static class Extensions {
    public static Vector3 GetGlobalPosition(this Spatial spatial) => spatial.GlobalTransform.origin;
    public static void SetGlobalPosition(this Spatial spatial, Vector3 globalPosition)
    {
        Transform g = spatial.GlobalTransform;
        g.origin = globalPosition;
        spatial.GlobalTransform = g;
    }

    public static Vector3 xz(this Vector3 vec) => new Vector3(vec.x, 0, vec.z); 
    public static Vector3 x(this Vector3 vec) => new Vector3(vec.x, 0, 0); 

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
}
