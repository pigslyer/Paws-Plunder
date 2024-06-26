using Godot;

public struct MeleeTargetInfo
{
    public Spatial Source;
}

public interface IMeleeTargettable
{
    void Target(MeleeTargetInfo info);
}
