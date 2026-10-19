using UnityEngine;

public class MarsTilt : MonoBehaviour
{
    void Start()
    {
        // Mars axial tilt
        transform.localRotation = Quaternion.Euler(25.19f, 0f, 0f);
    }
}
