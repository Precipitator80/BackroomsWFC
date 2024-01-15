namespace PrecipitatorWFC
{
    /// <summary>
    /// Three-way tile with three outsides and one back side.
    /// </summary>
    public class ThreeWayTile : Tile
    {
        public Tile[] outsideNeighbours; // Neighbours connecting to the open / front / outside of the three-way.
        public Tile[] backNeighbours; // Neighbours connecting behind the three-way.

        protected override Tile[] PossibleNeighbours(int relativeCardinality)
        {
            if (relativeCardinality == 0)
            {
                return backNeighbours;
            }
            return outsideNeighbours;
        }
    }
}