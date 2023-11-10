using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Cube tile with equal neighbours on all sides.
    /// </summary>
    public class CubeTile : Tile
    {
        public Tile[] neighbours; // Neighbours connecting to any side of the cube.

        public CubeTile(GameObject prefab) : base(prefab) { }

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            return neighbours;
        }
    }
}