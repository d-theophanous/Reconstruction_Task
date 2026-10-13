using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// One row of the settings menu (one scene). Lives on the row prefab; SettingsUI
// instantiates one per scene and calls Bind.
public class SettingsRowUI : MonoBehaviour
{
    public TMP_Text sceneNameLabel;
    public Toggle includeToggle;

    [FormerlySerializedAs("zoomSlider")]
    public Slider originalZoomSlider;
    [FormerlySerializedAs("zoomValueLabel")]
    [Tooltip("Optional - shows the current field of view next to the slider.")]
    public TMP_Text originalZoomValueLabel;

    public Slider reconstructionZoomSlider;
    [Tooltip("Optional - shows the current field of view next to the slider.")]
    public TMP_Text reconstructionZoomValueLabel;

    public Toggle fixedBlackScreenToggle;

    public void Bind(string label, SceneSettings settings, float minFieldOfView, float maxFieldOfView)
    {
        sceneNameLabel.text = label;

        includeToggle.SetIsOnWithoutNotify(settings.include);
        includeToggle.onValueChanged.AddListener(value => settings.include = value);

        BindZoom(originalZoomSlider, originalZoomValueLabel, settings.originalFieldOfView,
            value => settings.originalFieldOfView = value, minFieldOfView, maxFieldOfView);
        BindZoom(reconstructionZoomSlider, reconstructionZoomValueLabel, settings.reconstructionFieldOfView,
            value => settings.reconstructionFieldOfView = value, minFieldOfView, maxFieldOfView);

        fixedBlackScreenToggle.SetIsOnWithoutNotify(settings.blackScreenFixedTime);
        fixedBlackScreenToggle.onValueChanged.AddListener(value => settings.blackScreenFixedTime = value);
    }

    // Slider runs from the camera's min to max field of view, flipped so the right end is
    // "most zoomed in" (smallest field of view).
    static void BindZoom(Slider slider, TMP_Text valueLabel, float current, Action<float> onChanged,
        float minFieldOfView, float maxFieldOfView)
    {
        slider.direction = Slider.Direction.RightToLeft;
        slider.wholeNumbers = false;
        slider.minValue = minFieldOfView;
        slider.maxValue = maxFieldOfView;
        slider.SetValueWithoutNotify(Mathf.Clamp(current, minFieldOfView, maxFieldOfView));
        UpdateLabel(valueLabel, slider.value);

        slider.onValueChanged.AddListener(value =>
        {
            onChanged(value);
            UpdateLabel(valueLabel, value);
        });
    }

    static void UpdateLabel(TMP_Text label, float fieldOfView)
    {
        if (label != null) label.text = $"{fieldOfView:0}°";
    }
}
