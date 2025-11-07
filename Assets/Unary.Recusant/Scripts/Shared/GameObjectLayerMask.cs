namespace Unary.Recusant
{
    // Enum for code reference of game object layers
    public enum GameObjectLayerMask : int
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        LocalPlayer = 3,
        Water = 4,
        UI = 5,
        MotorMover = 6,
        ProxyPlayer = 7,
        ProxyPhysicsMover = 8,
        PhysicsObject = 9,
        Npc = 10,
    }
}
