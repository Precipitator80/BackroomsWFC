namespace Core.Data.SimpleTiledModel
{
    public class NeighborData
    {
        public readonly TileConfig LeftNeighborConfig;
        public readonly TileConfig RightNeighborConfig;
        public readonly int LeftRotation;
        public readonly int RightRotation;
        public readonly bool Horizontal;

        public NeighborData(TileConfig leftNeighborConfig, TileConfig rightNeighborConfig, int leftRotation, int rightRotation, bool horizontal = true)
        {
            LeftNeighborConfig = leftNeighborConfig;
            RightNeighborConfig = rightNeighborConfig;
            LeftRotation = leftRotation;
            RightRotation = rightRotation;
            Horizontal = horizontal;
        }
    }
}