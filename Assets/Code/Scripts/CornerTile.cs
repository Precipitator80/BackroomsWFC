using UnityEngine;

namespace PrecipitatorWFC
{
    // TODO: Comment!
    public class CornerTile : Tile
    {
        public Tile[] frontNeighbours;
        public Tile[] backNeighbours;

        public CornerTile(GameObject prefab) : base(prefab)
        {
        }

        public override Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int relativeCardinality = RelativeCardinality(collapsedCell, cardinalityToNeighbour);
            if (relativeCardinality < 2)
            {
                return frontNeighbours;
            }
            return backNeighbours;
        }

        public override Tile[] PossibleNeighbours(CellArc cellArc)
        {
            int cardinality = Cardinality(cellArc);
            if (cardinality < 2)
            {
                return frontNeighbours;
            }
            return backNeighbours;
        }
    }
}