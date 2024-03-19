using PrecipitatorWFC;
using UnityEngine;

public class FloorSpawner : MonoBehaviour
{
    public GameObject floor;

    public void SpawnFloor()
    {
        if (floor != null)
        {
            // Spawn a floor under the tile.
            GameObject newFloor = Instantiate(floor, this.transform, true);
            newFloor.transform.localPosition = Vector3.zero;
            //newFloor.transform.position = new Vector3(this.transform.position.x, this.transform.position.y - LevelGenerationManager.Instance.tileSize / 2, this.transform.position.z);
            newFloor.SetActive(true);
        }
    }
}