namespace PrecipitatorWFC
{
    /// <summary>
    /// Wall tile with a front and back side as well as two equal intermediate sides.
    /// </summary>
    public class WallTile : Tile
    {
        public Tile[] frontNeighbours; // Neighbours connecting to the open / front of the wall.
        public Tile[] sideNeighbours; // Neighbours connecting to the side of the wall to continue it.
        public Tile[] backNeighbours; // Neighbours connecting behind the wall.

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            switch (relativeCardinality)
            {
                case 0:
                    return backNeighbours;
                case 2:
                    return frontNeighbours;
                default:
                    return sideNeighbours;
            }
        }
    }
}