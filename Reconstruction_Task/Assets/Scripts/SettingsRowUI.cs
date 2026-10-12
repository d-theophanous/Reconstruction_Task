using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row of the settings menu (one scene). Lives on the row prefab; SettingsUI
// instantiates one per scene and calls Bind.
public class SettingsRowUI : MonoBehaviour
{
    public TMP_Text sceneNameLabel;
    public Toggle includeToggle;
    public Slider zoomSlider;
    [Tooltip("Optional - shows the current field of view next to the slider.")]
    public TMP_Text zoomValueLabel;
    public Toggle fixedBlackScreenToggle;

    public void Bind(string label, SceneSettings settings, float minFieldOfView, float maxFieldOfView)
    {
        sceneNameLabel.text = label;

        includeToggle.SetIsOnWithoutNotify(settings.include);
        includeToggle.onValueChanged.AddListener(value => settings.include = value);

        // Slider runs from the camera's min to max field of view, flipped so the right
        // end is "most zoomed in" (smallest field of view).
        zoomSlider.direction = Slider.Direction.RightToLeft;
        zoomSlider.wholeNumbers = false;
        zoomSlider.minValue = minFieldOfView;
        zoomSlider.maxValue = maxFieldOfView;
        zoomSlider.SetValueWithoutNotify(Mathf.Clamp(settings.fieldOfView, minFieldOfView, maxFieldOfView));
        UpdateZoomLabel(zoomSlider.value);
        zoomSlider.onValueChanged.AddListener(value =>
        {
            settings.fieldOfView = value;
            UpdateZoomLabel(value);
        });

        fixedBlackScreenToggle.SetIsOnWithoutNotify(settings.blackScreenFixedTime);
        fixedBlackScreenToggle.onValueChanged.AddListener(value => settings.blackScreenFixedTime = value);
    }

    void UpdateZoomLabel(float fieldOfView)
    {
        if (zoomValueLabel != null) zoomValueLabel.text = $"{fieldOfView:0}°";
    }
}
