using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InstanceAutoLightController : MonoBehaviour
{
    [Header("Toggle Key (New Input System)")]
    public Key toggleKey = Key.P;

    [Header("Lights")]
    public List<Light> lights = new List<Light>();

    [Header("Materials")]
    public List<Material> materials = new List<Material>();

    [Header("Color Settings")]
    public Color lightColor = Color.white;
    [Tooltip("The emission color property name used by your materials (default: _EmissionColor)")]
    public string emissionColorProperty = "_EmissionColor";
    [Tooltip("Multiplier applied to the emission color intensity")]
    public float emissionIntensity = 1f;

    [Header("Instance Color Randomization")]
    [Tooltip("Automatically assign a new random color to each instantiated prefab")]
    public bool randomizeColorOnSpawn = true;
    [Tooltip("Use HSV range sliders for tighter control over the randomized color")]
    public bool useHSVRange = false;
    [MinMaxRange(0f, 1f)]
    public Vector2 hueRange = new Vector2(0f, 1f);
    [MinMaxRange(0f, 1f)]
    public Vector2 saturationRange = new Vector2(0.7f, 1f);
    [MinMaxRange(0f, 1f)]
    public Vector2 valueRange = new Vector2(0.8f, 1f);

    private bool _lightsOn = true;

    private void Awake()
    {
        // Each clone gets its own material instances so color changes never
        // bleed across to other prefab instances (or the shared asset).
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
                materials[i] = new Material(materials[i]);
        }

        // Awake runs on every new instance immediately after instantiation,
        // before any other script can read the color — so each clone gets its
        // own unique color baked in before Start() fires.
        if (randomizeColorOnSpawn)
        {
            lightColor = useHSVRange
                ? Random.ColorHSV(
                    hueRange.x,        hueRange.y,
                    saturationRange.x, saturationRange.y,
                    valueRange.x,      valueRange.y)
                : Random.ColorHSV();
        }
    }

    private void Start()
    {
        ApplyColor();
        SetLightsState(_lightsOn);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            _lightsOn = !_lightsOn;
            SetLightsState(_lightsOn);
        }
    }

    /// <summary>
    /// Turns all registered lights and material emissions on or off.
    /// </summary>
    private void SetLightsState(bool on)
    {
        foreach (Light light in lights)
        {
            if (light != null)
                light.enabled = on;
        }

        foreach (Material mat in materials)
        {
            if (mat == null) continue;

            if (on)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(emissionColorProperty, lightColor * emissionIntensity);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor(emissionColorProperty, Color.black);
            }
        }
    }

    /// <summary>
    /// Applies the selected color to all lights and materials immediately.
    /// Call this at runtime whenever lightColor changes.
    /// </summary>
    public void ApplyColor()
    {
        foreach (Light light in lights)
        {
            if (light != null)
                light.color = lightColor;
        }

        foreach (Material mat in materials)
        {
            if (mat == null) continue;

            mat.SetColor("_Color", lightColor);

            if (_lightsOn)
                mat.SetColor(emissionColorProperty, lightColor * emissionIntensity);
        }
    }

    // ---------------------------------------------------------------
    // Public helpers — wire these up to UI buttons or other scripts
    // ---------------------------------------------------------------

    /// <summary>Programmatically turn lights on/off.</summary>
    public void SetLights(bool on)
    {
        _lightsOn = on;
        SetLightsState(_lightsOn);
    }

    /// <summary>Change the controller color and push it to all lights and materials instantly.</summary>
    public void SetColor(Color newColor)
    {
        lightColor = newColor;
        ApplyColor();
    }

    /// <summary>Add a light at runtime and sync its color/state.</summary>
    public void RegisterLight(Light light)
    {
        if (light != null && !lights.Contains(light))
        {
            lights.Add(light);
            light.color = lightColor;
            light.enabled = _lightsOn;
        }
    }

    /// <summary>Add a material at runtime and sync its color/state.</summary>
    public void RegisterMaterial(Material mat)
    {
        if (mat != null && !materials.Contains(mat))
        {
            materials.Add(mat);
            mat.SetColor("_Color", lightColor);

            if (_lightsOn)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(emissionColorProperty, lightColor * emissionIntensity);
            }
        }
    }

#if UNITY_EDITOR
    // Live-preview color changes in the Editor without entering Play Mode
    private void OnValidate()
    {
        ApplyColor();
    }
#endif
}

// ---------------------------------------------------------------------------
// Lightweight MinMaxRange attribute — renders as a dual-handle slider in the
// Inspector via the companion drawer below. No extra package required.
// ---------------------------------------------------------------------------
public class MinMaxRangeAttribute : PropertyAttribute
{
    public float Min { get; }
    public float Max { get; }
    public MinMaxRangeAttribute(float min, float max) { Min = min; Max = max; }
}

#if UNITY_EDITOR
namespace LightControllerEditor
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            if (prop.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.LabelField(pos, label.text, "Use MinMaxRange with Vector2");
                return;
            }

            var range = (MinMaxRangeAttribute)attribute;
            Vector2 v = prop.vector2Value;
            float minVal = v.x, maxVal = v.y;

            float labelW = EditorGUIUtility.labelWidth;
            float fieldW = 40f;
            float padding = 4f;

            Rect labelRect  = new Rect(pos.x, pos.y, labelW, pos.height);
            Rect minRect    = new Rect(pos.x + labelW, pos.y, fieldW, pos.height);
            Rect sliderRect = new Rect(pos.x + labelW + fieldW + padding, pos.y,
                                       pos.width - labelW - fieldW * 2 - padding * 2, pos.height);
            Rect maxRect    = new Rect(pos.xMax - fieldW, pos.y, fieldW, pos.height);

            EditorGUI.LabelField(labelRect, label);
            minVal = EditorGUI.FloatField(minRect, minVal);
            EditorGUI.MinMaxSlider(sliderRect, ref minVal, ref maxVal, range.Min, range.Max);
            maxVal = EditorGUI.FloatField(maxRect, maxVal);

            minVal = Mathf.Clamp(minVal, range.Min, maxVal);
            maxVal = Mathf.Clamp(maxVal, minVal, range.Max);
            prop.vector2Value = new Vector2(minVal, maxVal);
        }
    }
}
#endif