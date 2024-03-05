using System;
using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Chunk class to support infinite generation for infinite modifying in blocks.
    /// </summary>
    public class Chunk
    {
        /// <summary>
        /// Calculates the layer size based on the chunk size. Each chunk is divided into four layers as part of infinite modifying in blocks to allow for parallel, deterministic generation.
        /// </summary>
        public static int LayerSize
        {
            get
            {
                return Mathf.Clamp((int)(0.8f * LevelGenerationManager.Instance.chunkSize), 2, LevelGenerationManager.Instance.chunkSize - 1);
            }
        }

        public int y; // The chunk number in the y axis.
        public int x; // The chunk number in the x axis.
        public Unity.Mathematics.Random rng; // A random number generator used to make cell choices. A separate one is needed for each chunk to allow for parallel, deterministic generation.

        /// <summary>
        /// Constructor that initialises the chunk from a given position.
        /// Used to generate a chunk at the player's position.
        /// </summary>
        /// <param name="worldPosition">The position in the grid the chunk should be generated for.</param>
        public Chunk(Vector3 worldPosition)
        {
            // Partially used reference.
            // How can I compute which chunk a coordinate is in? user1430 - https://gamedev.stackexchange.com/questions/94021/how-can-i-compute-which-chunk-a-coordinat -e-is-in - 06.02.2024
            Vector3Int chunkCoordinates = LevelGenerationManager.Instance.WorldPosToChunkCoordinates(worldPosition);
            x = chunkCoordinates.x;
            y = chunkCoordinates.z;
            InitialiseRNG();
        }

        /// <summary>
        /// Alternative constructor that uses chunk numbers directly.
        /// The chunk identified from the player position can be used with offsets to generate adjacent chunks.
        /// </summary>
        /// <param name="y">The chunk number in the y axis.</param>
        /// <param name="x">The chunk number in the x axis.</param>
        public Chunk(int y, int x)
        {
            this.y = y;
            this.x = x;
            InitialiseRNG();
        }

        /// <summary>
        /// Initialises the RNG with a seed based on the chunk numbers to give each chunk a unique seed.
        /// The global seed is added to make it affect the RNG of each chunk.
        /// </summary>
        private void InitialiseRNG()
        {
            // Calculate the seed based on the chunk numbers and global seed.
            uint seed = (uint)Math.Abs(Math.Pow(x, 3) + Math.Pow(y, 3) + LevelGenerationManager.Instance.seed);

            // The RNG cannot be initialised with seed 0, so use 1 in this special case.
            if (seed == 0)
            {
                seed = 1;
            }

            // Initialise the RNG with the calculated seed.
            rng = new Unity.Mathematics.Random(seed);
        }

        public void Unload()
        {

        }

        ////// Getters to return the starting cell coordinates of each layer.

        /// <summary>
        /// Gets the starting coordinate / origin of layer 1, which has no offset from the chunk's origin.
        /// </summary>
        public Vector3 Layer1
        {
            get
            {
                return new Vector3(x * LevelGenerationManager.Instance.chunkSize, 0f, y * LevelGenerationManager.Instance.chunkSize);
            }
        }

        /// <summary>
        /// Gets the starting coordinate / origin of layer 2, which has offset in the x direction from the chunk's origin.
        /// </summary>
        public Vector3 Layer2
        {
            get
            {
                return new Vector3(x * LevelGenerationManager.Instance.chunkSize + (int)(LevelGenerationManager.Instance.chunkSize / 2), 0f, y * LevelGenerationManager.Instance.chunkSize);
            }
        }

        /// <summary>
        /// Gets the starting coordinate / origin of layer 3, which has offset in the y direction from the chunk's origin.
        /// </summary>
        public Vector3 Layer3
        {
            get
            {
                return new Vector3(x * LevelGenerationManager.Instance.chunkSize, 0f, y * LevelGenerationManager.Instance.chunkSize + (int)(LevelGenerationManager.Instance.chunkSize / 2));
            }
        }

        /// <summary>
        /// Gets the starting coordinate / origin of layer 4, which has offset in both the x and y direction from the chunk's origin.
        /// </summary>
        public Vector3 Layer4
        {
            get
            {
                return new Vector3(x * LevelGenerationManager.Instance.chunkSize + (int)(LevelGenerationManager.Instance.chunkSize / 2), 0f, y * LevelGenerationManager.Instance.chunkSize + (int)(LevelGenerationManager.Instance.chunkSize / 2));
            }
        }

        /// <summary>
        /// Function to print chunk information.
        /// </summary>
        /// <returns>A string of chunk information.</returns>
        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }
}