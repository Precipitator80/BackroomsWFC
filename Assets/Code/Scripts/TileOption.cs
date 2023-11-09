using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    // TODO: Comment / Delete / Rework into LG.
    public class TileOption : MonoBehaviour
    {
        public TileOption(Tile tile)
        {
            this.tile = tile;
        }

        public Tile tile;
        public List<int> possibleCardinalities = new List<int>() { 0, 1, 2, 3 };
    }
}