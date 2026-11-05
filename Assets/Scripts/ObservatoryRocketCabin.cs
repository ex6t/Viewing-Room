using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

//the cabin stays still while the scenery follows our teaching altitude
public sealed class ObservatoryRocketCabin
{
    internal const float SpaceSkyStartMetres = 55000f;
    public Color SkyColor { get; private set; }

    const float earthRadius = 58f * 2.2f;
    static readonly Vector3 farEarth = new Vector3(32f, -71f, 154f) * 2.2f;
    static readonly Vector3 earthBearing = Vector3.ProjectOnPlane(farEarth, Vector3.up).normalized;
    static readonly float farEarthAngle = Mathf.Asin(earthRadius / farEarth.magnitude);
    static readonly float farEarthLimb = Mathf.Asin(farEarth.y / farEarth.magnitude) + farEarthAngle;
    readonly Transform root, city, clouds, airplane, earth;
    readonly MeshRenderer earthSurface;
    readonly MaterialPropertyBlock earthLighting = new MaterialPropertyBlock();
    static readonly int sunDirectionId = Shader.PropertyToID("_ModelSunDirection");
    static readonly int hazeColorId = Shader.PropertyToID("_FlightHazeColor");
    static readonly int hazeId = Shader.PropertyToID("_FlightHaze");
    static readonly int atmosphereId = Shader.PropertyToID("_FlightAtmosphere");
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();
    readonly Material surface;
    readonly TMP_FontAsset font;
    readonly int layer;
    readonly TMP_Text altitudeText, layerText, controlsText;
    readonly RectTransform altitudeMarker;

    public ObservatoryRocketCabin(Transform parent, int layer, Material surfaceMaterial, TMP_FontAsset font,
        Mesh earthMesh, Material earthMaterial)
    {
        this.layer = layer;
        surface = surfaceMaterial;
        this.font = font;
        root = Group("Rocket Cabin Prototype", parent);
        Material hull = Tint("Cabin navy", new Color(0.035f, 0.055f, 0.085f));
        Material trim = Tint("Cabin trim", new Color(0.24f, 0.32f, 0.39f));
        Material cyan = Tint("Cabin lights", new Color(0.20f, 0.88f, 0.92f));
        Material seat = Tint("Seat", new Color(0.08f, 0.13f, 0.18f));

        Box("Floor", root, new Vector3(0f, -1.48f, 0.15f), new Vector3(3.5f, 0.12f, 4.3f), hull);
        Box("Ceiling", root, new Vector3(0f, 1.15f, 0.15f), new Vector3(3.5f, 0.14f, 4.3f), hull);
        Box("Left wall", root, new Vector3(-1.73f, -0.16f, 0.15f), new Vector3(0.12f, 2.55f, 4.3f), hull);
        Box("Right wall", root, new Vector3(1.73f, -0.16f, 0.15f), new Vector3(0.12f, 2.55f, 4.3f), hull);
        Box("Rear wall", root, new Vector3(0f, -0.16f, -1.95f), new Vector3(3.5f, 2.55f, 0.12f), hull);
        Box("Window lower hull", root, new Vector3(0f, -1f, 1.86f), new Vector3(3.5f, 0.94f, 0.16f), hull);
        Box("Window upper hull", root, new Vector3(0f, 1.02f, 1.86f), new Vector3(3.5f, 0.24f, 0.16f), hull);
        Box("Window left hull", root, new Vector3(-1.58f, 0.18f, 1.86f), new Vector3(0.34f, 1.48f, 0.16f), hull);
        Box("Window right hull", root, new Vector3(1.58f, 0.18f, 1.86f), new Vector3(0.34f, 1.48f, 0.16f), hull);
        Box("Window top frame", root, new Vector3(0f, 0.89f, 1.75f), new Vector3(2.9f, 0.09f, 0.17f), trim);
        Box("Window sill", root, new Vector3(0f, -0.48f, 1.72f), new Vector3(2.9f, 0.10f, 0.28f), trim);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Window upright", root, new Vector3(side * 1.43f, 0.20f, 1.75f), new Vector3(0.09f, 1.46f, 0.17f), trim);
            Box("Window light", root, new Vector3(side * 1.36f, 0.20f, 1.66f), new Vector3(0.018f, 1.23f, 0.025f), cyan);
            Box("Floor light", root, new Vector3(side * 1.52f, -1.40f, 0.13f), new Vector3(0.025f, 0.018f, 3.65f), cyan);
            Box("Seat arm", root, new Vector3(side * 0.52f, -0.76f, -0.30f), new Vector3(0.13f, 0.13f, 0.72f), trim);
        }
        Box("Seat cushion", root, new Vector3(0f, -1.03f, -0.39f), new Vector3(0.95f, 0.16f, 0.82f), seat);
        Box("Seat back", root, new Vector3(0f, -0.54f, -0.83f), new Vector3(0.95f, 1.15f, 0.16f), seat);
        Box("Dashboard", root, new Vector3(0f, -0.60f, 1.45f), new Vector3(2.45f, 0.46f, 0.16f), hull);
        Box("Dashboard rim", root, new Vector3(0f, -0.35f, 1.44f), new Vector3(2.48f, 0.025f, 0.20f), cyan);

