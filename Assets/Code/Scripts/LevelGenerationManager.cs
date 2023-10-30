using System;
using System.Collections.Generic;
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

            // Collapse a random cell.
            int cellsCollapsed = 0;
            while (cellsCollapsed < xSize * ySize)
            {
                int randomY = UnityEngine.Random.Range(0, ySize);
                int randomX = UnityEngine.Random.Range(0, xSize);
                bool collapsed = grid[randomY, randomX].ForceCollapse();

                if (collapsed)
                {
                    cellsCollapsed++;
                    // Create a propagation queue.
                    Queue<Cell> propagationQueue = new Queue<Cell>();
                    propagationQueue.Enqueue(grid[randomY, randomX]);

                    // Propagate until the queue is empty to ensure arc consistency.
                    while (propagationQueue.Count > 0)
                    {
                        Cell cell = propagationQueue.Dequeue();

                        // Propagate to each neighbour of the cell.
                        // Upper neighbour.
                        if (cell.y < ySize - 1)
                        {
                            grid[cell.y + 1, cell.x].PruneDomain(cell, 0, ref propagationQueue);
                        }
                        // Right neighbour.
                        if (cell.x < xSize - 1)
                        {
                            grid[cell.y, cell.x + 1].PruneDomain(cell, 1, ref propagationQueue);
                        }
                        // Bottom neighbour.
                        if (cell.y > 1)
                        {
                            grid[cell.y - 1, cell.x].PruneDomain(cell, 2, ref propagationQueue);
                        }
                        // Left neighbour.
                        if (cell.x > 1)
                        {
                            grid[cell.y, cell.x - 1].PruneDomain(cell, 3, ref propagationQueue);
                        }
                    }
                }
            }
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