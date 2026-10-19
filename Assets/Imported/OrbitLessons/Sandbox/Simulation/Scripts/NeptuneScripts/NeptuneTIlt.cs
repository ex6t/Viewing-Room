using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeptuneTilt : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
            // Neptune's axial tilt
            transform.localRotation = Quaternion.Euler(28.3f, 0f, 0f);
    }
}
