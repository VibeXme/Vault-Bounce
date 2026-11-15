using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Builds all five Vault Bounce levels, the sprites, the physics material,
// the tags and the Build Settings entries.
// Run it from the menu: Tools > Vault Bounce > Build All Levels
public static class VaultBounceBuilder
{
    static Sprite squareSprite, circleSprite, coneSprite;
    static PhysicsMaterial2D zeroFriction;

    static readonly Color WallColor   = new Color32(29, 36, 70, 255);
    static readonly Color SpikeColor  = new Color32(255, 77, 109, 255);
    static readonly Color LaserColor  = new Color32(255, 42, 77, 255);
    static readonly Color LockedColor = new Color32(214, 69, 69, 255);
    static readonly Color KeyColor    = new Color32(255, 210, 74, 255);
    static readonly Color BumperColor = new Color32(255, 209, 102, 255);
    static readonly Color DroneColor  = new Color32(255, 138, 91, 255);
    static readonly Color PlayerColor = new Color32(255, 111, 192, 255);

    // ---- Level layouts, copied from the browser version ----
    // Coordinates are pixels on a 960x540 screen (y goes down).
    // Rect      = {x, y, width, height}
    // Key       = {x, y}
    // Spotlight = {x, y, baseAngle, sweep, speed, range, halfAngle}
    // Laser     = {x, y, width, height, onTime, offTime, warnTime, phase}
    // Bumper    = {x, y, radius}
    const float PI2 = Mathf.PI / 2f;

    static float[] V(params float[] v) => v;

    class DroneDef
    {
        public float[][] pts;
        public float pat, chase, aggro;
    }

    class LevelDef
    {
        public float[][] solids = new float[0][];
        public float[][] spikes = new float[0][];
        public float[][] keys = new float[0][];
        public float[][] spots = new float[0][];
        public float[][] lasers = new float[0][];
        public float[][] bumpers = new float[0][];
        public DroneDef[] drones = new DroneDef[0];
    }

    static readonly float[][] BaseSolids =
    {
        V(0, 515, 960, 25), // floor
        V(0, 0, 20, 540),   // left wall
        V(940, 0, 20, 540), // right wall
        V(0, 0, 960, 20)    // ceiling
    };

    static readonly float[] ExitRect = V(880, 430, 50, 85);

    static readonly float[][] Platforms = // levels 2-5
    {
        V(100, 410, 180, 16), V(330, 330, 160, 16), V(560, 250, 160, 16), V(740, 380, 140, 16)
    };

    static readonly float[][] Spikes = // levels 2, 3, 5
    {
        V(240, 498, 110, 17), V(600, 498, 100, 17)
    };

    static readonly float[][] Keys = // levels 2-5
    {
        V(190, 370), V(410, 290), V(640, 210), V(810, 340), V(480, 440)
    };

    static readonly float[][] Lasers = // levels 3 and 5
    {
        V(520, 300, 8, 215, 1.6f, 1.6f, 0.45f, 0f),
        V(690, 270, 8, 245, 1.6f, 1.6f, 0.45f, 0.8f)
    };

    static readonly float[][] TwoSpots =
    {
        V(250, 30, PI2, 0.9f, 1.3f, 460, 0.35f),
        V(700, 30, PI2, -0.9f, 1.7f, 460, 0.35f)
    };

    static readonly LevelDef[] Levels =
    {
        new LevelDef // Level 1: bounce, platforms, spikes
        {
            solids = new[] { V(120, 400, 200, 16), V(380, 300, 180, 16), V(620, 200, 180, 16) },
            spikes = new[] { V(240, 498, 110, 17), V(600, 498, 100, 17) },
            keys   = new[] { V(200, 360), V(470, 260), V(710, 160), V(480, 440), V(860, 440) }
        },
        new LevelDef // Level 2: security spotlights
        {
            solids = Platforms,
            spikes = Spikes,
            keys   = Keys,
            spots  = TwoSpots
        },
        new LevelDef // Level 3: telegraphed lasers
        {
            solids = Platforms,
            spikes = Spikes,
            keys   = Keys,
            spots  = new[] { V(250, 30, PI2, 0.9f, 1.3f, 460, 0.35f) },
            lasers = Lasers
        },
        new LevelDef // Level 4: shock bumpers next to spike pits
        {
            solids  = Platforms,
            spikes  = new[] { V(210, 498, 130, 17), V(560, 498, 120, 17) },
            keys    = Keys,
            spots   = new[] { V(480, 30, PI2, 1.0f, 1.5f, 460, 0.32f) },
            lasers  = new[] { V(520, 300, 8, 215, 1.6f, 1.6f, 0.45f, 0.4f) },
            bumpers = new[] { V(170, 482, 20), V(660, 482, 20), V(430, 380, 18) }
        },
        new LevelDef // Level 5: patrol drones
        {
            solids = Platforms,
            spikes = Spikes,
            keys   = Keys,
            spots  = TwoSpots,
            lasers = Lasers,
            drones = new[]
            {
                new DroneDef { pts = new[] { V(120, 250), V(420, 250) }, pat = 110, chase = 185, aggro = 150 },
                new DroneDef { pts = new[] { V(600, 440), V(860, 440) }, pat = 120, chase = 190, aggro = 150 }
            }
        }
    };

