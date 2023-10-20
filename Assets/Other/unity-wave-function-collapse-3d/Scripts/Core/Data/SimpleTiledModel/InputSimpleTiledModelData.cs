using System.Collections.Generic;

namespace Core.Data.SimpleTiledModel
{
    public class InputSimpleTiledModelData
    {
        public readonly TileConfigData<SimpleTiledModelTileConfig> TileConfigData;

        public InputSimpleTiledModelData(TileConfigData<SimpleTiledModelTileConfig> tileConfigData)
        {
            TileConfigData = tileConfigData;
        }

        public List<NeighborData> NeighborDatas { get; private set; }

        public void SetNeighbors(List<NeighborData> neighborDatas)
        {
            NeighborDatas = neighborDatas;
        }
    }
}