using System;
using System.Linq;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Tile class to represent the choices a cell in the level may have.
    /// Holds a prefab to instantiate once all choices have been made.
    /// </summary>
    public abstract class Tile : MonoBehaviour, IEquatable<Tile>
    {
        private readonly int id; // The ID of the tile. Represents all different subclasses of tiles in the tile manager.
        public readonly GameObject Prefab; // The prefab to instantiate for this tile.

        /// <summary>
        /// Constructor to initialise the tile.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate for this tile.</param>
        public Tile(GameObject prefab)
        {
            Prefab = prefab;
            id = Array.IndexOf(LevelGenerationManager.Instance.tileSet, this);
            Debug.Log("Id is " + id);
        }

        /// <summary>
        /// Returns the possible neighbour tiles of a collapsed cell in the bird's eye direction to a neighbour.
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="collapsedCell">The cell that was collapsed.</param>
        /// <param name="cardinalityToNeighbour">The direction to the neighbour in the grid from a bird's eye view. (0 = Above, 1 = Right, 2 = Below, 3 = Left)</param>
        /// <returns>The possible neighbour tiles of the collapsed cell.</returns>
        public Tile[] PossibleNeighbours(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int relativeCardinality = RelativeCardinality(collapsedCell, cardinalityToNeighbour);
            return PossibleNeighbours(relativeCardinality);
        }

        /// <summary>
        /// Returns the possible neighbour tiles of one cell to another cell on an arc.
        /// </summary>
        /// <param name="cellArc">The arc of cells to get the neighbours along.</param>
        /// <returns>The possible neighbour tiles of the first cell in the arc.</returns>
        public Tile[] PossibleNeighbours(CellArc cellArc)
        {
            int cardinality = Cardinality(cellArc);
            return PossibleNeighbours(cardinality);
        }

        /// <summary>
        /// Returns the possible neighbour tiles of this tile given a relative cardinality to another cell.
        /// </summary>
        /// <param name="relativeCardinality">The cardinality towards the neighbouring cell.</param>
        /// <returns>The possible neighbour tiles of the tile.</returns>
        protected abstract Tile[] PossibleNeighbours(int relativeCardinality);

        /// <summary>
        /// Calculates the relative direction from a collapsed cell to one of its neighbours, specified by the actual cardinality from a bird's eye view of the grid.
        /// This can be used to get the neighbours of a collapsed cell that may have been rotated, which shifts the relative direction of neighbours.
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="collapsedCell"></param>
        /// <param name="cardinalityToNeighbour"></param>
        /// <returns>The relative direction from the collapsed cell to the neighbour, taking into account the collapsed cell's rotation.</returns>
        protected int RelativeCardinality(Cell collapsedCell, int cardinalityToNeighbour)
        {
            int cellCardinalRotation = 0; // The cardinal rotation of the collapsed cell relative to the grid.
            if (collapsedCell.Collapsed)
            {
                if (collapsedCell != null && collapsedCell.transform != null && collapsedCell.transform.localEulerAngles != null)
                {
                    cellCardinalRotation = ((int)((360 + Mathf.Round(collapsedCell.transform.localEulerAngles.y)) / 90)) % 4;
                }
            }
            return Math.Abs(cellCardinalRotation - cardinalityToNeighbour - 4) % 4;
        }

        /// <summary>
        /// Checks the cardinality of a cell arc in the grid.
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="cellArc">The cell arc to check the cardinality of.</param>
        /// <returns>The direction from the first cell to the second cell as 0-3 in steps of 90 degrees.</returns>
        /// <exception cref="ArgumentException">Thrown if the cells are not adjacent. Should be impossible.</exception>
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

        /// <summary>
        /// Checks whether this tile is supported on an arc.
        /// Might require special considerations for collapsed vs non-collapsed tiles due to chosen rotations.
        /// This might require a second calculation in the CellArc cardinality calculation.
        /// </summary>
        /// <param name="arc">The arc to check.</param>
        /// <returns>Whether the tile is supported on the arc.</returns>
        public bool Supported(CellArc arc)
        {
            // EITHER
            // Check that the other cell has this tile as a possible neighbour.
            Debug.Log("Checking support of " + arc);
            foreach (Tile neighbourTile in arc.cell2.tileOptions)
            {
                Tile[] possibleNeighbours = neighbourTile.PossibleNeighbours(new CellArc(arc.cell2, arc.cell1));
                // foreach (Tile tile in possibleNeighbours)
                // {
                //     Debug.Log("Possible neighbour: " + tile);
                // }
                if (possibleNeighbours.Contains(this))
                {
                    Debug.Log(arc + " is supported");
                    return true;
                }
            }
            // OR
            // Check that this tile has a possible neighbour that is an option in the other cell.

            Debug.Log(this + " is NOT supported on " + arc);
            return false;
        }

        /// <summary>
        /// Equals function to compare two tiles.
        /// </summary>
        /// <param name="other">The other tile to check equality with.</param>
        /// <returns>True if both tiles have the same id. This represents the same type.</returns>
        public bool Equals(Tile other)
        {
            bool equality = this.id == other.id;
            Debug.Log("Does " + this.id + " equal " + other.id + "? " + equality);
            return equality;
        }
    }
}