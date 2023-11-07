using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    public class CubeTile : Tile
    {
        public Tile[] neighbours;

        public CubeTile(GameObject prefab) : base(prefab, "Cube")
        {
        }

        public void Start()
        {
            Id = "Cube";
            name = Id;
        }

        public override Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour)
        {
            return neighbours;
        }

        public override Tile[] PossibleNeighbours(CellArc cellArc)
        {
            return neighbours;
        }
    }
}