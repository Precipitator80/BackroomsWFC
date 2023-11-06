using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrecipitatorWFC
{
    public abstract class Tile : MonoBehaviour
    {
        public readonly string Id;
        public readonly GameObject Prefab;

        public Tile(GameObject prefab)
        {
            Prefab = prefab;
            if (prefab == null)
            {
                Id = "Empty";
            }
            else
            {
                Id = prefab.name;
            }
        }

        public override string ToString()
        {
            return Id;
        }

        /**
         * collapsedCell: The cell that was collapsed.
         * cardinalityToNeighbour: The direction to the neighbour in the grid from a bird's eye view. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
         */
        public abstract Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour);

        public abstract Tile[] PossibleNeighbours(CellArc cellArc);

        /**
         * Cardinal rotation: 0-3 in steps of 90 degrees.
         * cellCardinalRotation: The cardinal rotation of the collapsed cell relative to the grid.
         * return relativeCardinality: The relative direction from the collapsed cell to the neighbour, taking into account the collapsed cell's rotation.
         */
        protected int RelativeCardinality(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int cellCardinalRotation = 0;
            if (collapsedCell.Collapsed())
            {
                if (collapsedCell != null && collapsedCell.transform != null && collapsedCell.transform.localEulerAngles != null)
                {
                    cellCardinalRotation = ((int)((360 + Mathf.Round(collapsedCell.transform.localEulerAngles.y)) / 90)) % 4;
                }
            }
            return Math.Abs(cellCardinalRotation - cardinalityToNeighbour - 4) % 4;
        }

        protected int Cardinality(CellArc cellArc)
        {
            if (cellArc.cell2.y - cellArc.cell1.y == 1)
            {
                return 0;
            }
            if (cellArc.cell2.x - cellArc.cell1.x == 1)
            {
                return 1;
            }
            if (cellArc.cell2.y - cellArc.cell1.y == -1)
            {
                return 2;
            }
            if (cellArc.cell2.y - cellArc.cell1.y == 1)
            {
                return 3;
            }
            throw new ArgumentException("Could not calculate cardinality of cell arc due to poor positioning.");
        }
    }
}