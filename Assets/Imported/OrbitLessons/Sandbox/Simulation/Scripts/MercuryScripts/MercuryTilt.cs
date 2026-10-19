using UnityEngine;

public class MercuryTilt : MonoBehaviour
{
    void Start()
    {
        // Mercury Axial Tilt
        transform.localRotation = Quaternion.Euler(57f, 0f, 0f);
    }
}
