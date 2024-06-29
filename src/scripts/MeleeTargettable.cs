using Godot;

public struct MeleeHitInfo
{
    public Spatial Source;
}

public interface IMeleeTargettable
{
    void Target(MeleeHitInfo info);
}