        Transform panel = Panel("Altitude Dashboard", root);
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1150f, 250f);
        panel.localPosition = new Vector3(0f, -0.60f, 1.35f);
        panel.localScale = Vector3.one * 0.00165f;
        Text(panel, "ROCKET ASCENT", new Vector2(-115f, 85f), new Vector2(790f, 36f), 24f, Color.white);
        altitudeText = Text(panel, "0 m", new Vector2(-115f, 30f), new Vector2(790f, 67f), 48f, new Color(0.35f, 1f, 0.98f));
        layerText = Text(panel, "Troposphere", new Vector2(-115f, -25f), new Vector2(790f, 40f), 23f, Color.white);
        controlsText = Text(panel, "X: pause   A/Enter: skip   B: cancel", new Vector2(-115f, -73f), new Vector2(790f, 34f), 21f, new Color(0.72f, 0.83f, 0.88f));
        Image(panel, "Altitude line", new Vector2(360f, 0f), new Vector2(3f, 180f), new Color(0.43f, 0.62f, 0.70f));
        for (int tick = 0; tick <= 4; tick++)
        {
            float y = -90f + tick * 45f;
            Image(panel, "Altitude tick", new Vector2(369f, y), new Vector2(20f, 2f), Color.white);
            Text(panel, (tick * 25).ToString() + (tick == 4 ? " km" : ""), new Vector2(432f, y), new Vector2(92f, 27f), 19f, Color.white);
        }
        altitudeMarker = Image(panel, "Altitude marker", new Vector2(345f, -90f), new Vector2(34f, 8f), new Color(0.35f, 1f, 0.98f));
        Text(root, "Time-compressed ascent / approximate layer boundaries", new Vector3(0f, 0.89f, 1.65f), new Vector2(1150f, 36f), 19f, 0.00165f);

        city = Group("Launch landscape", root);
        BuildCity(city);
        clouds = Group("Cloud landmarks", root);
        Material white = Tint("Cloud white", new Color(0.93f, 0.97f, 1f));
        for (int i = 0; i < 6; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (12f + (i % 3) * 8f);
            for (int puff = 0; puff < 3; puff++)
                Piece("Cloud", clouds, PrimitiveType.Sphere, new Vector3(x + puff * 4f, puff % 2, 26f + i * 9f), new Vector3(12f, 3.1f, 7f), white);
        }
        airplane = Group("Aircraft landmark", root);
        Material plane = Tint("Aircraft white", new Color(0.82f, 0.88f, 0.93f));
        Piece("Aircraft body", airplane, PrimitiveType.Sphere, Vector3.zero, new Vector3(5.6f, 0.5f, 0.65f), plane);
        Box("Aircraft wings", airplane, new Vector3(-0.3f, 0f, 0f), new Vector3(1.2f, 0.10f, 6f), plane);
        Box("Aircraft tail wings", airplane, new Vector3(-2f, 0.05f, 0f), new Vector3(0.7f, 0.10f, 2.1f), plane);
        Box("Aircraft tail", airplane, new Vector3(-2.15f, 0.56f, 0f), new Vector3(0.8f, 1.1f, 0.10f), plane);

        earth = Group("Upper atmosphere horizon", root);
        earth.localScale = Vector3.one * earthRadius;
        //reuse the globe without copying its orbit scripts or California pointer
        Mesh flightMesh = RefineFlightEarth(earthMesh);
        float meshScale = 1f / Mathf.Max(0.001f, flightMesh.bounds.extents.x);
        earthSurface = MeshPiece("Flight Earth", earth, flightMesh, -flightMesh.bounds.center * meshScale,
            Vector3.one * meshScale, earthMaterial).GetComponent<MeshRenderer>();
        SetAltitude(0f);
    }

    public void SetEarthView(Vector3 california, Vector3 north)
    {
        //frame the California side of the curved horizon with daylight on the visible surface
        Quaternion windowView = Quaternion.LookRotation(new Vector3(0.03f, 0.92f, -0.39f), Vector3.up);
        earth.localRotation = windowView * Quaternion.Inverse(Quaternion.LookRotation(california, north));
        var sunlight = root.TransformDirection(new Vector3(-0.45f, 0.72f, -0.53f).normalized);
        earthLighting.SetVector(sunDirectionId, sunlight);
        earthSurface.SetPropertyBlock(earthLighting);
    }

    public void SetAltitude(float altitudeMeters)
    {
        float altitude = Mathf.Clamp(altitudeMeters, 0f, 100000f);
        float lower = Mathf.Clamp01(altitude / 3000f);
        city.localScale = Vector3.one * Mathf.Lerp(1f, 0.16f, lower);
        city.localPosition = new Vector3(0f, -Mathf.Lerp(0f, 35f, lower), Mathf.Lerp(0f, 16f, lower));
        city.gameObject.SetActive(altitude < 24000f);
        float cloudY = Mathf.Lerp(18f, -23f, Mathf.Clamp01(altitude / 17000f));
        clouds.localPosition = new Vector3(0f, cloudY - 65f * AltitudeBlend(12000f, 28000f, altitude), 0f);
        clouds.gameObject.SetActive(altitude < 28000f);
        float passing = Mathf.Clamp01((altitude - 2500f) / 14000f);
        airplane.localPosition = new Vector3(Mathf.Lerp(-43f, 43f, passing), Mathf.Lerp(12f, -9f, passing), 35f);
        airplane.gameObject.SetActive(altitude > 2500f && altitude < 19000f);
        Color blueAir = Color.Lerp(new Color(0.29f, 0.61f, 0.84f), new Color(0.06f, 0.14f, 0.25f),
            AltitudeBlend(6000f, 24000f, altitude));
        SkyColor = Color.Lerp(blueAir, Color.black, AltitudeBlend(1000f, SpaceSkyStartMetres, altitude));
        //start with a wide horizon under the cabin, then let the same Earth recede
        float pullAway = AltitudeBlend(16000f, 100000f, altitude);
        float angularRadius = Mathf.Lerp(72f * Mathf.Deg2Rad, farEarthAngle, pullAway);
        float limb = Mathf.Lerp(-3.5f * Mathf.Deg2Rad, farEarthLimb, pullAway);
        float elevation = limb - angularRadius;
        Vector3 heading = Vector3.Slerp(Vector3.forward, earthBearing, pullAway);
        Vector3 direction = heading * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
        earth.localPosition = direction * (earthRadius / Mathf.Sin(angularRadius));
        //the air tints the surface but never hides it
        earthLighting.SetColor(hazeColorId, SkyColor);
        earthLighting.SetFloat(hazeId, 0.38f * (1f - AltitudeBlend(9000f, 50000f, altitude)));
        earthLighting.SetFloat(atmosphereId, AltitudeBlend(3500f, 18000f, altitude) * (1f - AltitudeBlend(28000f, 94000f, altitude)));
        earthSurface.SetPropertyBlock(earthLighting);
        altitudeText.text = altitude < 1000f ? altitude.ToString("0") + " m" : (altitude / 1000f).ToString("0.0") + " km";
        layerText.text = altitude >= 100000f ? "Edge of space / 100 km" : altitude >= 85000f ? "Thermosphere" :
            altitude >= 50000f ? "Mesosphere" : altitude >= 12000f ? "Stratosphere" : "Troposphere";
        altitudeMarker.anchoredPosition = new Vector2(345f, -90f + altitude / 100000f * 180f);
    }

    public void SetPaused(bool value)
    {
        controlsText.text = value ? "PAUSED   X: resume   A/Enter: skip   B: cancel" : "X: pause   A/Enter: skip   B: cancel";
    }

    static float AltitudeBlend(float start, float end, float altitude)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, altitude));
    }

    Mesh RefineFlightEarth(Mesh source)
    {
        //add surface detail for the close horizon while keeping the original texture mapping
        if (!source.isReadable || source.subMeshCount != 1) return source;
        Vector3[] original = source.vertices;
        Vector2[] originalUV = source.uv;
        int[] originalIndices = source.triangles;
        if (originalUV.Length != original.Length || originalIndices.Length == 0) return source;

        const int divisions = 3;
        const int verticesPerTriangle = (divisions + 1) * (divisions + 2) / 2;
        int vertexCount = originalIndices.Length / 3 * verticesPerTriangle;
        Vector3 center = source.bounds.center;
        float radius = source.bounds.extents.x;
        var vertices = new List<Vector3>(vertexCount);
        var normals = new List<Vector3>(vertexCount);
        var uv = new List<Vector2>(vertexCount);
        var indices = new List<int>(originalIndices.Length * divisions * divisions);
        for (int triangle = 0; triangle < originalIndices.Length; triangle += 3)
        {
            int a = originalIndices[triangle], b = originalIndices[triangle + 1], c = originalIndices[triangle + 2];
            int rowStart = vertices.Count;
            for (int row = 0; row <= divisions; row++)
            {
                int width = divisions - row + 1;
                for (int column = 0; column < width; column++)
                {
                    float wb = row / (float)divisions, wc = column / (float)divisions, wa = 1f - wb - wc;
                    Vector3 direction = (original[a] * wa + original[b] * wb + original[c] * wc - center).normalized;
                    vertices.Add(center + direction * radius);
                    normals.Add(direction);
                    uv.Add(originalUV[a] * wa + originalUV[b] * wb + originalUV[c] * wc);
                    int i = rowStart + column;
                    if (column + 1 >= width) continue;
                    indices.Add(i);
                    indices.Add(i + width);
                    indices.Add(i + 1);
                    if (column + 2 >= width) continue;
                    indices.Add(i + width);
                    indices.Add(i + width + 1);
                    indices.Add(i + 1);
                }
                rowStart += width;
            }
        }

        var mesh = new Mesh { name = "Flight Earth close horizon" };
        if (vertexCount > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(indices, 0);
        ownedMeshes.Add(mesh);
        return mesh;
    }

    public void Dispose()
    {
        if (root != null) Object.Destroy(root.gameObject);
        foreach (Material material in materials) Object.Destroy(material);
        foreach (Mesh mesh in ownedMeshes) Object.Destroy(mesh);
        materials.Clear();
        ownedMeshes.Clear();
    }

    void BuildCity(Transform parent)
    {
        Material ground = Tint("Launch ground", new Color(0.32f, 0.40f, 0.30f));
        Material pavement = Tint("Launch roads", new Color(0.19f, 0.24f, 0.26f));
        Material building = Tint("City buildings", new Color(0.58f, 0.56f, 0.49f));
        Material lightFace = Tint("Building roofs", new Color(0.71f, 0.69f, 0.61f));
        Material darkFace = Tint("Building sides", new Color(0.40f, 0.39f, 0.35f));
        Material[] buildingFaces = { lightFace, building, darkFace };
        Mesh buildingMesh = BuildingMesh();
        Material windows = Tint("City windows", new Color(0.28f, 0.40f, 0.48f));
        Material tower = Tint("Launch tower", new Color(0.67f, 0.72f, 0.73f));
        Box("Ground", parent, new Vector3(0f, -2.25f, 45f), new Vector3(130f, 0.40f, 145f), ground);
        Box("Launch road", parent, new Vector3(0f, -2.03f, 45f), new Vector3(5f, 0.025f, 130f), pavement);
        for (int i = 0; i < 20; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * (7f + (i % 4) * 5.5f);
            float z = 21f + i / 2 * 7f;
            float height = 3.5f + (i * 7 % 11);
            Transform block = MeshPiece("Building", parent, buildingMesh, new Vector3(x, height * 0.5f - 2f, z), new Vector3(4f, height, 4.5f), building);
            block.GetComponent<MeshRenderer>().sharedMaterials = buildingFaces;
            for (int row = 1; row <= 3; row++)
                Box("Building windows", parent, new Vector3(x, -2f + height * row / 4f, z - 2.27f), new Vector3(3.3f, 0.34f, 0.025f), windows);
        }
        Box("Launch tower left", parent, new Vector3(8f, 8f, 13f), new Vector3(0.45f, 20f, 0.45f), tower);
        Box("Launch tower right", parent, new Vector3(11f, 8f, 13f), new Vector3(0.45f, 20f, 0.45f), tower);
        for (int i = 0; i < 7; i++)
            Box("Launch tower crossbar", parent, new Vector3(9.5f, -1f + i * 3f, 13f), new Vector3(3.4f, 0.30f, 0.45f), tower);
    }

    Transform Group(string name, Transform parent)
    {
        var child = new GameObject(name);
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    Transform Panel(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    Material Tint(string name, Color color)
    {
        var material = new Material(surface) { name = name };
        //the Sun material keeps its shader but the cabin needs plain colors
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", Texture2D.whiteTexture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        materials.Add(material);
        return material;
    }

    Transform Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return Piece(name, parent, PrimitiveType.Cube, position, scale, material);
    }

    Transform Piece(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        return MeshPiece(name, parent, PrimitiveMesh(type), position, scale, material);
    }

    Mesh PrimitiveMesh(PrimitiveType type)
    {
        if (!meshes.TryGetValue(type, out Mesh mesh))
        {
            var sample = GameObject.CreatePrimitive(type);
            sample.SetActive(false);
            mesh = sample.GetComponent<MeshFilter>().sharedMesh;
            meshes.Add(type, mesh);
            Object.Destroy(sample);
        }
        return mesh;
    }

    Transform MeshPiece(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material)
    {
        Transform item = Group(name, parent);
        item.localPosition = position;
        item.localScale = scale;
        item.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = item.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        return item;
    }

    Mesh BuildingMesh()
    {
        //simple face shades help the building height read without extra lights
        Mesh mesh = Object.Instantiate(PrimitiveMesh(PrimitiveType.Cube));
        mesh.name = "Rocket city shaded cube";
        int[] indices = mesh.triangles;
        Vector3[] normals = mesh.normals;
        var faces = new[] { new List<int>(), new List<int>(), new List<int>() };
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 normal = normals[indices[i]];
            int face = normal.y > 0.5f ? 0 : Mathf.Abs(normal.x) > 0.5f || normal.y < -0.5f ? 2 : 1;
            faces[face].Add(indices[i]);
            faces[face].Add(indices[i + 1]);
            faces[face].Add(indices[i + 2]);
        }
        mesh.subMeshCount = faces.Length;
        for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
        ownedMeshes.Add(mesh);
        return mesh;
    }

    TMP_Text Text(Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.layer = layer;
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    void Text(Transform parent, string value, Vector3 position, Vector2 size, float fontSize, float scale)
    {
        Transform label = Panel("Ascent information", parent);
        var canvas = label.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        label.localPosition = position;
        label.localScale = Vector3.one * scale;
        label.GetComponent<RectTransform>().sizeDelta = size;
        Text(label, value, Vector2.zero, size, fontSize, new Color(0.64f, 0.77f, 0.84f));
    }

    RectTransform Image(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        obj.layer = layer;
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
