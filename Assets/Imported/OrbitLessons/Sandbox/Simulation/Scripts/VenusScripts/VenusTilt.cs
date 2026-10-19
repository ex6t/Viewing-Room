using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VenusTilt : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

            // Venus Axial Tilt
            transform.localRotation = Quaternion.Euler(177.3f, 0f, 0f);

    }
}
