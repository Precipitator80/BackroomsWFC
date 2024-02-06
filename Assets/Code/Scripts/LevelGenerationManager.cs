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

        public int seed = 0; // The seed to use for generation. Randomises random generation.
        public int chunkSize = 10; // The size of each chunk. Should not be lower than 3 to have proper overlap.
        public int tileSize = 2; // The size of all tiles in the tile set.
        public int numberOfChunks = 1; // Misleading name. How many chunks to spawn on each size. 1 = 1 NE, NW, SW and SE.
        public Tile[] tileSet; // A tile set of all possible tiles to place in the grid. Must contain the empty tile.
        public Tile emptyTile; // A tile used as empty space for the player starting area and for the ungenerated borders of chunks.
        private List<NonSymmetricTile> explicitTileSet; // An explicit tile set filled with a non-symmetric tile for each possible original tile rotation to avoid intricate rotation checks.
        public bool debugMode = false; // Logs each step of the algorithm to the console.
        public float timeOut = 1f; // The timeout to use for individual generation layers.
        private GameObject cellParent; // A gameObject to hold the currently loaded chunks.

        public GameObject CellParent
        {
            get
            {
                if (cellParent == null)
                {
                    cellParent = new GameObject("Level");
                    cellParent.transform.parent = this.transform;
                }
                return cellParent;
            }
        }

        private GameObject player; // The player's gameObject. Used for generation. Must use 'Player' tag.
        private GameObject Player
        {
            get
            {
                if (player == null)
                {
                    player = GameObject.FindGameObjectWithTag("Player");
                }
                return player;
            }
        }

        private NonSymmetricTile explicitEmptyTile;
        public NonSymmetricTile ExplicitEmptyTile
        {
            get
            {
                return explicitEmptyTile;
            }
        }

        public List<NonSymmetricTile> ExplicitTileSet
        {
            get
            {
                // Initialise the explicit tile set if it has not already been done.
                if (explicitTileSet == null)
                {
                    // Ensure that the empty tile is in the tile set.
                    if (emptyTile == null)
                    {
                        throw new Exception("The empty tile must be specified!");
                    }
                    if (!tileSet.Contains(emptyTile))
                    {
                        throw new Exception("The tile set does not contain the specified empty tile!");
                    }

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

                        // Set the explicit variant of the empty tile if applicable.
                        if (tile.Equals(emptyTile))
                        {
                            explicitEmptyTile = nonSymmetricTile;
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
                                // (variant.backWeights, variant.rightWeights, variant.frontWeights, variant.leftWeights) = (variant.leftWeights, variant.backWeights, variant.rightWeights, variant.frontWeights);

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


        New Infinite Modifying in Blocks Discussion:
        Have some way of identifying chunks around the player at any position. These chunks do have some overlap.
        Be able to identify which chunks around the player are not generated.
        Have the chunks to be loaded in a list that is generated in layers.
        */

        /// <summary>
        /// Generates a level using the MAC3 algorithm for WFC.
        /// </summary>
        public void GenerateLevel()
        {
            ClearGizmos();
            UnityEngine.Random.InitState(seed);

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

            if (ExplicitTileSet == null)
            {
                Debug.LogError("Could not generate explicit tileset before generation!");
            }
            else
            {
                // Spawn some empty tiles for the player.
                // Todo. Should this be done via code or preplaced in the editor? Best to use explicit empty tile if there is a variable for this anyway.

                // Spawn the rest of the level.
                SpawnChunksAroundPlayer();
            }
        }

        /// <summary>
        /// Spawns any chunks around the player up to the render distance. TODO: Make this and the render distance actually align.
        /// </summary>
        private void SpawnChunksAroundPlayer()
        {
            // Create a list to hold chunks to generate.
            List<Chunk> chunks = new List<Chunk>();

            // Get the player's chunk.
            Chunk playerChunk = new Chunk(Player.transform.position);

            // Get chunks around the player chunk. // TODO HAVE SOMETHING CHECK WHETHER A CHUNK IS GENERATED SO THAT IT ISN'T RESPAWNED IF ALREADY THERE. USE STARTING CELL OF LAYER 1 TO CHECK.
            for (int xChunkOffset = -numberOfChunks; xChunkOffset < numberOfChunks; xChunkOffset++)
            {
                for (int yChunkOffset = -numberOfChunks; yChunkOffset < numberOfChunks; yChunkOffset++)
                {
                    chunks.Add(new Chunk(playerChunk.y + yChunkOffset, playerChunk.x + xChunkOffset));
                }
            }

            // Spawn all the chunks in the list.
            SpawnChunks(chunks);
        }

        /// <summary>
        /// Spawns the chunk in a given list.
        /// </summary>
        /// <param name="chunks">The chunks to spawn.</param>
        private void SpawnChunks(List<Chunk> chunks)
        {
            foreach (Chunk chunk in chunks)
            {
                SpawnLayer(chunk.Layer1, chunk);
            }
            foreach (Chunk chunk in chunks)
            {
                SpawnLayer(chunk.Layer2, chunk);
            }
            foreach (Chunk chunk in chunks)
            {
                SpawnLayer(chunk.Layer3, chunk);
            }
            foreach (Chunk chunk in chunks)
            {
                SpawnLayer(chunk.Layer4, chunk);
            }
        }

        /// <summary>
        /// Spawns a single layer in a chunk.
        /// </summary>
        /// <param name="layerStartingCellCoordinate">The starting coordinate in the global grid of the layer.</param>
        /// <param name="parentChunk">The chunk that the layer is a part of.</param>
        private void SpawnLayer(Vector3 layerStartingCellCoordinate, Chunk parentChunk)
        {
            new LayerSpawner(layerStartingCellCoordinate, parentChunk).Spawn();
        }

        /// <summary>
        /// Converts global grid coordinates to world position relative to the level generation manager instance.
        /// </summary>
        /// <param name="gridCoordinates">The coordinates to convert.</param>
        /// <returns></returns>
        public Vector3 GridCoordinatesToWorldPos(Vector3 gridCoordinates)
        {
            return (gridCoordinates - this.transform.position) * tileSize;
        }

        /// <summary>
        /// Converts world positions relative to the level generation manager instance to global grid coordinates.
        /// </summary>
        /// <param name="worldPosition">The world position to convert.</param>
        /// <returns></returns>
        public Vector3 WorldPosToGridCoordinates(Vector3 worldPosition)
        {
            return (worldPosition / tileSize) + this.transform.position;
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

        // Gizmos for debugging. Used to draw colliders.
        public LinkedList<(Vector3, Vector3)> gizmosPosAndSize;
        void ClearGizmos()
        {
            gizmosPosAndSize = new LinkedList<(Vector3, Vector3)>();
        }

        // Visualise all gizmos (overlap box calls and box colliders).
        void OnDrawGizmos()
        {
            if (debugMode)
            {
                Gizmos.color = Color.red;
                foreach ((Vector3 gizmosPos, Vector3 gizmosSize) in gizmosPosAndSize)
                {
                    Gizmos.DrawWireCube(gizmosPos, gizmosSize);
                }
            }
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