using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace PrecipitatorWFC
{
    // TODO: Comment and clean!
    [ExecuteInEditMode]
    public class Cell : MonoBehaviour
    {
        public HashSet<Tile> tileOptions;
        protected GameObject tilePrefab;
        public int y; // The y coordinate of the cell in the grid.
        public int x; // The x coordinate of the cell in the grid.

        public Cell(int y, int x)
        {
            tileOptions = new HashSet<Tile>(TileManager.Instance.allTiles);
            this.y = y;
            this.x = x;
        }

        public void Awake()
        {
            transform.localPosition = new Vector3(TileManager.tileSize * x, 0f, TileManager.tileSize * y);
        }

        public bool Collapsed
        {
            get
            {
                return tilePrefab != null;
            }
        }

        public bool EmptyDomain
        {
            get
            {
                return tileOptions.Count == 0;
            }
        }

        public bool Assign(Tile tile)
        {
            bool changed = false;
            HashSet<Tile> tilesToRemove = new HashSet<Tile>(tileOptions.Where(otherTile => otherTile != tile));
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

        public bool Unassign(Tile tile)
        {
            return PruneDomain(tile);
        }

        public bool PruneDomain(Tile tile)
        {
            bool removed = tileOptions.Remove(tile);
            if (removed)
            {
                LevelGenerationManager.Instance.CurrentStateChanges.addDomainChange(this, tile);
            }
            return removed;
        }

        public bool RestoreDomain(Tile tile)
        {
            return tileOptions.Add(tile);
        }

        public Tile SelectTile()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            return tileOptionsArray[UnityEngine.Random.Range(0, tileOptionsArray.Length)];
        }

        private void Collapse()
        {
            // Check for a contradiction. Should never happen if this is called only after successful level generation.
            if (tileOptions.Count == 0)
            {
                throw new System.Exception("Contradiction! Cell has no choices left!");
            }

            // Instantiate the tile prefab in the cell.
            Tile tileOption = tileOptions.ToArray()[0];
            tilePrefab = Instantiate(tileOption.Prefab, this.transform);

            // // Use one of the possible rotations.
            // float randomRotation = 90f * tileOption.possibleCardinalities[Random.Range(0, tileOption.possibleCardinalities.Count)];
            // collapsedCell.transform.Rotate(0f, randomRotation, 0f);
        }

        public void PruneDomain(Cell adjacentCell, int cardinalityToThis, ref Queue<Cell> propagationQueue)
        {
            // Get a set of all the possible neighbours of the adjacentCell.
            HashSet<Tile> possibleNeighbours = new HashSet<Tile>();
            foreach (Tile tile in adjacentCell.tileOptions)
            {
                possibleNeighbours.AddRange(tile.PossibleNeighbours(adjacentCell, cardinalityToThis));
            }

            // Intersect the current cell's options with the possible neighbours of the adjacentCell.
            int sizeBefore = tileOptions.Count;
            tileOptions.IntersectWith(possibleNeighbours);
            int sizeAfter = tileOptions.Count;

            // Add the current cell to the propagation queue if its domain was changed.
            // This ensures arc consistency.
            if (sizeBefore != sizeAfter)
            {
                propagationQueue.Enqueue(this);
            }
        }

        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }
}