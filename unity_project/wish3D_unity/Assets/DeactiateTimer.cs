using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DeactivatePrefab : MonoBehaviour
{
    public GameObject prefabToDeactivate; // Assign this in the Inspector

    void Start()
    {
        if (prefabToDeactivate != null)
        {
            StartCoroutine(DeactivateAfterTime(10));
        }
    }

    IEnumerator DeactivateAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        prefabToDeactivate.SetActive(false);
    }
}
