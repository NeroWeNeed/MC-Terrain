using NeroWeNeed.Terrain;
using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public sealed class RuntimeTerrainSettingsInitializationSystem : SystemBase
{
    private EndInitializationEntityCommandBufferSystem entityCommandBufferSystem;
    protected override void OnCreate()
    {
        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        base.OnCreate();
    }
    protected override void OnUpdate()
    {
        var ecb = entityCommandBufferSystem.CreateCommandBuffer();
        Entities.WithAll<TerrainSettings>().WithNone<RuntimeTerrainSettings>().ForEach((Entity entity, in TerrainSettings storageSettings) =>
        {
            var blob = RuntimeTerrainSettingsData.Create(storageSettings.value);
            ecb.AddComponent(entity, new RuntimeTerrainSettings
            {
                value = blob
            });

        }).WithoutBurst().Run();
        Entities.WithAll<RuntimeTerrainSettings>().WithNone<TerrainSettings>().ForEach((Entity entity, in RuntimeTerrainSettings settings) =>
        {
            ecb.RemoveComponent<RuntimeTerrainSettings>(entity);
        }).WithoutBurst().Run();
    }
    protected override void OnDestroy()
    {
        Entities.WithAll<RuntimeTerrainSettings>().ForEach((Entity entity, in RuntimeTerrainSettings settings) =>
        {
            EntityManager.RemoveComponent<RuntimeTerrainSettings>(entity);
        }).WithStructuralChanges().Run();
    }
}