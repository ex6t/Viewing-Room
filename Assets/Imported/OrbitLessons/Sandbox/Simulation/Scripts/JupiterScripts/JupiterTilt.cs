using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JupiterTilt : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        // Jupiter Axial Tilt
        transform.localRotation = Quaternion.Euler(3, 0f, 0f);

    }
}
