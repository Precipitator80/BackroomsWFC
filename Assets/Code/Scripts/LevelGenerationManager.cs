using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// LevelGenerationManager to handle the MAC3 / WFC search.
    /// </summary>
    [ExecuteInEditMode]
    public class LevelGenerationManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the LevelGenerationManager
        /// </summary>
        private static LevelGenerationManager instance;
        public static LevelGenerationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("LevelGeneration is null!");
                }
                return instance;
            }
        }

        /// <summary>
        /// Awake function to set the LevelGenerationManager instance.
        /// </summary>
        public void Awake()
        {
            Debug.Log("LevelGenerationManager is awake!");
            instance = this;
        }

        public int ySize = 10; // The depth of the level.
        public int xSize = 10; // The width of the level.
        public int tileSize = 2; // The size of all tiles in the tile set.
        public Tile[] tileSet; // A tile set of all possible tiles to place in the grid.
        private List<NonSymmetricTile> explicitTileSet; // An explicit tile set filled with a non-symmetric tile for each possible original tile rotation to avoid intricate rotation checks.
        public Cell[,] grid; // A grid of cells to run MAC3 / WFC on.
        public bool debugMode = false; // Logs each step of the algorithm to the console.
        private Stack<StateChange> stateChanges; // A stack of state changes for each recursive step / depth of search.
        private List<Cell> cellList; // A list of cells left to assign.
        public float timeOut = 10f;
        private float startTime;

        public List<NonSymmetricTile> ExplicitTileSet
        {
            get
            {
                // Initialise the explicit tile set if it has not already been done.
                if (explicitTileSet == null)
                {
                    explicitTileSet = new List<NonSymmetricTile>();

                    // Create a game object to hold the explicit tile set.
                    GameObject explicitTileSetGO = new GameObject("Explicit Tile Set Parent");
                    explicitTileSetGO.transform.parent = this.transform;

                    // Create non-symmetric variants for each tile.
                    foreach (Tile tile in tileSet)
                    {
                        // Create a non-symmetric copy of the original tile.
                        NonSymmetricTile nonSymmetricTile = tile.InstantiateNonSymmetricTile();
                        nonSymmetricTile.name = tile.name;
                        nonSymmetricTile.transform.parent = explicitTileSetGO.transform;

                        // Add the copy to the explicit tile set and set it as the 0 rotation variant.
                        tile.ExplicitVariants = new NonSymmetricTile[4];
                        tile.ExplicitVariants[0] = nonSymmetricTile;
                        explicitTileSet.Add(nonSymmetricTile);
                        if (debugMode)
                        {
                            Debug.Log("Added explicit variant of tile " + tile + ": " + tile.ExplicitVariants[0]);
                        }

                        // Create rotation variants for non-cube tiles.
                        if (tile.GetType() != typeof(CubeTile))
                        {
                            nonSymmetricTile.name += " - 0"; // Specify the rotation of the variant.
                            for (int cardinality = 1; cardinality < tile.ExplicitVariants.Length; cardinality++)
                            {
                                // Create a rotation variant with the previous rotation as the base.
                                NonSymmetricTile variant = Instantiate(explicitTileSet[explicitTileSet.Count - 1], explicitTileSetGO.transform);

                                // Rotate the variant by 90 degrees and specify its rotation / cardinality in the name.
                                variant.transform.Rotate(0f, 90f, 0f);
                                variant.name = tile.name + " - " + cardinality;

                                // Add the variant to both the tile's variants array and the explicit tile set.
                                tile.ExplicitVariants[cardinality] = variant;
                                explicitTileSet.Add(variant);
                                if (debugMode)
                                {
                                    foreach (Tile[] side in new Tile[][] { variant.backNeighbours, variant.rightNeighbours, variant.frontNeighbours, variant.leftNeighbours })
                                    {
                                        foreach (Tile neighbour in variant.backNeighbours)
                                        {
                                            Debug.Log("Tile variant " + variant + " has neighbour: " + neighbour);
                                        }
                                    }
                                }

                                // Switch the neighbour and rotation arrays to follow the 90 degree rotation.
                                (variant.backNeighbours, variant.rightNeighbours, variant.frontNeighbours, variant.leftNeighbours) = (variant.leftNeighbours, variant.backNeighbours, variant.rightNeighbours, variant.frontNeighbours);
                                (variant.backRotations, variant.rightRotations, variant.frontRotations, variant.leftRotations) = (variant.leftRotations, variant.backRotations, variant.rightRotations, variant.frontRotations);

                                // Increase cardinality values of neighbours to account for the 90 degree rotation.
                                foreach (int[] rotationsArray in new int[][] { variant.backRotations, variant.rightRotations, variant.frontRotations, variant.leftRotations })
                                {
                                    for (int i = 0; i < rotationsArray.Length; i++)
                                    {
                                        rotationsArray[i] = (rotationsArray[i] + 1) % 4;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // For cube tiles, simply set the explicit rotation variants to be the 0 rotation non-symmetric tile.
                            tile.ExplicitVariants[1] = nonSymmetricTile;
                            tile.ExplicitVariants[2] = nonSymmetricTile;
                            tile.ExplicitVariants[3] = nonSymmetricTile;
                        }
                    }

                    // Replace the neighbours of each explicit tile without explicit rotational information with explicit variants accounting for the rotations.
                    foreach (NonSymmetricTile tile in explicitTileSet)
                    {
                        if (debugMode)
                        {
                            Debug.Log("Tile in explicit tile set: " + tile + ". Neighbours");
                        }

                        // Use 2D arrays of the neighbours and rotations to simplify the operation.
                        Tile[][] neighbourArrays = new Tile[][] { tile.backNeighbours, tile.rightNeighbours, tile.frontNeighbours, tile.leftNeighbours };
                        int[][] rotationsArrays = new int[][] { tile.backRotations, tile.rightRotations, tile.frontRotations, tile.leftRotations };

                        // Go through each neighbour array and neighbour tile in it.
                        for (int cardinality = 0; cardinality < neighbourArrays.Length; cardinality++)
                        {
                            for (int i = 0; i < neighbourArrays[cardinality].Length; i++)
                            {
                                // Get the rotation / cardinality value of the variant to use.
                                int rotation = rotationsArrays[cardinality][i];

                                if (debugMode)
                                {
                                    Debug.Log("Cardinality: " + cardinality + ". Neighbour index: " + i + ". Rotation: " + rotation);
                                    Debug.Log("NeighbourArrays.Length: " + neighbourArrays.Length);
                                    Debug.Log("NeighbourArrays[cardinality].Length: " + neighbourArrays[cardinality].Length);
                                    Debug.Log("NeighbourArrays[cardinality][i]: " + neighbourArrays[cardinality][i]);
                                    Debug.Log("neighbourArrays[cardinality][i].explicitVariants.Length: " + neighbourArrays[cardinality][i].ExplicitVariants.Length);
                                }

                                // Set the neighbour to the explicit variant with the correct rotation.
                                neighbourArrays[cardinality][i] = neighbourArrays[cardinality][i].ExplicitVariants[rotation];

                                if (debugMode)
                                {
                                    Debug.Log(tile + " has, at cardinality " + cardinality + ", neighbour " + neighbourArrays[cardinality][i]);
                                }
                            }
                        }
                    }
                }
                return explicitTileSet;
            }
        }

        //int chunkSize = 13;
        //int blocksize = (chunkSize - 2);

        /*
        General idea:
        Grid is potentially infinite:
            Dictionary of Dictionary mapping int and int to Cell.
        Choose a chunk size.
        Choose a chunk distance to generate around the player (hide rest with fog in-game).
        Generate a single layer by identifying a sub-area of each chunk and then generating these concurrently through multithreading.
            Per sub-area:
                Initialise cells if needed.
                (Would WFC bleed over outside of chunks? How would you manage this? What about chunk borders? Would the multithreading break determinism?)
            Define 
        */

        /// <summary>
        /// Generates a level using the MAC3 algorithm for WFC.
        /// </summary>
        public void GenerateLevel()
        {
            // Reset the explicit tile set in case the tile set was changed.
            explicitTileSet = null;

            // Ensure that the tile set is filled.
            if (tileSet.Length == 0)
            {
                Debug.LogError("Tile set for level generation cannot be empty!");
                return;
            }

            // Delete the previous level if one was generated.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            // Initialise the state changes stack and add an initial state.
            stateChanges = new Stack<StateChange>();
            enterNewState(null);

            // Set up a grid of cells.
            grid = new Cell[ySize, xSize];
            cellList = new List<Cell>();
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    grid[y, x] = new Cell(y, x);
                    cellList.Add(grid[y, x]);
                }
            }


            // The grid will only not be globally arc consistent at the start if the domains are not all equal.
            // In simple WFC, all cells have the same domain choices at the start, so AC3 doesn't have to be run.
            // If choosing preset cells, it might be sufficient to just run macAC3 around those cells rather than running them on every cell.
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
            Debug.Log("Start Time: " + startTime);

            // Run the MAC3 search algorithm.
            MAC3();

            if (finished())
            {
                if (!TimedOut)
                {
                    Debug.Log("Level generation successful! Collapsing all cells!");
                    foreach (Cell cell in grid)
                    {
                        cell.Collapse();
                    }
                }
                else
                {
                    Debug.LogError("Level generation failed due to timeout!");
                }
            }
            else
            {
                Debug.LogError("Level generation failed! Could not find a solution");
            }
            Debug.Log("End Time: " + Time.realtimeSinceStartup);
        }

        /// <summary>
        /// Maintaining Arc Consistency Algorithm using AC3.
        /// </summary>
        private void MAC3()
        {
            // Check whether all cells have been assigned.
            if (finished())
            {
                if (debugMode)
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
            bool changed = cell.Assign(tile);
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
                if (debugMode)
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
                    cell.Unassign(tile);
                    macAC3(cell);
                    MAC3();
                } // Exception to let AC3 cancel early in the case of a domain wipeout.
                catch (EmptyDomainException) { }
            }

            if (finished())
            {
                if (debugMode)
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
            if (debugMode)
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
            if (debugMode)
            {
                Debug.Log("Reverting state!");
            }
            if (stateChanges.Count > 1)
            {
                StateChange currentState = stateChanges.Pop();
                currentState.revert();
                cellList.Add(currentState.assignedCell);
            }
            else if (debugMode)
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
            return macAC3(getArcs(cell));
        }

        /// <summary>
        /// Arc Consistency 3 in the MAC Algorithm.
        /// </summary>
        /// <param name="queue">The propagation queue of arcs to check.</param>
        /// <returns>Whether any domains were changed.</returns>
        private bool macAC3(Queue<CellArc> queue)
        {
            bool changed = false;
            if (debugMode)
            {
                Debug.Log("Running macAC3 with a propagation queue of size " + queue.Count);
            }
            while (queue.Count > 0)
            {
                CellArc arc = queue.Dequeue();
                if (debugMode)
                {
                    Debug.Log("Revising arc: " + arc);
                }
                if (Revise(arc))
                {
                    changed = true;
                    if (debugMode)
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
                return Time.realtimeSinceStartup - startTime > timeOut;
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
            if (cell.y < ySize - 1)
            {
                generateArcPair(cell, grid[cell.y + 1, cell.x], ref queue);
            }
            // Right neighbour.
            if (cell.x < xSize - 1)
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
            if (cell.y < ySize - 1)
            {
                Cell otherCell = grid[cell.y + 1, cell.x];
                if (otherCell != arc.cell2)
                {
                    queue.Enqueue(new CellArc(otherCell, cell));
                }
            }
            // Right neighbour.
            if (cell.x < xSize - 1)
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

        /// <summary>
        /// Hooks to add generation buttons to the editor.
        /// </summary>
        static LevelGenerationManager()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// Adds generation control buttons to the GUI.
        /// </summary>
        /// <param name="sceneView">The sceneView being used.</param>
        static void OnSceneGUI(SceneView sceneView)
        {
            var generator = UnityEngine.Object.FindObjectOfType<LevelGenerationManager>();
            if (generator == null)
            {
                return;
            }

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(sceneView.position.width - 190, sceneView.position.height - 110, 180, 100));

            if (GUILayout.Button("Generate Simple tiled output"))
            {
                generator.GenerateLevel();
            }
            if (GUILayout.Button("Abort and Clear"))
            {
                throw new NotImplementedException();
            }

            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(LevelGenerationManager))]
    public class WaveFunctionCollapseGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LevelGenerationManager generator = (LevelGenerationManager)target;
            if (GUILayout.Button("Generate Simple tiled output"))
            {
                generator.GenerateLevel();
            }
            if (GUILayout.Button("Abort and Clear"))
            {
                throw new NotImplementedException();
            }
            DrawDefaultInspector();
        }
    }
#endif
}