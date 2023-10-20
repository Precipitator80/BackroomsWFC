namespace Core.Data.SimpleTiledModel
{
    public class SimpleTiledModelParams : WaveFunctionCollapseModelParams
    {
        public const string DEFAULT_SUBSET = "Default";

        /// <summary>
        /// Determines if the output solutions are tilable. It's useful for creating things like tileable textures,
        /// but also has a surprising influence on the output. When working with WFC, it's often a good idea to toggle
        /// Periodic Output on and off, checking if either setting influences the results in a favorable way.
        /// </summary>
        public readonly bool Periodic;

        /// <summary>
        /// Defines which subset of tiles to use from Input data
        /// </summary>
        public readonly string SubsetName;

        public SimpleTiledModelParams(int width, int height, int depth, bool periodic = false, string subsetName = DEFAULT_SUBSET) : base(width, height, depth)
        {
            Periodic = periodic;
            SubsetName = subsetName;
        }
    }
}