using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShowAndHideText : MonoBehaviour
{
    private TextMeshPro textComponent;
    private void Start()
    {
        // Get the Text component attached to the current GameObject
        textComponent = GetComponentInChildren<TextMeshPro>();


        if (textComponent != null)
        {
            Debug.Log(textComponent.text);
            textComponent.text = "Wish3D";
            StartCoroutine(wait_n_hide());

        }
        else
        {
            Debug.LogWarning("Text component not found on the current GameObject.");
        }
    }
    private IEnumerator wait_n_hide()
    {
        yield return new WaitForSeconds(10);
        textComponent.enabled = false;
        

    }
}