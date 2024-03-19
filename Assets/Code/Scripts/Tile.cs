using System;
using System.Collections.Generic;
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
        public GameObject model; // The game object placed in the level when collapsing the tile. Kept separately to allow for easy interchanging of models.
        public int weight; // The global weight of the tile. Adjusts the likelihood of picking this tile among other choices.

        private NonSymmetricTile[] explicitVariants; // An array used to store explicit tile variants that represent each possible rotation of a tile.
        // Use a getter and setter to keep the array hidden from the editor.
        public NonSymmetricTile[] ExplicitVariants
        {
            get
            {
                return explicitVariants;
            }
            set
            {
                explicitVariants = value;
            }
        }

        /// <summary>
        /// Returns the possible neighbour tiles of this tile given a relative cardinality to another cell.
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="relativeCardinality">The cardinality towards the neighbouring cell.</param>
        /// <returns>The possible neighbour tiles of the tile.</returns>
        protected abstract Tile[] PossibleNeighbours(int relativeCardinality);

        /// <summary>
        /// Creates a copy of the tile with neighbour data given as a non-symmetric tile.
        /// This is used as a base to create rotation variants for tiles to compose a fully explicit tile set.
        /// </summary>
        /// <returns>A non-symmetric copy of the tile.</returns>
        public abstract NonSymmetricTile InstantiateNonSymmetricTile();

        /// <summary>
        /// Calculates the relative direction from a cell to one of its neighbours, specified by the actual cardinality from a bird's eye view of the grid.
        /// This can be used to get the neighbours of a collapsed cell that may have been rotated, which shifts the relative direction of neighbours.
        /// !! This is not required when using an explicit tile set. Instead of checking tile rotation, each possible rotation is given its own tile with adjusted neighbours. !!
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="cellArc">The cell arc to check the relative cardinality of.</param>
        /// <returns>The relative direction from the collapsed cell to the neighbour, taking into account the collapsed cell's rotation.</returns>
        // protected int RelativeCardinality(CellArc cellArc)
        // {
        //     int cardinalityToNeighbour = Cardinality(cellArc);
        //     int relativeCardinality = cardinalityToNeighbour;
        //     if (cellArc.cell1.Collapsed)
        //     {
        //         int cellCardinalRotation = ((int)((360 + Mathf.Round(cellArc.cell1.tilePrefab.transform.localEulerAngles.y)) / 90)) % 4;
        //         relativeCardinality = Math.Abs(cellCardinalRotation - cardinalityToNeighbour - 4) % 4;
        //     }
        //     if (LevelGenerationManager.Instance.debugMode)
        //     {
        //         Debug.Log("Relative cardinality = " + relativeCardinality);
        //     }
        //     return relativeCardinality;
        // }

        /// <summary>
        /// Checks the cardinality of a cell arc in the grid.
        /// Cardinal rotation: 0-3 in steps of 90 degrees. (0 = Above, 1 = Right, 2 = Below, 3 = Left)
        /// </summary>
        /// <param name="cellArc">The cell arc to check the cardinality of.</param>
        /// <returns>The direction from the first cell to the second cell as 0-3 in steps of 90 degrees.</returns>
        /// <exception cref="ArgumentException">Thrown if the cells are not adjacent. Should be impossible.</exception>
        protected int Cardinality(CellArc cellArc)
        {
            string debugMessage = string.Empty;
            if (LevelGenerationManager.Instance.debugMode)
            {
                debugMessage = "Checking cardinality: y: " + (cellArc.cell2.y - cellArc.cell1.y) + ", x: " + (cellArc.cell2.x - cellArc.cell1.x);
            }
            if (cellArc.cell2.y - cellArc.cell1.y == 1)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log(debugMessage + ". Cardinality = 0.");
                }
                return 0;
            }
            if (cellArc.cell2.x - cellArc.cell1.x == 1)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log(debugMessage + ". Cardinality = 1.");
                }
                return 1;
            }
            if (cellArc.cell2.y - cellArc.cell1.y == -1)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log(debugMessage + ". Cardinality = 2.");
                }
                return 2;
            }
            if (cellArc.cell2.x - cellArc.cell1.x == -1)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log(debugMessage + ". Cardinality = 3.");
                }
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
            ////// EITHER
            //// Check that the other cell has this tile as a possible neighbour.
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("Checking support of " + this + " on " + arc);
            }
            // Calculate the relative cardinality from the neighbour to the cell it should support.
            int relativeCardinality = Cardinality(new CellArc(arc.cell2, arc.cell1)); // Assumes fully explicit tile set! Use RelativeCardinality instead if this is not the case.
            foreach (Tile neighbourTile in arc.cell2.tileOptions)
            {
                Tile[] possibleNeighbours = neighbourTile.PossibleNeighbours(relativeCardinality);
                if (possibleNeighbours.Contains(this))
                {
                    if (LevelGenerationManager.Instance.debugMode)
                    {
                        Debug.Log(this + " is supported on " + arc + "(2)");
                    }
                    return true;
                }
            }
            ////// OR
            //// Check that this tile has a possible neighbour that is an option in the other cell. (Only the first option is implemented.)

            // Return false if support could not be found.
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log(this + " is NOT supported on " + arc);
            }
            return false;
        }

        /// <summary>
        /// Equals function to compare two tiles.
        /// </summary>
        /// <param name="other">The other tile to check equality with.</param>
        /// <returns>True if both tiles have the same id. This represents the same type.</returns>
        /// Debugging code:
        /// bool identityEquality = this == other; // Does not work when multiple instances are used.
        /// bool idEquality = this.id == other.id; // Must be declared manually for each tile.
        /// bool typeEquality = this.GetType() == other.GetType(); // Does not work for two different tiles using the same symmetry subclass.
        /// Debug.Log("Does tile " + this + " / " + this.id + " / " + this.GetType() + " equal tile " + other + " / " + other.id + " / " + other.GetType() + "? " + identityEquality + " / " + idEquality + " / " + typeEquality);
        public bool Equals(Tile other)
        {
            return this.name == other.name;
        }

        /// <summary>
        /// Creates a copy of this tile and places it into a given cell.
        /// </summary>
        /// <param name="cell">The cell to place the tile prefab in.</param>
        public void CollapseIntoCell(Cell cell)
        {
            // Only collapse if a model is available.
            if (this.model == null)
            {
                throw new Exception("The tile '" + this + "' does not have a model available to allow for collapse!");
            }

            // Destroy the previous tilePrefab if the cell was replaced by a higher layer.
            if (cell.tilePrefab != null)
            {
                DestroyImmediate(cell.tilePrefab);
            }

            // Instantiate the prefab at following the transform of the tile to preserve scaling and rotation.
            cell.tilePrefab = Instantiate(this.model, this.transform);

            // Give it the name of the tile rather than the model.
            cell.tilePrefab.name = this.name;

            // Add a reference to the cell the model is in.
            CellReference cellReference = cell.tilePrefab.AddComponent<CellReference>();
            cellReference.cell = cell;

            // Change the tile's parent to the level generation manager, keeping the same initial scaling and rotation but aligning the position with the grid.
            cell.tilePrefab.transform.SetParent(LevelGenerationManager.Instance.CellParent.transform, true);
            cell.tilePrefab.transform.localPosition = cell.worldCoordinates;

            // Give each tile a box collider to allow for detection in generation.
            BoxCollider collider = cell.tilePrefab.AddComponent<BoxCollider>();
            Vector3 originalSize = new Vector3(LevelGenerationManager.Instance.tileSize / (2f * cell.tilePrefab.transform.localScale.x), LevelGenerationManager.Instance.tileSize / (2f * cell.tilePrefab.transform.localScale.y), LevelGenerationManager.Instance.tileSize / (2f * cell.tilePrefab.transform.localScale.z));
            Vector3 rotatedSize = cell.tilePrefab.transform.rotation * originalSize;
            collider.size = rotatedSize;
            collider.isTrigger = true;

            FloorSpawner floorSpawner = cell.tilePrefab.GetComponent<FloorSpawner>();
            if (floorSpawner != null)
            {
                Debug.Log("Spawned floor at collapse " + floorSpawner.transform.position);
                floorSpawner.SpawnFloor();
            }
            else
            {
                Debug.Log("Floor spawner is null for tile " + cell.tilePrefab);
            }

            // Add collider information to the debug gizmos.
            LevelGenerationManager.Instance.gizmosPosAndSize.AddLast(new LinkedListNode<(Vector3, Vector3)>((collider.transform.position, collider.size)));

            // Set the previous tile of the cell so that it can be recovered if a layer fails to generate on top of the cell.
            cell.previouslyCollapsedTile = this;

            // Activate the tile prefab in case the models are switched off.
            cell.tilePrefab.SetActive(true);
        }
    }
}