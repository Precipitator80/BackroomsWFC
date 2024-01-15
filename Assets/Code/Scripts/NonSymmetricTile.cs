namespace PrecipitatorWFC
{
    /// <summary>
    /// Non-symmetric tile with four different neighbours.
    /// </summary>
    public class NonSymmetricTile : Tile
    {
        public Tile[] backNeighbours; // Neighbours connecting to the back of the tile.
        public Tile[] rightNeighbours; // Neighbours connecting to the right of the tile.
        public Tile[] frontNeighbours; // Neighbours connecting to the front of the tile.
        public Tile[] leftNeighbours; // Neighbours connecting to the left of the tile.

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