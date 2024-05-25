using Unity.Entities;

public class ToEntities
{
    public static void SendMapData(MapGenOutput data)
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MapSystem>().SetMapData(data);
    }
}
