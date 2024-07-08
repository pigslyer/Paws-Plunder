using Godot;

namespace PawsPlunder;

public struct MeleeHitInfo
{
    public Node3D Source;
}

public interface IMeleeTargettable
{
    void Target(MeleeHitInfo info);
}
