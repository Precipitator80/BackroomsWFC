using System;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Cube tile with equal neighbours on all sides.
    /// </summary>
    public class CubeTile : Tile
    {
        public Tile[] neighbours; // Neighbour tiles connecting to any side of the cube.
        public int[] backRotations; // Valid rotations / cardinalities of neighbours when placed at the back of the cube tile.
        public int[] weights; // Weights of the neighbour tiles.

        public override NonSymmetricTile InstantiateNonSymmetricTile()
        {
            if (neighbours.Length != backRotations.Length) //|| backRotations.Length != weights.Length)
            {
                throw new Exception("Ensure that the tile '" + name + "' has equal length of neighbour, rotation and weight information.");
            }

            // Create a copy of the tile and add a non-symmetric tile component with the same components, removing the Cube tile component.
            NonSymmetricTile nonSymmetricTile = Instantiate(gameObject).AddComponent<NonSymmetricTile>();
            nonSymmetricTile.model = this.model;
            nonSymmetricTile.weight = this.weight;
            DestroyImmediate(nonSymmetricTile.gameObject.GetComponent<CubeTile>());

            // Copy neighbour information into each side of the time.
            nonSymmetricTile.backNeighbours = new Tile[neighbours.Length];
            nonSymmetricTile.rightNeighbours = new Tile[neighbours.Length];
            nonSymmetricTile.frontNeighbours = new Tile[neighbours.Length];
            nonSymmetricTile.leftNeighbours = new Tile[neighbours.Length];
            Array.Copy(neighbours, nonSymmetricTile.backNeighbours, neighbours.Length);
            Array.Copy(neighbours, nonSymmetricTile.rightNeighbours, neighbours.Length);
            Array.Copy(neighbours, nonSymmetricTile.frontNeighbours, neighbours.Length);
            Array.Copy(neighbours, nonSymmetricTile.leftNeighbours, neighbours.Length);

            /*
            nonSymmetricTile.backWeights = new int[weights.Length];
            nonSymmetricTile.rightWeights = new int[weights.Length];
            nonSymmetricTile.frontWeights = new int[weights.Length];
            nonSymmetricTile.leftWeights = new int[weights.Length];
            Array.Copy(weights, nonSymmetricTile.backWeights, weights.Length);
            Array.Copy(weights, nonSymmetricTile.rightWeights, weights.Length);
            Array.Copy(weights, nonSymmetricTile.frontWeights, weights.Length);
            Array.Copy(weights, nonSymmetricTile.leftWeights, weights.Length);
            */

            // Set up rotation arrays. Keep the back rotation the same, but shift the rotation of the neighbour for each other side correspondingly.
            nonSymmetricTile.backRotations = new int[backRotations.Length];
            nonSymmetricTile.rightRotations = new int[backRotations.Length];
            nonSymmetricTile.frontRotations = new int[backRotations.Length];
            nonSymmetricTile.leftRotations = new int[backRotations.Length];
            Array.Copy(backRotations, nonSymmetricTile.backRotations, backRotations.Length);
            for (int i = 0; i < neighbours.Length; i++)
            {
                nonSymmetricTile.rightRotations[i] = (backRotations[i] + 1) % 4;
                nonSymmetricTile.frontRotations[i] = (backRotations[i] + 2) % 4;
                nonSymmetricTile.leftRotations[i] = (backRotations[i] + 3) % 4;
            }

            return nonSymmetricTile;
        }

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            return neighbours;
        }
    }
}