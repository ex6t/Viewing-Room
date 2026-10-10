using System;
using UnityEngine;

// One clock connects the local sky to the inherited Earth orbit. Day zero is a
// teaching reference at perihelion, not a live date or a particular town.
public class EarthObservationModel : MonoBehaviour
{
    public EarthOrbitModel earthOrbit;
    public Transform globeAxis;
    public Transform globeSpin;
    public Transform observerMarker;
    public Light worldSun;
    public Transform skySun;
    public Transform observationOrigin;
    public Transform skyFrame;
    public Material skyMaterial;
    public string sunDirectionProperty = "_SunDirection";
    public float globeRadius = 0.11f;
    public float skySunDistance = 60f;
    public float hoursPerSecond = 1f;
    public float groundSunIntensity = 1.2f;
    public float spaceSunIntensity = 1.2f;

    public const double LatitudeDegrees = 35.0;
    public const double InitialSolarHours = 14.0;
    public const double InitialDays = 0.0;
    // A solar day includes Earth's progress around the Sun. Spin must not reset at an orbital wrap.
    public const double SiderealTurnsPerDay = 1.0 + 1.0 / EarthOrbitModel.PeriodDays;
    public double SimulationDays { get; private set; }
    public double SolarTimeHours { get; private set; }
    public double ObliquityDegrees { get; private set; }
    public double SolarDeclinationDegrees { get; private set; }
    public Vector3 SunDirectionLocal { get; private set; }
    public Vector3 ObserverNormalLocal { get; private set; }
    public Vector3 ObserverNormalWorld { get; private set; }
    public Vector3 AxisDirectionLocal { get; private set; }
    public Matrix4x4 StarsRotation { get; private set; }
    public bool IsSuspended { get; private set; } = true;

    double qx, qy, qz, tx, ty, tz, nx, ny, nz;
    double initialRotation, vernalRotation, latitudeSin, latitudeCos;
    Vector3 orbitSunDirection;
    bool initialized;
    bool groundView = true;
    int sunDirectionId;
    int worldToSkyId;
    int starsRotationId;

    public bool InitializeModel()
    {
        if (initialized) return true;
        if (earthOrbit == null || !earthOrbit.InitializeModel())
        {
            Debug.LogError("[EarthObservationModel] Assign a complete Earth orbit model.", this);
            return false;
        }
        Bergers.BergerSol.CalculateOrbitalParameters(EarthOrbitModel.ReferenceYear,
            out _, out double obliquity, out double perihelionLongitude);
        ObliquityDegrees = obliquity;
        // Berger's C# return already subtracts 180 degrees from the MATLAB longitude.
        double springAnomaly = WrapDegrees(180.0 - perihelionLongitude) * Math.PI / 180.0;
        vernalRotation = springAnomaly + Math.PI;
        GenerateRotation.GenerateRot.rotation(obliquity, springAnomaly, out double[,] rotation);
        // Original generate_rot_m.m transposes Rodrigues' matrix; keep the port unchanged
        // and take its rows here to reproduce the original equatorial frame.
        qx = rotation[0, 0]; qy = rotation[0, 1]; qz = rotation[0, 2];
        tx = rotation[1, 0]; ty = rotation[1, 1]; tz = rotation[1, 2];
        nx = rotation[2, 0]; ny = rotation[2, 1]; nz = rotation[2, 2];
        AxisDirectionLocal = ResearchToUnity(nx, ny, nz);
        latitudeSin = Math.Sin(LatitudeDegrees * Math.PI / 180.0);
        latitudeCos = Math.Cos(LatitudeDegrees * Math.PI / 180.0);
        // At perihelion the Sun direction is (-1,0,0). Set the observer to early afternoon.
        double solarRightAscension = Math.Atan2(-tx, -qx);
        initialRotation = solarRightAscension + (InitialSolarHours - 12.0) * Math.PI / 12.0;
        sunDirectionId = Shader.PropertyToID(sunDirectionProperty);
        worldToSkyId = Shader.PropertyToID("_WorldToSky");
        starsRotationId = Shader.PropertyToID("_StarsRotation");
        earthOrbit.SetSuspended(true);
        if (globeAxis != null)
            globeAxis.localRotation = Quaternion.LookRotation(ResearchToUnity(tx, ty, tz), AxisDirectionLocal);
        initialized = true;
        SetDays(InitialDays);
        return true;
    }

    public void SetSuspended(bool suspended)
    {
        IsSuspended = suspended;
        if (earthOrbit != null) earthOrbit.SetSuspended(true);
    }

    public void Scrub(float direction, float deltaSeconds)
    {
        if (IsSuspended || deltaSeconds <= 0f || hoursPerSecond <= 0f) return;
        SetDays(SimulationDays + (double)Mathf.Clamp(direction, -1f, 1f) * deltaSeconds * hoursPerSecond / 24.0);
    }

    public void SetDays(double days)
    {
        if (double.IsNaN(days) || double.IsInfinity(days) || !InitializeModel()) return;
        SimulationDays = days;
        earthOrbit.SetSuspended(true);
        earthOrbit.SetElapsedDays(days);
        UpdateObserver();
        UpdatePresentation();
    }

    public void ResetDay() { SetDays(InitialDays); }

