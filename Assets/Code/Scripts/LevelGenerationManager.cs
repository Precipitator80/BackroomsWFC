using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PrecipitatorWFC
{
    [ExecuteInEditMode]
    public class LevelGenerationManager : MonoBehaviour
    {
        public int ySize = 10;
        public int xSize = 10;
        private Cell[,] grid;

        int cellsCollapsed;

        private Stack<StateChange> stateChanges;

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

        private void Awake()
        {
            Debug.Log("LevelGenerationManager is awake!");
            instance = this;
        }

        public void Begin()
        {
            // Set up a grid of cells.
            grid = new Cell[ySize, xSize];
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    grid[y, x] = new Cell(y, x);
                }
            }

            stateChanges = new Stack<StateChange>();
            enterNewState();
            // run macAC3 once? Only if using preset tiles.

            cellsCollapsed = 0;

            MAC3();
            // while (cellsCollapsed < xSize * ySize)
            // {
            //     int randomY = UnityEngine.Random.Range(0, ySize);
            //     int randomX = UnityEngine.Random.Range(0, xSize);
            //     bool collapsed = grid[randomY, randomX].ForceCollapse();

            //     if (collapsed)
            //     {
            //         cellsCollapsed++;
            //         // Create a propagation queue.
            //         Queue<Cell> propagationQueue = new Queue<Cell>();
            //         propagationQueue.Enqueue(grid[randomY, randomX]);

            //         // Propagate until the queue is empty to ensure arc consistency.
            //         while (propagationQueue.Count > 0)
            //         {
            //             Cell cell = propagationQueue.Dequeue();

            //             // Propagate to each neighbour of the cell.
            //             // Upper neighbour.
            //             if (cell.y < ySize - 1)
            //             {
            //                 grid[cell.y + 1, cell.x].PruneDomain(cell, 0, ref propagationQueue);
            //             }
            //             // Right neighbour.
            //             if (cell.x < xSize - 1)
            //             {
            //                 grid[cell.y, cell.x + 1].PruneDomain(cell, 1, ref propagationQueue);
            //             }
            //             // Bottom neighbour.
            //             if (cell.y > 1)
            //             {
            //                 grid[cell.y - 1, cell.x].PruneDomain(cell, 2, ref propagationQueue);
            //             }
            //             // Left neighbour.
            //             if (cell.x > 1)
            //             {
            //                 grid[cell.y, cell.x - 1].PruneDomain(cell, 3, ref propagationQueue);
            //             }
            //         }
            //     }
            // }
        }

        private void MAC3()
        {
            if (finished())
            {
                return;
            }

            enterNewState();

            Cell cell = selectCell();
            Tile tile = cell.selectTile();
            cell.assign(tile);


            if (finished())
            {
                return;
            }

            if (macAC3(cell))
            {
                MAC3();
            }


            revertState();
            cell.unassign(tile);

            if (!cell.emptyDomain())
            {
                if (macAC3(cell))
                {
                    MAC3();
                }
                revertState();
            }
            cell.restoreDomain(tile);
        }

        private bool finished()
        {
            return cellsCollapsed == (xSize * ySize);
        }
        private Cell selectCell()
        {
            // Should later switch to lowest entropy.
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    if (!grid[y, x].Collapsed())
                    {
                        return grid[y, x];
                    }
                }
            }
            //throw new Exception("All cells are already collapsed!");
            Debug.Log("All cells are already collapsed! Returning 0,0.");
            return grid[0, 0];
        }

        private void enterNewState()
        {
            stateChanges.Append(new StateChange());
        }

        public StateChange CurrentState
        {
            get
            {
                return stateChanges.Peek();
            }
        }

        private void revertState()
        {
            StateChange currentState = stateChanges.Pop();
            currentState.revert();
        }

        private bool macAC3(Cell cell)
        {
            return macAC3(getArcs(cell));
        }

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

        private void generateArcPair(Cell cell1, Cell cell2, ref Queue<CellArc> queue)
        {
            queue.Append(new CellArc(cell1, cell2));
            queue.Append(new CellArc(cell2, cell1));
        }

        private bool macAC3(Queue<CellArc> queue)
        {
            bool changed = false;
            while (queue.Count > 0)
            {
                CellArc arc = queue.Dequeue();
                if (revise(arc))
                {
                    foreach (CellArc targetedArc in getArcs(arc.cell1))
                    {
                        queue.Append(targetedArc);
                    }
                }
            }
            return changed;
        }

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
                    queue.Append(new CellArc(otherCell, cell));
                }
            }
            // Right neighbour.
            if (cell.x < xSize - 1)
            {
                Cell otherCell = grid[cell.y, cell.x + 1];
                if (otherCell != arc.cell2)
                {
                    queue.Append(new CellArc(otherCell, cell));
                }
            }
            // Bottom neighbour.
            if (cell.y > 1)
            {
                Cell otherCell = grid[cell.y - 1, cell.x];
                if (otherCell != arc.cell2)
                {
                    queue.Append(new CellArc(otherCell, cell));
                }
            }
            // Left neighbour.
            if (cell.x > 1)
            {
                Cell otherCell = grid[cell.y, cell.x - 1];
                if (otherCell != arc.cell2)
                {
                    queue.Append(new CellArc(otherCell, cell));
                }
            }
            return queue;
        }


        private bool revise(CellArc arc)
        {
            bool changed = false;
            foreach (Tile tile in arc.cell1.tileOptions)
            {
                bool supported = false;
                // EITHER
                // Check that the other cell has this tile as a possible neighbour.
                // OR
                // Check that this tile has a possible neighbour that is an option in the other cell.

                // CONSIDERATIONS FOR COLLAPSED VS NOT COLLAPSED?
                throw new NotImplementedException();
            }
            return changed;
        }

        static LevelGenerationManager()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

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
                generator.Begin();
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
                generator.Begin();
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