using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    [ExecuteInEditMode]
    public class TileManager : MonoBehaviour
    {
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