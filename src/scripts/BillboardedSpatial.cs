using Godot;

namespace PawsPlunder;

public partial class BillboardedSpatial : Node3D
{

    public override void _Process(double delta)
    {
        Camera3D activeCamera = GetViewport().GetCamera3D();

        GlobalRotation = activeCamera.GlobalRotation;

        Rotation -= Rotation.X00();
    }
}
