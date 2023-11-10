using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Corner tile with a diagonal symmetry giving two front and two back sides.
    /// </summary>
    public class CornerTile : Tile
    {
        public Tile[] frontNeighbours; // Neighbours connecting to the open side of the corner.
        public Tile[] backNeighbours; // Neighbours connecting behind the corner.

        public CornerTile(GameObject prefab) : base(prefab) { }

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            if (relativeCardinality < 2)
            {
                return frontNeighbours;
            }
            return backNeighbours;
        }
    }
}