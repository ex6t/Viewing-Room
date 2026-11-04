using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

//the cabin stays still while the scenery follows our teaching altitude
public sealed class ObservatoryRocketCabin
{
    public Color SkyColor { get; private set; }

    readonly Transform root, city, clouds, airplane, earth;
    readonly Transform atmosphere;
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly Dictionary<PrimitiveType, Mesh> meshes = new Dictionary<PrimitiveType, Mesh>();
    readonly Material surface;
    readonly TMP_FontAsset font;
    readonly int layer;
    readonly TMP_Text altitudeText, layerText, controlsText;
    readonly RectTransform altitudeMarker;

    public ObservatoryRocketCabin(Transform parent, int layer, Material surfaceMaterial, TMP_FontAsset font)
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
        controlsText = Text(panel, "X: pause   B: cancel", new Vector2(-115f, -73f), new Vector2(790f, 34f), 21f, new Color(0.72f, 0.83f, 0.88f));
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
        Material ocean = Tint("Earth ocean", new Color(0.04f, 0.27f, 0.43f));
        Material rim = Tint("Atmosphere rim", new Color(0.31f, 0.72f, 0.92f));
        atmosphere = Piece("Atmosphere", earth, PrimitiveType.Sphere, new Vector3(0f, -112f, 105f), Vector3.one * 219f, rim);
        Piece("Curved Earth", earth, PrimitiveType.Sphere, new Vector3(0f, -112f, 90f), Vector3.one * 210f, ocean);
        Material land = Tint("Earth land", new Color(0.22f, 0.39f, 0.28f));
        LandPatch("Distant coastline", new Vector2(-22f, 75f), new Vector2(17f, 12f), land);
        LandPatch("Distant land", new Vector2(22f, 80f), new Vector2(14f, 10f), land);
        SetAltitude(0f);
    }

    public void SetAltitude(float altitudeMeters)
    {
        float altitude = Mathf.Clamp(altitudeMeters, 0f, 100000f);
        float lower = Mathf.Clamp01(altitude / 3000f);
        city.localScale = Vector3.one * Mathf.Lerp(1f, 0.16f, lower);
        city.localPosition = new Vector3(0f, -Mathf.Lerp(0f, 35f, lower), Mathf.Lerp(0f, 16f, lower));
        city.gameObject.SetActive(altitude < 24000f);
        clouds.localPosition = new Vector3(0f, Mathf.Lerp(18f, -23f, Mathf.Clamp01(altitude / 17000f)), 0f);
        clouds.gameObject.SetActive(altitude < 28000f);
        float passing = Mathf.Clamp01((altitude - 2500f) / 14000f);
        airplane.localPosition = new Vector3(Mathf.Lerp(-23f, 24f, passing), Mathf.Lerp(12f, -9f, passing), 35f);
        airplane.gameObject.SetActive(altitude > 2500f && altitude < 19000f);
        float upper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12000f, 47000f, altitude));
        earth.localPosition = new Vector3(0f, Mathf.Lerp(-90f, 0f, upper), 0f);
        atmosphere.localScale = Vector3.one * Mathf.Lerp(219f, 212f, Mathf.Clamp01(altitude / 100000f));
        SkyColor = Color.Lerp(new Color(0.29f, 0.61f, 0.84f), new Color(0.003f, 0.008f, 0.025f), Mathf.SmoothStep(0f, 1f, altitude / 75000f));
        altitudeText.text = altitude < 1000f ? altitude.ToString("0") + " m" : (altitude / 1000f).ToString("0.0") + " km";
        layerText.text = altitude >= 100000f ? "Edge of space / 100 km" : altitude >= 85000f ? "Thermosphere" :
            altitude >= 50000f ? "Mesosphere" : altitude >= 12000f ? "Stratosphere" : "Troposphere";
        altitudeMarker.anchoredPosition = new Vector2(345f, -90f + altitude / 100000f * 180f);
    }

    public void SetPaused(bool value)
    {
        controlsText.text = value ? "PAUSED   X: resume   B: cancel" : "X: pause   B: cancel";
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

    void LandPatch(string name, Vector2 center, Vector2 size, Material material)
    {
        //these small patches follow the ocean sphere instead of floating above it
        const int segments = 12, rings = 4;
        var vertices = new Vector3[1 + segments * rings];
        var triangles = new List<int>();
        vertices[0] = LandPoint(center.x, center.y);
        for (int ring = 1; ring <= rings; ring++)
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                float coastline = 0.85f + 0.15f * Mathf.Sin(angle * 3f + center.x);
                float fraction = ring / (float)rings * coastline;
                int current = 1 + (ring - 1) * segments + i;
                int next = 1 + (ring - 1) * segments + (i + 1) % segments;
                vertices[current] = LandPoint(center.x + Mathf.Cos(angle) * size.x * fraction,
                    center.y + Mathf.Sin(angle) * size.y * fraction);
                if (ring == 1) triangles.AddRange(new[] { 0, next, current });
                else
                {
                    int inner = current - segments, innerNext = next - segments;
                    triangles.AddRange(new[] { inner, next, current, inner, innerNext, next });
                }
            }
        var mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ownedMeshes.Add(mesh);
        MeshPiece(name, earth, mesh, Vector3.zero, Vector3.one, material);
    }

    static Vector3 LandPoint(float x, float z)
    {
        const float radius = 105.35f;
        return new Vector3(x, -112f + Mathf.Sqrt(radius * radius - x * x - (z - 90f) * (z - 90f)), z);
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
