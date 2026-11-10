using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class ForgeDayCycle : MonoBehaviour
{
    [SerializeField, Min(1)] float secondsPerDay = 600;
    [SerializeField, Range(0, 23.99f)] float startHour = 8;
    public ForgeClock Clock { get; private set; }
    public bool Paused { get; set; }
    public int Day => Clock?.Day ?? 1;
    public double CurrentHour => Clock?.Hour ?? startHour;
    public float SecondsPerDay { get => secondsPerDay; set => secondsPerDay = Mathf.Max(1, value); }
    public float AnnouncementAlpha => Fade(announcementAge);
    readonly List<Light> replacedLights = new List<Light>();
    Light sun, moon;
    Transform sunDisc, moonDisc;
    Material sunMaterial, moonMaterial;
    Camera sceneCamera;
    Color previousBackground, previousAmbient;
    AmbientMode previousAmbientMode;
    Light previousSun;
    float announcementAge;
    GUIStyle title;
    bool initialized;

    void Awake() { Clock = new ForgeClock(startHour); Clock.DayChanged += AnnounceDay; }
    void Start()
    {
        previousSun = RenderSettings.sun;
        previousAmbient = RenderSettings.ambientLight;
        previousAmbientMode = RenderSettings.ambientMode;
        foreach (var light in FindObjectsByType<Light>())
            if (light.type == LightType.Directional && light.enabled) { replacedLights.Add(light); light.enabled = false; }
        sun = CreateLight("Sun", new Color(1f, .84f, .63f));
        moon = CreateLight("Moon", new Color(.58f, .7f, 1f));
        sunDisc = CreateDisc("Sun disc", new Color(1f, .8f, .4f), .65f, out sunMaterial);
        moonDisc = CreateDisc("Moon disc", new Color(.72f, .83f, 1f), .45f, out moonMaterial);
        sceneCamera = Camera.main;
        if (sceneCamera) previousBackground = sceneCamera.backgroundColor;
        RenderSettings.ambientMode = AmbientMode.Flat;
        initialized = true;
        ApplyLighting();
    }
    Light CreateLight(string label, Color color)
    {
        var obj = new GameObject(label); obj.transform.SetParent(transform, false);
        var light = obj.AddComponent<Light>(); light.type = LightType.Directional;
        light.color = color; light.shadows = LightShadows.Soft; return light;
    }
    Transform CreateDisc(string label, Color color, float size, out Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name = label;
        obj.transform.SetParent(transform, false); obj.transform.localScale = Vector3.one * size;
        Destroy(obj.GetComponent<Collider>());
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        material = shader ? new Material(shader) : null;
        var renderer = obj.GetComponent<Renderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        if (material) { material.color = color; renderer.sharedMaterial = material; }
        return obj.transform;
    }
    void Update()
    {
        if(Paused)return;
        Clock.Advance(Time.deltaTime, Mathf.Max(1, secondsPerDay));
        announcementAge += Time.unscaledDeltaTime;
        if (initialized) ApplyLighting();
    }
    void AnnounceDay(int day) { announcementAge = 0; }
    public static float Fade(float age)
    {
        if (age < 0 || age >= 4) return 0;
        if (age < .8f) return Mathf.SmoothStep(0, 1, age / .8f);
        if (age < 2.6f) return 1;
        return 1 - Mathf.SmoothStep(0, 1, (age - 2.6f) / 1.4f);
    }
    void ApplyLighting()
    {
        float angle = ((float)CurrentHour - 6) / 24 * Mathf.PI * 2;
        var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), .25f).normalized;
        sun.transform.rotation = Quaternion.LookRotation(-direction);
        moon.transform.rotation = Quaternion.LookRotation(direction);
        float daylight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.12f, .3f, direction.y));
        sun.intensity = 1.8f * Mathf.Clamp01(direction.y * 4);
        moon.intensity = .45f * Mathf.Clamp01(-direction.y * 4);
        RenderSettings.sun = daylight > .5f ? sun : moon;
        RenderSettings.ambientLight = Color.Lerp(new Color(.13f, .17f, .27f), new Color(.48f, .49f, .52f), daylight);
        if (sceneCamera) sceneCamera.backgroundColor = Color.Lerp(new Color(.025f, .04f, .09f), new Color(.32f, .46f, .62f), daylight);
        var center = new Vector3(2, 1, 5);
        sunDisc.position = center + direction * 7;
        moonDisc.position = center - direction * 7;
        sunDisc.gameObject.SetActive(direction.y > -.08f);
        moonDisc.gameObject.SetActive(direction.y < .08f);
    }
    void OnGUI()
    {
        if(Paused)return;
        float alpha = AnnouncementAlpha;
        if (alpha <= 0) return;
        if (title == null) title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 56, fontStyle = FontStyle.Bold };
        var oldColor = GUI.color; int oldDepth = GUI.depth; GUI.depth = -1000;
        float height = Mathf.Max(80, title.CalcHeight(new GUIContent("Day " + Day), Screen.width));
        var rect = new Rect(0, 12, Screen.width, height);
        GUI.color = new Color(0, 0, 0, alpha * .8f); GUI.Label(new Rect(2, 14, Screen.width, height), "Day " + Day, title);
        GUI.color = new Color(1, 1, 1, alpha); GUI.Label(rect, "Day " + Day, title);
        GUI.color = oldColor; GUI.depth = oldDepth;
    }
    void OnDestroy()
    {
        if (Clock != null) Clock.DayChanged -= AnnounceDay;
        if (initialized) {
            foreach (var light in replacedLights) if (light) light.enabled = true;
            RenderSettings.sun = previousSun;
            RenderSettings.ambientMode = previousAmbientMode; RenderSettings.ambientLight = previousAmbient;
            if (sceneCamera) sceneCamera.backgroundColor = previousBackground;
        }
        if (sun) Destroy(sun.gameObject); if (moon) Destroy(moon.gameObject);
        if (sunDisc) Destroy(sunDisc.gameObject); if (moonDisc) Destroy(moonDisc.gameObject);
        if (sunMaterial) Destroy(sunMaterial); if (moonMaterial) Destroy(moonMaterial);
    }
}
