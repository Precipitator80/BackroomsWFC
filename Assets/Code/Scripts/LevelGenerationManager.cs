using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrecipitatorWFC
{
    // TODO: Fix generation algorithm!

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
        private void Awake()
        {
            Debug.Log("LevelGenerationManager is awake!");
            instance = this;
        }

        public int ySize = 10; // The depth of the level.
        public int xSize = 10; // The width of the level.
        public int tileSize = 2; // The size of all tiles in the tile set.
        public Tile[] tileSet; // A tile set of all possible tiles to place in the grid.
        private Cell[,] grid; // A grid of cells to run MAC3 / WFC on.
        private Stack<StateChange> stateChanges; // A stack of state changes for each recursive step / depth of search.

        /// <summary>
        /// Generates a level using the MAC3 algorithm for WFC.
        /// </summary>
        public void GenerateLevel()
        {
            // Initialise the state changes stack and add an initial state.
            stateChanges = new Stack<StateChange>();
            enterNewState();

            // Set up a grid of cells.
            grid = new Cell[ySize, xSize];
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    grid[y, x] = new Cell(y, x);
                }
            }

            // The grid will only not be globally arc consistent at the start if the domains are not all equal.
            // In simple WFC, all cells have the same domain choices at the start, so AC3 doesn't have to be run.
            // If choosing preset cells, it might be sufficient to just run macAC3 around those cells rather than running them on every cell.
            // macAC3();

            // Run the MAC3 search algorithm.
            MAC3();

            Debug.Log("Finished MAC3 level generation.");
        }

        /// <summary>
        /// Maintaining Arc Consistency Algorithm using AC3.
        /// </summary>
        private void MAC3()
        {
            // Check whether all cells have been assigned.
            if (finished())
            {
                Debug.Log("Finished! (1)");
                return;
            }

            // Create a new state.
            enterNewState();

            // Select a cell and tile to assign.
            Cell cell = selectCell();
            Tile tile = cell.SelectTile();

            // Assign the tile to the cell, removing all other tile from the cell's domain.
            bool changed = cell.Assign(tile);

            // Check whether all cells have been assigned.
            if (finished())
            {
                Debug.Log("Finished! (2)");
                return;
            }

            // Propagate any changes and, if there were any, run the algorithm again to assign further cells.
            // If no changes were made by AC3 (returns false) after checking for a solution, then a dead end was reached.
            try
            {
                if (macAC3(cell) || changed)
                {
                    MAC3();
                }
            } // Exception to let AC3 cancel early in the case of a domain wipeout.
            catch (EmptyDomainException) { }

            // If recursion finished, this code is reached.
            // Revert the state and remove the tile value that was checked from the cell's domain.
            revertState();
            changed = cell.Unassign(tile);

            // If the domain is not empty, propagate the domain pruning.
            // If this resulted in changes, run the algorithm again.
            if (!cell.EmptyDomain)
            {
                try
                {
                    if (macAC3(cell) || changed)
                    {
                        MAC3();
                    }
                } // Exception to let AC3 cancel early in the case of a domain wipeout.
                catch (EmptyDomainException) { }
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
        private void enterNewState()
        {
            stateChanges.Push(new StateChange());
            Debug.Log("Entered new state. Stack size is now " + stateChanges.Count);
        }

        /// <summary>
        /// Pops the current state and reverts any changes made by it.
        /// Only done when the current state is not the starting state.
        /// </summary>
        private void revertState()
        {
            Debug.Log("Reverting state!");
            if (stateChanges.Count > 1)
            {
                StateChange currentState = stateChanges.Pop();
                currentState.revert();
            }
            else
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
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    if (!grid[y, x].Assigned)
                    {
                        return grid[y, x];
                    }
                }
            }
            Debug.Log("All cells are already collapsed! Returning 0,0.");
            return grid[0, 0];
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
            Debug.Log("Running macAC3 with a propagation queue of size " + queue.Count);
            while (queue.Count > 0)
            {
                CellArc arc = queue.Dequeue();
                Debug.Log("Revising arc: " + arc);
                if (Revise(arc))
                {
                    changed = true;
                    Debug.Log("Getting targeted arcs for arc: " + arc);
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
            HashSet<Tile> tilesToRemove = new HashSet<Tile>(arc.cell1.tileOptions.Where(otherTile => !otherTile.Supported(arc)));
            foreach (Tile unsupportedTile in tilesToRemove)
            {
                changed = true;
                arc.cell1.tileOptions.Remove(unsupportedTile);
                CurrentStateChanges.addDomainChange(arc.cell1, unsupportedTile);
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