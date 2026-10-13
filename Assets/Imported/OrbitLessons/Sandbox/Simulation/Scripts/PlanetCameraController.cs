using UnityEngine;


[RequireComponent(typeof(Camera))]
public class PlanetCameraController : MonoBehaviour
{
    [Tooltip("The planet Transform this camera follows (assign the planet root).")]
    public Transform planet;

    [Tooltip("Optional Sun transform. When assigned and useSunDirection=true, camera will be placed on the side of the planet away from the Sun.")]
    public Transform sun;

    [Tooltip("If true and a Sun is provided, the camera will be positioned along the planet->sun axis (outside, looking toward planet).")]
    public bool useSunDirection = true;

    [Tooltip("Extra distance from the planet's visible surface (in world units).")]
    public float distanceFromSurface = 2.0f;

    [Tooltip("Additional upward offset (world units) relative to planet center (helps avoid horizon clipping).")]
    public float heightOffset = 0.5f;

    [Tooltip("If true, the camera will be unparented at Start so it does not inherit planet rotation.")]
    public bool unparentOnStart = true;

    [Tooltip("If true, camera will slowly orbit the planet in addition to staying at the computed distance.")]
    public bool orbitAroundPlanet = false;
    [Tooltip("Degrees per second for orbiting camera.")]
    public float orbitSpeedDegPerSec = 10f;
    [Tooltip("Orbit radius addition (overrides Z if orbitAroundPlanet is true).")]
    public float orbitRadiusAddition = 0f;

    [Tooltip("Whether the camera should LookAt the planet each frame.")]
    public bool lookAtPlanet = true;

    [Header("Optional override if renderer bounds are not correct")]
    [Tooltip("If > 0, this will be used as the planet radius instead of estimating from renderers.")]
    public float radiusOverride = 0f;

    // Internal
    private float orbitAngle = 0f;

    void Start()
    {
        if (planet == null)
        {
            Debug.LogWarning("[PlanetCameraController] planet not assigned on " + gameObject.name);
            return;
        }

        if (unparentOnStart && transform.parent != null)
        {
            // detach to avoid inheriting planet's rotation while keeping current world transform
            transform.SetParent(null, true);
        }
    }

    void LateUpdate()
    {
        if (planet == null)
            return;

        // determine planet "radius" from renderer(s) or override
        float planetRadius = radiusOverride;

        // fallback if no renderer found and no override set
        if (planetRadius <= 0.0001f)
            planetRadius = 1.0f;

        // choose direction for camera placement
        Vector3 dir;
        if (useSunDirection && sun != null)
        {
            // place camera on side opposite the sun (so camera looks at the planet from the lit side or consistent direction)
            dir = (planet.position - sun.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.back;
        }
        else
        {
            // prefer to keep same rough direction from planet to current camera position, but guard if overlapping
            dir = (transform.position - planet.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = -planet.forward; // fallback
        }

        Vector3 desiredPos;

        if (orbitAroundPlanet)
        {
            orbitAngle += orbitSpeedDegPerSec * Time.deltaTime;
            float rad = Mathf.Deg2Rad * orbitAngle;
            float orbitRadius = planetRadius + distanceFromSurface + orbitRadiusAddition;
            // orbit around planet's up axis in world space
            Vector3 orbitOffset = new Vector3(Mathf.Sin(rad) * orbitRadius, heightOffset, Mathf.Cos(rad) * orbitRadius);
            desiredPos = planet.position + orbitOffset;
        }
        else
        {
            float distance = planetRadius + distanceFromSurface;
            desiredPos = planet.position + dir * distance + planet.up * heightOffset;
        }

        // set position and orientation
        transform.position = desiredPos;

        if (lookAtPlanet)
        {
            // ensure up is world up for stable horizon; use planet.up if you want camera aligned to planet axis
            transform.rotation = Quaternion.LookRotation((planet.position - transform.position).normalized, Vector3.up);
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (planet == null)
            return;
        float r = radiusOverride;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(planet.position, r);
        Gizmos.color = Color.green;
        // show target position
        Vector3 debugDir = useSunDirection && sun != null ? (planet.position - sun.position).normalized : ((transform.position - planet.position).normalized);
        if (debugDir.sqrMagnitude < 0.0001f) debugDir = -planet.forward;
        Vector3 debugPos = planet.position + debugDir * (r + distanceFromSurface) + planet.up * heightOffset;
        Gizmos.DrawSphere(debugPos, 0.05f);
    }
#endif
}