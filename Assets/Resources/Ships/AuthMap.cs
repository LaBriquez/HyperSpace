using Unity.Entities;
using UnityEngine;

//sert à selectionner la map dans le BotSystem et dans le PlayerSystem

public class AuthMap : MonoBehaviour
{
    public int indexMap;
}

public struct Map : IComponentData
{
    public int IndexMap;
}

public class MapBaker : Baker<AuthMap>
{
    public override void Bake(AuthMap authoring)
    {
        AddComponent(new Map
        {
            IndexMap = authoring.indexMap
        });
    }
}