using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    public class WallTile : Tile
    {
        public Tile[] frontNeighbours;
        public Tile[] sideNeighbours;
        public Tile[] backNeighbours;

        public WallTile(GameObject prefab) : base(prefab, "Wall")
        {
        }

        public void Start()
        {
            Id = "Wall";
            name = Id;
        }

        public override Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int relativeCardinality = RelativeCardinality(collapsedCell, cardinalityToNeighbour);
            switch (relativeCardinality)
            {
                case 0:
                    return backNeighbours;
                case 2:
                    return frontNeighbours;
                default:
                    return sideNeighbours;
            }
        }

        public override Tile[] PossibleNeighbours(CellArc cellArc)
        {
            int cardinality = Cardinality(cellArc);
            switch (cardinality)
            {
                case 0:
                    return backNeighbours;
                case 2:
                    return frontNeighbours;
                default:
                    return sideNeighbours;
            }
        }
    }
}