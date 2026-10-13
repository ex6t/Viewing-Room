using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UranusTilt : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Uranus' axial tilt
        transform.localRotation = Quaternion.Euler(98f, 0f, 0f);
    }
}
