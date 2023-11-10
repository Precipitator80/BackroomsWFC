using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// A cell in the level's grid that can be collapsed to hold a tile prefab.
    /// Starts with all possible tile options, which are gradually removed during search.
    /// </summary>
    [ExecuteInEditMode]
    public class Cell : MonoBehaviour
    {
        public HashSet<Tile> tileOptions; // A set of possible tile options for the cell at any given point. Reduced to one option before collapse.
        protected GameObject tilePrefab; // A GameObject to hold the tile prefab once the cell has been collapsed.
        public int y; // The y coordinate of the cell in the grid.
        public int x; // The x coordinate of the cell in the grid.

        /// <summary>
        /// Constructor that initialises the tile options and position variables.
        /// </summary>
        /// <param name="y">The y coordinate of the cell in the grid.</param>
        /// <param name="x">The x coordinate of the cell in the grid.</param>
        public Cell(int y, int x)
        {
            tileOptions = new HashSet<Tile>(LevelGenerationManager.Instance.tileSet);
            this.y = y;
            this.x = x;
        }

        /// <summary>
        /// Awake method that moves the cell to the right position in the grid.
        /// </summary>
        public void Awake()
        {
            transform.localPosition = new Vector3(LevelGenerationManager.Instance.tileSize * x, 0f, LevelGenerationManager.Instance.tileSize * y);
        }

        /// <summary>
        /// Property to indicate whether the cell's domain is empty or not.
        /// </summary>
        public bool EmptyDomain
        {
            get
            {
                return tileOptions.Count == 0;
            }
        }

        /// <summary>
        /// Property to indicate whether the cell has been assigned a tile or not.
        /// Could also be from propagation leaving one choice rather than an explicit assignment.
        /// </summary>
        public bool Assigned
        {
            get
            {
                return tileOptions.Count == 1;
            }
        }

        /// <summary>
        /// Property to indicate whether the cell has been collapsed and assigned a prefab in the game world.
        /// </summary>
        public bool Collapsed
        {
            get
            {
                return tilePrefab != null;
            }
        }

        /// <summary>
        /// Assign a specific tile to a cell by removing all other tiles from the domain.
        /// This means that the cell is set to equal this specific tile (left branch).
        /// </summary>
        /// <param name="tile">The tile to assign the cell to.</param>
        /// <returns>Whether domain pruning occurred.</returns>
        public bool Assign(Tile tile)
        {
            // Create a copy of the tile options set that only includes tiles that are not the tile to assign.
            HashSet<Tile> tilesToRemove = new HashSet<Tile>(tileOptions.Where(otherTile => otherTile != tile));

            // Remove all the other tiles from the tile option set.
            bool changed = false;
            foreach (Tile otherTile in tilesToRemove)
            {
                if (PruneDomain(otherTile))
                {
                    changed = true;
                }
            }
            Debug.Log("Assigned tile " + tile + ": " + this);
            return changed;
        }

        /// <summary>
        /// "Unassign" a specific tile to a cell by removing it from the domain.
        /// This means that the cell is set to not equal this specific tile (right branch).
        /// </summary>
        /// <param name="tile">The tile to remove.</param>
        /// <returns>Whether the tile was removed / pruned successfully.</returns>
        public bool Unassign(Tile tile)
        {
            return PruneDomain(tile);
        }

        /// <summary>
        /// Remove / prune a specific tile from a cell's domain.
        /// </summary>
        /// <param name="tile">The tile to remove.</param>
        /// <returns>Whether the tile was removed / pruned successfully.</returns>
        public bool PruneDomain(Tile tile)
        {
            bool removed = tileOptions.Remove(tile);
            if (removed)
            {
                LevelGenerationManager.Instance.CurrentStateChanges.addDomainChange(this, tile);
            }
            return removed;
        }

        /// <summary>
        /// Readds a tile to the domain of a cell.
        /// </summary>
        /// <param name="tile">The tile to put back into the domain.</param>
        /// <returns>Whether the domain was restored successfully.</returns>
        public bool RestoreDomain(Tile tile)
        {
            return tileOptions.Add(tile);
        }

        /// <summary>
        /// Selects a tile in the domain of the cell.
        /// Currently the first tile is chosen, but in the future weighting could be used.
        /// </summary>
        /// <returns>The chosen tile.</returns>
        public Tile SelectTile()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            return tileOptionsArray[UnityEngine.Random.Range(0, tileOptionsArray.Length)];
        }

        /// <summary>
        /// Collapses the cell, instantiating the remaining tile option as the tile prefab.
        /// </summary>
        /// <exception cref="System.Exception">Thrown when there is no tile option to use. Should never be thrown when collapsed after successful level generation.</exception>
        private void Collapse()
        {
            // Check for a contradiction. Should never happen if this is called only after successful level generation.
            if (tileOptions.Count == 0)
            {
                throw new EmptyDomainException("Domain was empty when attempting to collapse a cell!");
            }

            // Instantiate the tile prefab in the cell.
            Tile tileOption = tileOptions.ToArray()[0];
            tilePrefab = Instantiate(tileOption.Prefab, this.transform);

            // // Use one of the possible rotations.
            // float randomRotation = 90f * tileOption.possibleCardinalities[Random.Range(0, tileOption.possibleCardinalities.Count)];
            // collapsedCell.transform.Rotate(0f, randomRotation, 0f);
        }

        /// <summary>
        /// Function to print cell information.
        /// </summary>
        /// <returns>A string of cell information.</returns>
        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }
}