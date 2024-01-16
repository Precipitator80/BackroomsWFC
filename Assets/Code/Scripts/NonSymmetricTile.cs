using System;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Non-symmetric tile with four different neighbours.
    /// </summary>
    public class NonSymmetricTile : Tile
    {
        public Tile[] backNeighbours; // Neighbours connecting to the back of the tile.
        public int[] backRotations; // Rotations of neighbours at the back of the tile.
        public Tile[] rightNeighbours; // Neighbours connecting to the right of the tile.
        public int[] rightRotations; // Rotations of neighbours on the right of the tile.
        public Tile[] frontNeighbours; // Neighbours connecting to the front of the tile.
        public int[] frontRotations; // Rotations of neighbours on the front of the tile.
        public Tile[] leftNeighbours; // Neighbours connecting to the left of the tile.
        public int[] leftRotations; // Rotations of neighbours on the left of the tile.

        public override NonSymmetricTile InstantiateNonSymmetricTile()
        {
            // Instantiate the tile and copy neighbours from the original tile by value to separate references.
            // If this is not done, then variants cannot be created properly.
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