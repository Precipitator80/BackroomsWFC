using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    public class ThreeWayTile : Tile
    {
        public Tile[] outsideNeighbours;
        public Tile[] backNeighbours;

        public ThreeWayTile(GameObject prefab) : base(prefab)
        {
        }

        public override Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int relativeCardinality = RelativeCardinality(collapsedCell, cardinalityToNeighbour);
            if (relativeCardinality == 0)
            {
                return backNeighbours;
            }
            return outsideNeighbours;
        }

        public override Tile[] PossibleNeighbours(CellArc cellArc)
        {
            int cardinality = Cardinality(cellArc);
            if (cardinality == 0)
            {
                return backNeighbours;
            }
            return outsideNeighbours;
        }
    }
}