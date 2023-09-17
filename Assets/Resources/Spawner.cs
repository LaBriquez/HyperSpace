using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct Config : IComponentData
{
    public Entity Prefab, PrefabPlayer, Prefab2, PrefabPlayer2;
}

public class Spawner : MonoBehaviour
{
    public GameObject prefab, prefabPlayer, prefab2, prefabPlayer2;
}

public class ConfigBaker : Baker<Spawner>
{
    public override void Bake(Spawner authoring)
    {
        AddComponent(new Config
        {
            Prefab = GetEntity(authoring.prefab),
            Prefab2 = GetEntity(authoring.prefab2),
            PrefabPlayer2 = GetEntity(authoring.prefabPlayer2),
            PrefabPlayer = GetEntity(authoring.prefabPlayer),
        });
    }
}

public partial struct ShipSpawningSystem : ISystem
{
    static bool isAwake = false;
    
    public static void Activate()
    {
        isAwake = true;
    }
    
    void OnCreate(ref SystemState state) {}
    void OnDestroy(ref SystemState state) {}

    public void OnUpdate(ref SystemState state)
    {
        // pour entrer dans la game
        if (!isAwake) return;

        if (!SystemAPI.HasSingleton<Config>())
            return;

        Config config = SystemAPI.GetSingleton<Config>();

        int index = 1;
        int ShipCount = GameSetting.shipCount1 + GameSetting.shipCount2;
        Game.mainGame.SetMaxEnnemies(ShipCount);

        isAwake = false;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // spawn du player
        ecb.SetComponent(ecb.Instantiate(GameSetting.shipIndex == 0? config.PrefabPlayer : config.PrefabPlayer2), new LocalTransform
        {
            Position = new float3(0, 0, 0),
            Rotation = quaternion.Euler(0, 0, 0),
            Scale = 1
        });

        int length = (int) math.pow(ShipCount, 1.0f / 3.0f);
        int space = 20;

        // spawn des bot (des 2 type de vaisseaux)
        for (int i = 0; i < GameSetting.shipCount1; i++)
        {
            var entity = ecb.Instantiate(config.Prefab);

            ecb.SetComponent(entity, new LocalTransform
            {
                Position = new float3(index % length * space, (index / length) % length * space, index / (length * length) * space),
                Rotation = quaternion.Euler(0, 0, 0),
                Scale = 1
            });
            index++;
        }
        
        for (int i = 0; i < GameSetting.shipCount2; i++)
        {
            var entity = ecb.Instantiate(config.Prefab2);

            ecb.SetComponent(entity, new LocalTransform
            {
                Position = new float3(index % length * space, (index / length) % length * space, index / (length * length) * space),
                Rotation = quaternion.Euler(0, 0, 0),
                Scale = 1
            });
            index++;
        }
    }
}