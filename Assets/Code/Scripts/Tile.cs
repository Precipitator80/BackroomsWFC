using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrecipitatorWFC
{
    public abstract class Tile : MonoBehaviour
    {
        private int id = -1;
        public string Id;
        public readonly GameObject Prefab;

        public Tile(GameObject prefab, string Id = "Empty")
        {
            Prefab = prefab;
            if (prefab != null && !prefab.name.Equals(string.Empty))
            {
                this.Id = prefab.name;
            }
            else
            {
                this.Id = Id;
            }
            id = Array.IndexOf(TileManager.Instance.allTiles, Prefab);
            Debug.Log("Id is " + this.Id + " / " + this.id);
        }

        public int TileID
        {
            get
            {
                if (id == -1)
                {
                    id = Array.IndexOf(TileManager.Instance.allTiles, Prefab);
                    Debug.Log("id is " + id);
                }
                return id;
            }
        }

        /*
        public override string ToString()
        {
            return Id;
        }
        */

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
            Debug.Log("Checking cardinality: y: " + (cellArc.cell2.y - cellArc.cell1.y) + ", x: " + (cellArc.cell2.x - cellArc.cell1.x));
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
            if (cellArc.cell2.x - cellArc.cell1.x == -1)
            {
                return 3;
            }
            throw new ArgumentException("Could not calculate cardinality of cell arc due to poor positioning.");
        }

        public bool supported(CellArc arc)
        {
            // EITHER
            // Check that the other cell has this tile as a possible neighbour.
            Debug.Log("Checking support of " + arc);
            foreach (Tile neighbourTile in arc.cell2.tileOptions)
            {
                Tile[] possibleNeighbours = neighbourTile.PossibleNeighbours(new CellArc(arc.cell2, arc.cell1));
                foreach (Tile tile in possibleNeighbours)
                {
                    Debug.Log("Possible neighbour: " + tile);
                }
                if (possibleNeighbours.Contains(this))
                {
                    Debug.Log(arc + " is supported");
                    return true;
                }
            }
            // OR
            // Check that this tile has a possible neighbour that is an option in the other cell.

            // CONSIDERATIONS FOR COLLAPSED VS NOT COLLAPSED? Add this into the Cardinality calculation using a CellArc?

            Debug.Log(this + " is NOT supported on " + arc);
            return false;
        }
    }
}