using System.Collections.Generic;
using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.P;

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

    private bool _lightsOn = true;

    private void Start()
    {
        ApplyColor();
        SetLightsState(_lightsOn);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
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