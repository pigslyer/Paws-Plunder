using Godot;

public class BillboardedSpatial : Spatial
{
    public override void _Process(float delta)
    {
        Camera activeCamera = GetViewport().GetCamera();

        GlobalRotation = activeCamera.GlobalRotation;

        Rotation -= Rotation.x();
    }
}
