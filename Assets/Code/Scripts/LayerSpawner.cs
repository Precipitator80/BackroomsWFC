using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Spawns a single layer of a chunk. Uses the chunk's RNG.
    /// </summary>
    public class LayerSpawner
    {
        Cell[,] grid; // A grid of cells to run MAC3 / WFC on.
        Stack<StateChange> stateChanges; // A stack of state changes for each recursive step / depth of search.
        List<Cell> cellList; // A list of cells left to assign.
        Vector3 startingCellCoordinate; // The global grid coordinates of the starting cell for this layer (0,0).
        Chunk parentChunk; // The chunk that this layer is a part of. Used for RNG.
        float startTime; // Start time variable to track the generation duration of this chunk.

        /// <summary>
        /// Constructor that sets the position and chunk of the layer.
        /// </summary>
        /// <param name="startingCellCoordinates">The global grid coordinates of the starting cell for this layer (0,0).</param>
        /// <param name="parentChunk">The chunk that this layer is a part of.</param>
        public LayerSpawner(Vector3 startingCellCoordinates, Chunk parentChunk)
        {
            this.startingCellCoordinate = startingCellCoordinates;
            this.parentChunk = parentChunk;
        }

        /// <summary>
        /// Spawns the layer. Kept separate from object creation for more control over when to spawn the layer.
        /// </summary>
        public void Spawn()
        {
            // Initialise the state changes stack and add an initial state.
            stateChanges = new Stack<StateChange>();
            enterNewState(null);

            // Set up a grid of cells with padding for the border.
            grid = new Cell[Chunk.LayerSize + 2, Chunk.LayerSize + 2];
            cellList = new List<Cell>();
            for (int y = 0; y < grid.GetLength(0); y++)
            {
                for (int x = 0; x < grid.GetLength(1); x++)
                {
                    // Check for previous information. If there is none, make a fresh cell.
                    Cell previousCell = GetCell(y + (int)startingCellCoordinate.z, x + (int)startingCellCoordinate.x);
                    if (previousCell != null)
                    {
                        grid[y, x] = previousCell;
                        previousCell.updateLocalCoordinates(y, x, parentChunk);
                    }
                    else
                    {
                        grid[y, x] = new Cell(y, x, startingCellCoordinate + new Vector3(x, 0f, y), parentChunk);
                        // If this is a new tile at the border, turn it into an empty tile.
                        if (y == 0 || y == grid.GetLength(0) - 1 || x == 0 || x == grid.GetLength(1) - 1)
                        {
                            grid[y, x].tileOptions = new HashSet<Tile>() { LevelGenerationManager.Instance.ExplicitEmptyTile };
                        }
                    }
                    cellList.Add(grid[y, x]);
                }
            }

            // Refresh any cells in the middle of the layer. Previous information is kept through the separate 'previouslyCollapsedTile' variable in the Cell class.
            for (int y = 1; y < grid.GetLength(0) - 1; y++)
            {
                for (int x = 1; x < grid.GetLength(1) - 1; x++)
                {
                    grid[y, x].tileOptions = new HashSet<Tile>(LevelGenerationManager.Instance.ExplicitTileSet);
                }
            }

            try
            {
                /* LEGACY MAC3 DISCUSSION
                    // The grid will only not be globally arc consistent at the start if the domains are not all equal.
                    // In simple WFC, all cells have the same domain choices at the start, so AC3 doesn't have to be run.
                    // If choosing preset cells, it might be sufficient to just run macAC3 around those cells rather than running them on every cell.
                */
                // Run AC3 on the cells to make them arc consistent with any border cells and previous cells.
                Queue<CellArc> startingQueue = new Queue<CellArc>();
                foreach (Cell cell in grid)
                {
                    Queue<CellArc> queueForOne = getArcs(cell);
                    while (queueForOne.Count > 0)
                    {
                        startingQueue.Enqueue(queueForOne.Dequeue());
                    }
                }
                macAC3(startingQueue);

                startTime = Time.realtimeSinceStartup;
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Layer Generation Start Time: " + startTime);
                }

                // Run the MAC3 search algorithm.
                MAC3();

                if (finished())
                {
                    if (!TimedOut)
                    {
                        if (LevelGenerationManager.Instance.debugMode)
                        {
                            Debug.Log("Layer generation successful! Collapsing all cells!");
                        }
                        foreach (Cell cell in grid)
                        {
                            cell.Collapse();
                        }
                    }
                    else if (LevelGenerationManager.Instance.debugMode)
                    {
                        Debug.LogError("Layer generation failed due to timeout!");
                    }
                }
                else if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.LogError("Level generation failed! Could not find a solution");
                }
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Layer Generation End Time: " + Time.realtimeSinceStartup);
                }
            }
            catch (EmptyDomainException)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Failed to generate layer. Recovering previous tiles and leaving as is.");
                }

                // Set the tileOptions variable to what it was before to restore tile information.
                for (int y = 1; y < grid.GetLength(0) - 1; y++)
                {
                    for (int x = 1; x < grid.GetLength(1) - 1; x++)
                    {
                        grid[y, x].tileOptions = new HashSet<Tile>() { grid[y, x].previouslyCollapsedTile };
                    }
                }
            }
        }

        public void Unload()
        {
            foreach (Cell cell in grid)
            {
                if (cell.tilePrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(cell.tilePrefab);
                }
            }
        }

        /// <summary>
        /// Searches for a pre-existing cell in a certain position.
        /// </summary>
        /// <param name="globalY">The y coordinate within the global grid.</param>
        /// <param name="globalX">The x coordinate within the global grid.</param>
        /// <returns></returns>
        private Cell GetCell(int globalY, int globalX)
        {
            // Get the world position of the cell to search for.
            Vector3 worldPosition = LevelGenerationManager.Instance.GridCoordinatesToWorldPos(new Vector3(globalX, 0f, globalY));

            // Do a box overlap to check for the box collider of a tile of a previously spawned chunk.
            Vector3 halfExtents = new Vector3(LevelGenerationManager.Instance.tileSize / 2f, LevelGenerationManager.Instance.tileSize / 2f, LevelGenerationManager.Instance.tileSize / 2f);
            Collider[] colliders = Physics.OverlapBox(worldPosition, halfExtents);

            // Gizmos debug data.
            LevelGenerationManager.Instance.gizmosPosAndSize.AddLast(new LinkedListNode<(Vector3, Vector3)>((worldPosition, 2f * halfExtents)));

            // Check whether a previously generated tile could be found at the position and get the cell through the tile.
            if (colliders.Length > 0)
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("colliders.length = " + colliders.Length + " Current(x, y) = (" + globalX + ", " + globalY + "). Position: " + worldPosition);
                }
                foreach (Collider collider in colliders)
                {
                    if (collider.GetType() == typeof(BoxCollider))
                    {
                        if (LevelGenerationManager.Instance.debugMode)
                        {
                            Debug.Log(collider.gameObject.name);
                        }

                        // Check for a cell reference within the collider.
                        CellReference cellReference = collider.gameObject.GetComponent<CellReference>();
                        if (cellReference == null)
                        {
                            cellReference = collider.transform.parent.GetComponentInParent<CellReference>();
                        }

                        // Update the information of the cell and use it in the next chunk.
                        if (cellReference != null && cellReference.cell != null)
                        {
                            if (LevelGenerationManager.Instance.debugMode)
                            {
                                Debug.Log("Found cell reference! Current (x,y) = (" + globalX + "," + globalY + "). Position: " + worldPosition + ". Collapsed Cell (x,y) = (" + cellReference.cell.x + "," + cellReference.cell.y + "). Collapsed cell position: " + cellReference.transform.position + ". CellReference GO name: " + cellReference.gameObject.name);
                            }
                            return cellReference.cell;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Maintaining Arc Consistency Algorithm using AC3.
        /// </summary>
        private void MAC3()
        {
            // Check whether all cells have been assigned.
            if (finished())
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Finished! (1)");
                }
                return;
            }

            // Select a cell and tile to assign.
            Cell cell = selectCell();
            Tile tile = cell.SelectTile();

            // Create a new state.
            // Assign the tile to the cell, removing all other tile from the cell's domain.
            enterNewState(cell);
            bool changed = cell.Assign(tile, this);
            cellList.Remove(cell);

            // Propagate any changes and, if there were any, run the algorithm again to assign further cells.
            // If no changes were made by AC3 (returns false) after checking for a solution, then a dead end was reached.
            try
            {
                if (changed)
                {
                    macAC3(cell);
                }
                MAC3();
            } // Exception to let AC3 cancel early in the case of a domain wipeout.
            catch (EmptyDomainException) { }

            if (finished())
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Finished! (2)");
                }
                return;
            }

            // If recursion finished, this code is reached.
            // Revert the state and remove the tile value that was checked from the cell's domain.
            revertState();

            // If the domain is not empty, propagate the domain pruning.
            // If this resulted in changes, run the algorithm again.
            if (!cell.EmptyDomain)
            {
                try
                {
                    cell.Unassign(tile, this);
                    macAC3(cell);
                    MAC3();
                } // Exception to let AC3 cancel early in the case of a domain wipeout.
                catch (EmptyDomainException) { }
            }

            if (finished())
            {
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Finished! (3)");
                }
                return;
            }

            // Restore the cell's domain with the chosen tile to be a potential option when making a previous choice differently.
            cell.RestoreDomain(tile);
        }

        /// <summary>
        /// Get the current state change.
        /// </summary>
        public StateChange CurrentStateChanges
        {
            get
            {
                return stateChanges.Peek();
            }
        }

        /// <summary>
        /// Enter a new state by adding an empty state change to the stack.
        /// </summary>
        private void enterNewState(Cell assignedCell)
        {
            stateChanges.Push(new StateChange(assignedCell));
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("Entered new state. Stack size is now " + stateChanges.Count);
            }
        }

        /// <summary>
        /// Pops the current state and reverts any changes made by it.
        /// Only done when the current state is not the starting state.
        /// </summary>
        private void revertState()
        {
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("Reverting state!");
            }
            if (stateChanges.Count > 1)
            {
                StateChange currentState = stateChanges.Pop();
                currentState.revert();
                cellList.Add(currentState.assignedCell);
            }
            else if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("State changes is at starting size!");
            }
        }

        /// <summary>
        /// Method to select a cell to make a choice for.
        /// At the moment it is in ascending order but should later be switched to lowest entropy
        /// </summary>
        /// <returns>The cell to make a choice for.</returns>
        private Cell selectCell()
        {
            //return selectCellAscendingOrder();
            //return selectCellSmallestDomain();
            return SelectCellWithLowestWeightedEntropy();
        }

        /// <summary>
        /// Selects the first cell that is not yet assigned.
        /// </summary>
        /// <returns>The first cell that is not yet assigned.</returns>
        private Cell selectCellAscendingOrder()
        {
            if (cellList.Count > 0)
            {
                return cellList[0];
            }
            Debug.Log("All cells are already collapsed! Returning 0,0.");
            return grid[0, 0];
        }

        /// <summary>
        /// Selects the cell with the smallest domain not yet assigned.
        /// </summary>
        /// <returns>The cell with the smallest domain not yet assigned.</returns>
        private Cell selectCellSmallestDomain()
        {
            Cell smallestDomainCell = null;
            int smallestDomainSize = int.MaxValue;
            foreach (Cell potentialCell in cellList)
            {
                if (potentialCell.tileOptions.Count < smallestDomainSize)
                {
                    smallestDomainCell = potentialCell;
                    smallestDomainSize = potentialCell.tileOptions.Count;
                }
            }
            if (smallestDomainCell == null)
            {
                Debug.Log("Trying to select cell when all are assigned! Returning default (0,0).");
                return grid[0, 0];
            }
            Debug.Log("Smallest domain cell: " + smallestDomainCell);
            return smallestDomainCell;
        }

        private Cell SelectCellWithLowestWeightedEntropy()
        {
            Cell lowestEntropyCell = null;
            float lowestWeightedEntropy = float.MaxValue;

            foreach (Cell cell in cellList)
            {
                float weightedEntropy = CalculateShannonEntropy(cell);

                if (weightedEntropy < lowestWeightedEntropy)
                {
                    lowestWeightedEntropy = weightedEntropy;
                    lowestEntropyCell = cell;
                }
            }

            return lowestEntropyCell;
        }

        private float CalculateShannonEntropy(Cell cell)
        {
            if (cell.tileOptions.Count == 0)
            {
                return 0f;
            }

            // Calculate the sum of weights
            float sumOfWeights = cell.tileOptions.Sum(tile => tile.weight);

            // Calculate the sum of (weight * log(weight))
            float sumWeightedLogWeights = cell.tileOptions.Sum(tile => tile.weight * Mathf.Log(tile.weight));

            // Calculate the Shannon entropy
            float shannonEntropy = Mathf.Log(sumOfWeights) - (sumWeightedLogWeights / sumOfWeights);

            return shannonEntropy;
        }

        /// <summary>
        /// AC3 starting with one cell.
        /// </summary>
        /// <param name="cell"> The starting cell.</param>
        /// <returns>Whether any domains were changed.</returns>
        private bool macAC3(Cell cell)
        {
            return macAC3(getTargetedArcs(cell));
        }

        /// <summary>
        /// Arc Consistency 3 in the MAC Algorithm.
        /// </summary>
        /// <param name="queue">The propagation queue of arcs to check.</param>
        /// <returns>Whether any domains were changed.</returns>
        private bool macAC3(Queue<CellArc> queue)
        {
            bool changed = false;
            if (LevelGenerationManager.Instance.debugMode)
            {
                Debug.Log("Running macAC3 with a propagation queue of size " + queue.Count);
            }
            while (queue.Count > 0)
            {
                CellArc arc = queue.Dequeue();
                if (LevelGenerationManager.Instance.debugMode)
                {
                    Debug.Log("Revising arc: " + arc);
                }
                if (Revise(arc))
                {
                    changed = true;
                    if (LevelGenerationManager.Instance.debugMode)
                    {
                        Debug.Log("Getting targeted arcs for arc: " + arc);
                    }
                    foreach (CellArc targetedArc in getTargetedArcs(arc))
                    {
                        if (!queue.Contains(arc))
                        {
                            queue.Enqueue(targetedArc);
                        }
                    }
                }
            }
            return changed;
        }

        /// <summary>
        /// Checks whether assignments have been finished.
        /// </summary>
        /// <returns>True if all cells have been assigned a tile.</returns>
        private bool finished()
        {
            if (TimedOut)
            {
                return true;
            }
            //return cellsCollapsed == (xSize * ySize); // Might be a more optimal way to check this to implement later.
            foreach (Cell cell in grid)
            {
                if (cell.tileOptions.Count != 1)
                {
                    return false;
                }
            }
            return true;
        }

        private bool TimedOut
        {
            get
            {
                return Time.realtimeSinceStartup - startTime > LevelGenerationManager.Instance.timeOut;
            }
        }

        /// <summary>
        /// Gets all the arcs around a single cell in the grid.
        /// </summary>
        /// <param name="cell">The cell to get connected arcs of.</param>
        /// <returns>All arcs around the cell.</returns>
        private Queue<CellArc> getArcs(Cell cell)
        {
            Queue<CellArc> queue = new Queue<CellArc>();
            // Propagate to each neighbour of the cell.
            // Upper neighbour.
            if (cell.y < grid.GetLength(0) - 1)
            {
                generateArcPair(cell, grid[cell.y + 1, cell.x], ref queue);
            }
            // Right neighbour.
            if (cell.x < grid.GetLength(1) - 1)
            {
                generateArcPair(cell, grid[cell.y, cell.x + 1], ref queue);
            }
            // Bottom neighbour.
            if (cell.y > 1)
            {
                generateArcPair(cell, grid[cell.y - 1, cell.x], ref queue);
            }
            // Left neighbour.
            if (cell.x > 1)
            {
                generateArcPair(cell, grid[cell.y, cell.x - 1], ref queue);
            }
            return queue;
        }

        /// <summary>
        /// Creates arcs for a pair of cells and adds them to a queue.
        /// </summary>
        /// <param name="cell1">The first cell of the arc.</param>
        /// <param name="cell2">The second cell of the arc.</param>
        /// <param name="queue">The queue to add the arcs to.</param>
        private void generateArcPair(Cell cell1, Cell cell2, ref Queue<CellArc> queue)
        {
            queue.Enqueue(new CellArc(cell1, cell2));
            queue.Enqueue(new CellArc(cell2, cell1));
        }

        /// <summary>
        /// Gets all the arcs targeting a given cell.
        /// </summary>
        /// <param name="arc">An arc containing the target cell as well as another cell to ignore.</param>
        /// <returns>A queue of arcs targeting the given cell.</returns>
        private Queue<CellArc> getTargetedArcs(CellArc arc)
        {
            Queue<CellArc> queue = new Queue<CellArc>();
            Cell cell = arc.cell1;
            // Propagate to each neighbour of the cell.
            // Upper neighbour.
            if (cell.y < grid.GetLength(0) - 1)
            {
                Cell otherCell = grid[cell.y + 1, cell.x];
                if (otherCell != arc.cell2)
                {
                    queue.Enqueue(new CellArc(otherCell, cell));
                }
            }
            // Right neighbour.
            if (cell.x < grid.GetLength(1) - 1)
            {
                Cell otherCell = grid[cell.y, cell.x + 1];
                if (otherCell != arc.cell2)
                {
                    queue.Enqueue(new CellArc(otherCell, cell));
                }
            }
            // Bottom neighbour.
            if (cell.y > 1)
            {
                Cell otherCell = grid[cell.y - 1, cell.x];
                if (otherCell != arc.cell2)
                {
                    queue.Enqueue(new CellArc(otherCell, cell));
                }
            }
            // Left neighbour.
            if (cell.x > 1)
            {
                Cell otherCell = grid[cell.y, cell.x - 1];
                if (otherCell != arc.cell2)
                {
                    queue.Enqueue(new CellArc(otherCell, cell));
                }
            }
            return queue;
        }

        /// <summary>
        /// Gets all the arcs targeting a given cell.
        /// </summary>
        /// <returns>A queue of arcs targeting the given cell.</returns>
        private Queue<CellArc> getTargetedArcs(Cell cell)
        {
            Queue<CellArc> queue = new Queue<CellArc>();
            // Propagate to each neighbour of the cell.
            // Upper neighbour.
            if (cell.y < grid.GetLength(0) - 1)
            {
                Cell otherCell = grid[cell.y + 1, cell.x];
                queue.Enqueue(new CellArc(otherCell, cell));
            }
            // Right neighbour.
            if (cell.x < grid.GetLength(1) - 1)
            {
                Cell otherCell = grid[cell.y, cell.x + 1];
                queue.Enqueue(new CellArc(otherCell, cell));
            }
            // Bottom neighbour.
            if (cell.y > 1)
            {
                Cell otherCell = grid[cell.y - 1, cell.x];
                queue.Enqueue(new CellArc(otherCell, cell));
            }
            // Left neighbour.
            if (cell.x > 1)
            {
                Cell otherCell = grid[cell.y, cell.x - 1];
                queue.Enqueue(new CellArc(otherCell, cell));
            }
            return queue;
        }

        /// <summary>
        /// An arc revision that removes any domain values not supporting it.
        /// </summary>
        /// <param name="arc">The arc to revise.</param>
        /// <returns>Whether the domain of the arc's primary / first cell was changed without any domain wipeout.</returns>
        /// <exception cref="EmptyDomainException">Thrown when revision leaves a domain empty.</exception>
        private bool Revise(CellArc arc)
        {
            bool changed = false;
            // Remove any tiles not supported.
            HashSet<Tile> tilesToRemove = new HashSet<Tile>(arc.cell1.tileOptions.Where(tile => !tile.Supported(arc)));
            foreach (Tile tileToRemove in tilesToRemove)
            {
                changed = true;
                arc.cell1.tileOptions.Remove(tileToRemove);
                CurrentStateChanges.addDomainChange(arc.cell1, tileToRemove);
            }

            // Check if domain is empty.
            if (arc.cell1.tileOptions.Count == 0)
            {
                throw new EmptyDomainException();
            }

            return changed;
        }
    }
}