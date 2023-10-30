using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
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
    }
}