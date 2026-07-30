using UnityEngine;
<<<<<<< Updated upstream
=======
using System.Collections.Generic;
>>>>>>> Stashed changes

public class RandomObjectSpawner : MonoBehaviour
{
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 4f;
    public bool usePooling = false;
<<<<<<< Updated upstream
=======
    public List<GameObject> objectList;
>>>>>>> Stashed changes

    public void ReturnToPool(GameObject obj)
    {
        Destroy(obj);
    }
}
