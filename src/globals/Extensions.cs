using Godot;


public static class Extensions {
    public static Vector3 xz(this Vector3 vec) => new Vector3(vec.x, 0, vec.z); 
}
