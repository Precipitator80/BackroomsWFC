using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace PrecipitatorWFC
{
    [ExecuteInEditMode]
    public class Cell : MonoBehaviour
    {
        public HashSet<Tile> tileOptions;
        protected GameObject collapsedCell;
        public int y;
        public int x;

        public Cell(int y, int x)
        {
            tileOptions = new HashSet<Tile>(TileManager.Instance.allTiles);
            this.y = y;
            this.x = x;
        }

        public bool Collapsed()
        {
            return collapsedCell != null;
        }

        private void CollapseCheck()
        {
            if (tileOptions.Count < 2)
            {
                Collapse();
            }
        }

        public bool ForceCollapse()
        {
            if (!Collapsed())
            {
                tileOptions = new HashSet<Tile>(TileManager.Instance.allTiles);
                Tile[] tileOptionsArray = tileOptions.ToArray();
                Tile randomTileOption = tileOptionsArray[UnityEngine.Random.Range(0, tileOptionsArray.Length)];
                tileOptions.Add(randomTileOption);
                Collapse();
                return true;
            }
            return false;
        }

        public void assign(Tile tile)
        {
            collapsedCell = Instantiate(tile.gameObject, new Vector3(2 * x, 0f, 2 * y), Quaternion.identity);
        }

        public bool unassign(Tile tile)
        {
            Destroy(collapsedCell);
            bool removed = tileOptions.Remove(tile);
            if (removed)
            {
                LevelGenerationManager.Instance.CurrentState.addDomainChange(this, tile);
            }
            return removed;
        }

        public bool restoreDomain(Tile tile)
        {
            return tileOptions.Add(tile);
        }

        public bool emptyDomain()
        {
            return tileOptions.Count == 0;
        }

        public Tile selectTile()
        {
            Tile[] tileOptionsArray = tileOptions.ToArray();
            return tileOptionsArray[UnityEngine.Random.Range(0, tileOptionsArray.Length)];
        }


        private void Collapse()
        {
            // Check for a contradiction.
            if (tileOptions.Count == 0)
            {
                throw new System.Exception("Contradiction! Cell has no choices left!");
            }

            // Instantiate the tile prefab in the cell.
            Tile tileOption = tileOptions.ToArray()[0];
            collapsedCell = Instantiate(tileOption.gameObject, new Vector3(2 * x, 0f, 2 * y), Quaternion.identity);

            // Use one of the possible rotations.
            //float randomRotation = 90f * tileOption.possibleCardinalities[Random.Range(0, tileOption.possibleCardinalities.Count)];
            //collapsedCell.transform.Rotate(0f, randomRotation, 0f);

            // Propagate the choice made to other cells.
            //LevelGenerationManager.Instance.Propagate(this);
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
    }
}