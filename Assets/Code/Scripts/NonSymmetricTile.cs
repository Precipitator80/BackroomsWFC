using System;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Non-symmetric tile with four different neighbours.
    /// </summary>
    public class NonSymmetricTile : Tile
    {
        // Information for neighbours connecting to the back of the tile.
        public Tile[] backNeighbours; // Neighbour tiles.
        public int[] backRotations; // Valid rotations / cardinalities of the neighbour tiles.
        public int[] backWeights; // Weights of the neighbour tiles.

        // Information for neighbours connecting to the right of the tile.
        public Tile[] rightNeighbours;
        public int[] rightRotations;
        public int[] rightWeights;

        // Information for neighbours connecting to the front of the tile.
        public Tile[] frontNeighbours;
        public int[] frontRotations;
        public int[] frontWeights;

        // Information for neighbours connecting to the left of the tile.
        public Tile[] leftNeighbours;
        public int[] leftRotations;
        public int[] leftWeights;

        public override NonSymmetricTile InstantiateNonSymmetricTile()
        {
            // Instantiate the tile and copy neighbours from the original tile by value to separate references.
            // If this is not done, then variants cannot be created properly.

            if (backNeighbours.Length != backRotations.Length   //|| backRotations.Length != backWeights.Length
             || rightNeighbours.Length != rightRotations.Length //|| rightRotations.Length != rightWeights.Length
             || frontNeighbours.Length != frontRotations.Length //|| frontRotations.Length != frontWeights.Length
             || leftNeighbours.Length != leftRotations.Length)  //|| leftRotations.Length != leftWeights.Length)
            {
                throw new Exception("Ensure that the tile '" + name + "' has equal length of neighbour, rotation and weight information.");
            }

            NonSymmetricTile copy = Instantiate(this);
            Array.Copy(backNeighbours, copy.backNeighbours, backNeighbours.Length);
            Array.Copy(rightNeighbours, copy.rightNeighbours, rightNeighbours.Length);
            Array.Copy(frontNeighbours, copy.frontNeighbours, frontNeighbours.Length);
            Array.Copy(leftNeighbours, copy.leftNeighbours, leftNeighbours.Length);
            return copy;
        }

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            switch (relativeCardinality)
            {
                case 0:
                    return backNeighbours;
                case 1:
                    return rightNeighbours;
                case 2:
                    return frontNeighbours;
                default:
                    return leftNeighbours;
            }
        }
    }
}