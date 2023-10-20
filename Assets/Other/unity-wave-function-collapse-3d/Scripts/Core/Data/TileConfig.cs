using UnityEngine;

namespace Core.Data
{
    public class TileConfig
    {
        public readonly string Id;
        public readonly GameObject Prefab;

        public TileConfig(GameObject prefab)
        {
            Prefab = prefab;
            if (prefab == null)
            {
                Id = "Empty";
            }
            else
            {
                Id = prefab.name;
            }
        }

        public override string ToString()
        {
            return Id;
        }
    }
}