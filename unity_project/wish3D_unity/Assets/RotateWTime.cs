using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float rotationSpeed = 100f; // Rotation speed in degrees per second

    void Update()
    {
        // Rotate around the Y axis
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}

