namespace Core.Data
{
    public class CellState
    {
        public readonly double EntropyLevel;

        /// <summary>
        /// Going to be null if Entropy level is more than 0
        /// </summary>
        public readonly ITile Tile;

        public CellState(double entropyLevel, ITile tileIndex)
        {
            EntropyLevel = entropyLevel;
            Tile = tileIndex;
        }

        public bool Collapsed
        {
            get { return Tile != null; }
        }
    }
}