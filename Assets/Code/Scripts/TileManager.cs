using UnityEngine;

namespace PrecipitatorWFC
{
    // TODO: Comment!
    [ExecuteInEditMode]
    public class TileManager : MonoBehaviour
    {
        public static readonly int tileSize = 2;
        private static TileManager instance;
        public static TileManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("TileManager is null!");
                }
                return instance;
            }
        }

        public void Awake()
        {
            Debug.Log("TileManager is awake!");
            instance = this;
        }

        public Tile[] allTiles;
    }
}