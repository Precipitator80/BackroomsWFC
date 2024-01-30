using UnityEngine;

namespace PrecipitatorWFC
{
    /// <summary>
    /// Holds a slot for a cell reference that can be attached to a prefab spawned in the level.
    /// This allows for previous cell information to be read in future chunks.
    /// </summary>
    public class CellReference : MonoBehaviour
    {
        public Cell cell;
    }
}