    [MenuItem("Tools/Vault Bounce/Build All Levels")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art"));
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        AssetDatabase.Refresh();

        foreach (var tag in new[] { "Ground", "Wall", "Hazard", "Key", "Exit" })
            AddTag(tag);

        Physics2D.gravity = new Vector2(0f, -22f);

        zeroFriction = GetZeroFriction();
        squareSprite = MakeSprite("Square", 16, (x, y) => Color.white, new Vector2(0.5f, 0.5f));
        circleSprite = MakeSprite("Circle", 64, (x, y) =>
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            return Mathf.Sqrt(dx * dx + dy * dy) <= 31f ? Color.white : Color.clear;
        }, new Vector2(0.5f, 0.5f));
        coneSprite = MakeSprite("Cone", 64, (x, y) =>
        {
            float depth = (63 - y) / 63f;       // 0 at the apex (top row), 1 at the bottom
            float halfWidth = depth * 31.5f;
            return Mathf.Abs(x - 31.5f) <= halfWidth ? Color.white : Color.clear;
        }, new Vector2(0.5f, 1f));

        var scenes = new EditorBuildSettingsScene[Levels.Length];
        for (int i = 0; i < Levels.Length; i++)
        {
            BuildLevel(i);
            scenes[i] = new EditorBuildSettingsScene($"Assets/Scenes/Scene_Level{i + 1}.unity", true);
        }
        EditorBuildSettings.scenes = scenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Vault Bounce: built 5 levels and added them to Build Settings.");
    }

    static void BuildLevel(int index)
    {
        var def = Levels[index];
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 2.7f; // 5.4 units tall, 1 unit = 100 px
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.12f);

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        BuildUI(gm);

        foreach (var r in BaseSolids) MakeRect("Boundary", r, WallColor, false, "Ground");
        foreach (var r in def.solids) MakeRect("Platform", r, WallColor, false, "Ground");
        foreach (var r in def.spikes) MakeRect("Spikes", r, SpikeColor, false, "Hazard");

        var exit = MakeRect("ExitDoor", ExitRect, LockedColor, true, "Exit");
        var door = exit.AddComponent<ExitDoor>();
        door.doorRenderer = exit.GetComponent<SpriteRenderer>();
        gm.exitDoor = door;

        foreach (var k in def.keys) MakeKey(k);
        foreach (var s in def.spots) MakeSpotlight(s);
        foreach (var l in def.lasers) MakeLaser(l);
        foreach (var b in def.bumpers) MakeBumper(b);
        foreach (var d in def.drones) MakeDrone(d);
        MakePlayer(60, 460);

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/Scene_Level{index + 1}.unity");
    }

    // Converts pixel coordinates (top-left origin) to world units centered on the camera.
    static Vector3 ToWorld(float x, float y) => new Vector3(x / 100f - 4.8f, 2.7f - y / 100f, 0f);

    static GameObject MakeRect(string name, float[] r, Color color, bool trigger, string tag)
    {
        var go = new GameObject(name);
        go.tag = tag;
        go.transform.position = ToWorld(r[0] + r[2] / 2f, r[1] + r[3] / 2f);
        go.transform.localScale = new Vector3(r[2] / 100f, r[3] / 100f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one; // the transform scale sets the real size
        box.isTrigger = trigger;
        box.sharedMaterial = zeroFriction;
        return go;
    }

    static Transform AddCircleVisual(Transform parent, float diameter, Color color)
    {
        var v = new GameObject("Visual");
        v.transform.SetParent(parent, false);
        v.transform.localScale = new Vector3(diameter, diameter, 1f);
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = color;
        return v.transform;
    }

    static void MakeKey(float[] k)
    {
        var go = new GameObject("GoldenKey");
        go.tag = "Key";
        go.transform.position = ToWorld(k[0], k[1]);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.09f;
        col.isTrigger = true;
        go.AddComponent<GoldenKey>();
        AddCircleVisual(go.transform, 0.18f, KeyColor);
    }

    static void MakeSpotlight(float[] s)
    {
        float x = s[0], y = s[1], sweep = s[3], speed = s[4], range = s[5], half = s[6];
        float len = range / 100f, t = Mathf.Tan(half) * len;

        // Pivot sits at the apex; the cone points down (-Y) when the angle is 0.
        var pivot = new GameObject("SecuritySpotlight");
        pivot.transform.position = ToWorld(x, y);
        var poly = pivot.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        poly.points = new[] { Vector2.zero, new Vector2(-t, -len), new Vector2(t, -len) };

        var spot = pivot.AddComponent<SecuritySpotlight>();
        spot.sweepAngle = sweep * Mathf.Rad2Deg;
        spot.sweepSpeed = speed;
        spot.detectionFillRate = 1.35f;

        var cone = new GameObject("ConeVisual");
        cone.transform.SetParent(pivot.transform, false);
        cone.transform.localScale = new Vector3(2f * t, len, 1f);
        var sr = cone.AddComponent<SpriteRenderer>();
        sr.sprite = coneSprite;
        sr.color = new Color(1f, 0.85f, 0.35f, 0.18f);
    }

    static void MakeLaser(float[] l)
    {
        var go = MakeRect("Laser", V(l[0], l[1], l[2], l[3]), LaserColor, true, "Hazard");
        var pulse = go.AddComponent<PulsingLaser>();
        pulse.activeDuration = l[4];
        pulse.inactiveDuration = l[5];
        pulse.warningDuration = l[6];
    }

    static void MakeBumper(float[] b)
    {
        var go = new GameObject("ShockBumper");
        go.transform.position = ToWorld(b[0], b[1]);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = b[2] / 100f;
        go.AddComponent<ShockBumper>();
        AddCircleVisual(go.transform, 2f * b[2] / 100f, BumperColor);
    }

    static void MakeDrone(DroneDef d)
    {
        var go = new GameObject("PatrolDrone");
        go.transform.position = ToWorld(d.pts[0][0], d.pts[0][1]);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.16f;
        col.isTrigger = true;

        var drone = go.AddComponent<PatrolDrone>();
        drone.patrolSpeed = d.pat / 100f;
        drone.chaseSpeed = d.chase / 100f;
        drone.aggroRadius = d.aggro / 100f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = DroneColor;

        var waypoints = new Transform[d.pts.Length];
        for (int i = 0; i < d.pts.Length; i++)
        {
            var wp = new GameObject($"Waypoint{i}");
            wp.transform.position = ToWorld(d.pts[i][0], d.pts[i][1]);
            waypoints[i] = wp.transform;
        }
        drone.waypoints = waypoints;
    }

    static void MakePlayer(float x, float y)
    {
        var go = new GameObject("Player");
        go.transform.position = ToWorld(x, y);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.14f;
        col.sharedMaterial = zeroFriction;
        var bp = go.AddComponent<BouncyPlayer>();
        bp.visualSprite = AddCircleVisual(go.transform, 0.28f, PlayerColor);
    }

    static void BuildUI(GameManager gm)
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gm.keysText = MakeText(canvasGO.transform, "KeysText", "KEYS: 0 / 5",
            TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(40, -30));
        gm.levelText = MakeText(canvasGO.transform, "LevelText", "LEVEL 1",
            TextAlignmentOptions.Top, new Vector2(0.5f, 1), new Vector2(0, -30));

        // Detection meter (top-right)
        var sliderGO = new GameObject("DetectionSlider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(canvasGO.transform, false);
        var rt = (RectTransform)sliderGO.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-40, -70);
        rt.sizeDelta = new Vector2(300, 24);
        var bg = sliderGO.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.15f);

        var slider = sliderGO.GetComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.targetGraphic = bg;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(sliderGO.transform, false);
        var fr = (RectTransform)fill.transform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(0, 1);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(1f, 0.4f, 0.3f);
        slider.fillRect = fr;

        gm.detectionSlider = slider;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        TextAlignmentOptions align, Vector2 anchor, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 60);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static Sprite MakeSprite(string name, int size, System.Func<int, int, Color> pixel, Vector2 pivot)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();

        string assetPath = $"Assets/Art/{name}.png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, "Art", name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size; // the sprite is exactly 1 world unit across
        importer.spritePivot = pivot;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static PhysicsMaterial2D GetZeroFriction()
    {
        const string path = "Assets/Art/ZeroFriction.asset";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("ZeroFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}
