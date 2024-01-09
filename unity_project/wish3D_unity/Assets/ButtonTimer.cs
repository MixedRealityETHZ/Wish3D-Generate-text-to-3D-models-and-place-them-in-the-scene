using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonTimer : MonoBehaviour
{
    public Button myButton;

    void Start()
    {
        if (myButton != null)
        {
            myButton.gameObject.SetActive(false);
            StartCoroutine(ShowButtonAfterTime(10));
        }
    }

    IEnumerator ShowButtonAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        myButton.gameObject.SetActive(true);
    }
}