    // Presentation changes views without changing simulation time or the scientific frame.
    public void ApplyView(bool viewFromGround)
    {
        groundView = viewFromGround;
        if (InitializeModel()) UpdatePresentation();
    }

    void UpdateObserver()
    {
        double anomaly = earthOrbit.TrueAnomalyAtDays(SimulationDays) * Math.PI / 180.0;
        double sx = -Math.Cos(anomaly), sy = -Math.Sin(anomaly);
        double spin = initialRotation + 2.0 * Math.PI * ((SimulationDays * SiderealTurnsPerDay) % 1.0);
        double cosine = Math.Cos(spin), sine = Math.Sin(spin);
        double mx = qx * cosine + tx * sine;
        double my = qy * cosine + ty * sine;
        double mz = qz * cosine + tz * sine;
        double eastX = -qx * sine + tx * cosine;
        double eastY = -qy * sine + ty * cosine;
        double upX = latitudeCos * mx + latitudeSin * nx;
        double upY = latitudeCos * my + latitudeSin * ny;
        double upZ = latitudeCos * mz + latitudeSin * nz;
        double northX = -latitudeSin * mx + latitudeCos * nx;
        double northY = -latitudeSin * my + latitudeCos * ny;
        double sunEast = sx * eastX + sy * eastY;
        double sunUp = sx * upX + sy * upY;
        double sunNorth = sx * northX + sy * northY;
        SunDirectionLocal = new Vector3((float)sunEast, (float)sunUp, (float)sunNorth);
        ObserverNormalLocal = ResearchToUnity(upX, upY, upZ);
        ObserverNormalWorld = earthOrbit.orbitPlane.TransformDirection(ObserverNormalLocal).normalized;
        orbitSunDirection = ResearchToUnity(sx, sy, 0.0);
        double hourAngle = Math.Atan2(-sunEast, sx * mx + sy * my);
        SolarTimeHours = (12.0 + hourAngle * 12.0 / Math.PI + 24.0) % 24.0;
        SolarDeclinationDegrees = Math.Asin(Math.Max(-1.0, Math.Min(1.0, sx * nx + sy * ny))) * 180.0 / Math.PI;
        UpdateStars(spin - vernalRotation);
        // Research XY -> Unity XZ reverses handedness, hence the negative local Y spin.
        if (globeSpin != null) globeSpin.localRotation = Quaternion.Euler(0f, (float)(-spin * 180.0 / Math.PI), 0f);
        if (observerMarker != null)
        {
            observerMarker.position = earthOrbit.earth.position +
                earthOrbit.orbitPlane.TransformVector(ObserverNormalLocal * globeRadius);
            observerMarker.rotation = Quaternion.FromToRotation(Vector3.up, ObserverNormalWorld);
        }
    }

    void UpdateStars(double meridianAngle)
    {
        double cosine = Math.Cos(meridianAngle), sine = Math.Sin(meridianAngle);
        // The reused star cube has north at +Y, RA0 at +Z and RA18h at +X.
        // Map the rotating local east/up/north frame into that fixed celestial sky.
        Matrix4x4 rotation = Matrix4x4.identity;
        rotation.m00 = (float)-cosine;
        rotation.m01 = (float)(-latitudeCos * sine);
        rotation.m02 = (float)(latitudeSin * sine);
        rotation.m10 = 0f;
        rotation.m11 = (float)latitudeSin;
        rotation.m12 = (float)latitudeCos;
        rotation.m20 = (float)-sine;
        rotation.m21 = (float)(latitudeCos * cosine);
        rotation.m22 = (float)(-latitudeSin * cosine);
        StarsRotation = rotation;
    }

    void UpdatePresentation()
    {
        Quaternion horizonRotation = skyFrame != null ? skyFrame.rotation : Quaternion.identity;
        Vector3 groundSunDirection = horizonRotation * SunDirectionLocal;
        if (skySun != null && observationOrigin != null)
        {
            skySun.position = observationOrigin.position + groundSunDirection * skySunDistance;
            skySun.rotation = Quaternion.LookRotation(-groundSunDirection, horizonRotation * Vector3.up);
        }
        if (worldSun != null)
        {
            Vector3 direction = groundView ? groundSunDirection : earthOrbit.orbitPlane.TransformDirection(orbitSunDirection);
            worldSun.transform.rotation = Quaternion.LookRotation(-direction);
            worldSun.intensity = groundView ? groundSunIntensity * Mathf.Clamp01((SunDirectionLocal.y + 0.02f) / 0.12f) : spaceSunIntensity;
        }
        if (skyMaterial != null)
        {
            skyMaterial.SetMatrix(worldToSkyId, Matrix4x4.Rotate(Quaternion.Inverse(horizonRotation)));
            skyMaterial.SetMatrix(starsRotationId, StarsRotation);
            if (skyMaterial.HasProperty(sunDirectionId))
                skyMaterial.SetVector(sunDirectionId, new Vector4(SunDirectionLocal.x, SunDirectionLocal.y, SunDirectionLocal.z, 0f));
        }
    }

    static double WrapDegrees(double degrees) { return ((degrees % 360.0) + 360.0) % 360.0; }
    static Vector3 ResearchToUnity(double x, double y, double z) { return new Vector3((float)x, (float)z, (float)y); }
    void OnDisable() { SetSuspended(true); }
}
