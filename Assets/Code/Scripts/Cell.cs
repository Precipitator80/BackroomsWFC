using System;
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
    public class Cell
    {
        public HashSet<Tile> tileOptions; // A set of possible tile options for the cell at any given point. Reduced to one option before collapse.
        public Tile previouslyCollapsedTile; // The tile chosen for this cell in previous generation.
        public GameObject tilePrefab; // A GameObject to hold the tile prefab once the cell has been collapsed.
        public int y; // The y coordinate of the cell in the current layer being collapsed.
        public int x; // The x coordinate of the cell in the current layer being collapsed.
        public Vector3 worldCoordinates; // The coordinates of the cell in the global grid.
        private Chunk currentChunk; // The current chunk that the cell is part of in generation. Holds the RNG for all its cells.

        /// <summary>
        /// Constructor that initialises the tile options and position variables.
        /// </summary>
        /// <param name="y">The y coordinate of the cell in the current layer being collapsed.</param>
        /// <param name="x">The x coordinate of the cell in the current layer being collapsed.</param>
        /// <param name="globalGridCoordinates">The coordinates of the cell in the global grid.</param>
        /// <param name="currentChunk">The current chunk that the cell is part of in generation. Holds the RNG for all its cells.</param>
        public Cell(int y, int x, Vector3 globalGridCoordinates, Chunk currentChunk)
        {
            tileOptions = new HashSet<Tile>(LevelGenerationManager.Instance.ExplicitTileSet);
            this.y = y;
            this.x = x;
            this.worldCoordinates = LevelGenerationManager.Instance.GridCoordinatesToWorldPos(globalGridCoordinates);
            this.currentChunk = currentChunk;
        }

        /// <summary>
        /// Updates the local coordinates and chunk of the cell to allow re-use of the cell in further generation.
        /// </summary>
        /// <param name="y">The y coordinate of the cell in the current layer being collapsed.</param>
        /// <param name="x">The x coordinate of the cell in the current layer being collapsed.</param>
        /// <param name="currentChunk"></param>
        public void updateLocalCoordinates(int y, int x, Chunk currentChunk)
        {
            this.y = y;
            this.x = x;
            this.currentChunk = currentChunk;
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
        /// <param name="layerSpawner">The layer spawner holding the current state of its generation.</param>
        /// <returns>Whether domain pruning occurred.</returns>
        public bool Assign(Tile tile, LayerSpawner layerSpawner)
        {
            // Create a copy of the tile options set that only includes tiles that are not the tile to assign.
            HashSet<Tile> tilesToRemove = new HashSet<Tile>(tileOptions.Where(otherTile => !otherTile.Equals(tile)));

            // Remove all the other tiles from the tile option set.
            bool changed = false;
            foreach (Tile otherTile in tilesToRemove)
            {
                try
                {
                    PruneDomain(otherTile, layerSpawner);
                }
                catch (EmptyDomainException)
                {
                    Debug.LogError("Domain wipeout while assigning a cell! There is likely an error in the code.");
                }
                changed = true;
            }
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("Assigned tile " + tile + ": " + this);
            }
            return changed;
        }

        /// <summary>
        /// "Unassign" a specific tile to a cell by removing it from the domain.
        /// This means that the cell is set to not equal this specific tile (right branch).
        /// </summary>
        /// <param name="tile">The tile to remove.</param>
        /// <param name="layerSpawner">The layer spawner holding the current state of its generation.</param>
        public void Unassign(Tile tile, LayerSpawner layerSpawner)
        {
            PruneDomain(tile, layerSpawner);
        }

        /// <summary>
        /// Remove / prune a specific tile from a cell's domain.
        /// </summary>
        /// <param name="tile">The tile to remove.</param>
        /// <param name="layerSpawner">The layer spawner holding the current state of its generation.</param>
        public void PruneDomain(Tile tile, LayerSpawner layerSpawner)
        {
            tileOptions.Remove(tile);
            layerSpawner.CurrentStateChanges.addDomainChange(this, tile);
            if (tileOptions.Count == 0)
            {
                throw new EmptyDomainException("Domain wipeout when pruning domain!");
            }
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
        /// Currently a random tile is chosen, but in the future weighting could be used.
        /// </summary>
        /// <returns>The chosen tile.</returns>
        public Tile SelectTile()
        {
            return SelectTileWeighted();
        }

        public Tile SelectTileWeighted()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            int weightSum = tileOptionsArray.Sum(tileOption => tileOption.weight);
            int randomWeight = currentChunk.rng.NextInt(0, weightSum);

            //Debug.Log("Selecting tile by weighting. Sum is " + weightSum);
            foreach (Tile tileOption in tileOptionsArray)
            {
                //Debug.Log("Random weight: " + randomWeight + ". TileOption weight: " + tileOption.weight);
                if (randomWeight < tileOption.weight)
                {
                    //Debug.Log("Chosen tile: " + tileOption);
                    return tileOption;
                }

                randomWeight -= tileOption.weight;
            }

            // This should never happen if the weights are configured correctly
            throw new InvalidOperationException("Weighted options are not configured correctly");
        }

        public Tile SelectTileRandomChoice()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            return tileOptionsArray[currentChunk.rng.NextInt(0, tileOptionsArray.Length)];
        }

        public Tile SelectTileAscending()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            return tileOptionsArray[0];
        }

        /// <summary>
        /// Collapses the cell, instantiating the remaining tile option as the tile prefab.
        /// </summary>
        /// <exception cref="EmptyDomainException">Thrown when there is no tile option to use. Should never be thrown when collapsed after successful level generation.</exception>
        /// <exception cref="System.Exception">Thrown when there is more than one tile option left to use. Should never be thrown when collapsed after successful level generation.</exception>
        public void Collapse()
        {
            // Check for a contradiction. Should never happen if this is called only after successful level generation.
            if (EmptyDomain)
            {
                throw new EmptyDomainException("Domain was empty when attempting to collapse a cell!");
            }
            else if (!Assigned)
            {
                throw new System.Exception("Cell collapse was called when there was more than one tile option left!");
            }

            // Instantiate the tile prefab in the cell.
            Tile tileOption = tileOptions.ToArray()[0];
            tileOption.CollapseIntoCell(this);

            // // Use one of the possible rotations.
            // float randomRotation = 90f * tileOption.possibleCardinalities[currentChunk.rng.NextInt(0, tileOption.possibleCardinalities.Count)];
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