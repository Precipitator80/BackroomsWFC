namespace Core.Data
{
    public class WaveFunctionCollapseModelParams
    {
        public WaveFunctionCollapseModelParams(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        /// <summary>
        /// X dimension of the output data
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Y dimension of the output data
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Z dimension of the output data
        /// </summary>
        public readonly int Depth;
    }
}