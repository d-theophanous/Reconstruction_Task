using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// One row of the settings menu (one scene). Lives on the row prefab; SettingsUI
// instantiates one per scene, in the order the researcher set, and calls Bind.
public class SettingsRowUI : MonoBehaviour
{
    public TMP_Text sceneNameLabel;
    public Toggle includeToggle;

    [Tooltip("Optional - moves this scene up in the order.")]
    public Button moveUpButton;
    [Tooltip("Optional - moves this scene down in the order.")]
    public Button moveDownButton;

    [FormerlySerializedAs("zoomSlider")]
    public Slider originalZoomSlider;
    [FormerlySerializedAs("zoomValueLabel")]
    [Tooltip("Optional - shows the current field of view next to the slider.")]
    public TMP_Text originalZoomValueLabel;

    public Slider reconstructionZoomSlider;
    [Tooltip("Optional - shows the current field of view next to the slider.")]
    public TMP_Text reconstructionZoomValueLabel;

    public Toggle fixedBlackScreenToggle;

    // trialPosition: 0 = first scene (practice), 1 = second scene, -1 = not included.
    public void Bind(string label, SceneSettings settings, int trialPosition,
        float minFieldOfView, float maxFieldOfView, SettingsUI owner)
    {
        sceneNameLabel.text = label;

        // At most two scenes can be included - ticking a third does nothing.
        includeToggle.SetIsOnWithoutNotify(settings.include);
        includeToggle.onValueChanged.AddListener(value =>
        {
            if (value && !owner.CanIncludeAnother())
            {
                includeToggle.SetIsOnWithoutNotify(false);
                return;
            }
            settings.include = value;
            owner.Refresh(); // trial positions (and the practice label) changed
        });

        if (moveUpButton != null) moveUpButton.onClick.AddListener(() => owner.MoveScene(settings.sceneName, -1));
        if (moveDownButton != null) moveDownButton.onClick.AddListener(() => owner.MoveScene(settings.sceneName, 1));

        BindZoom(originalZoomSlider, originalZoomValueLabel, settings.originalFieldOfView,
            value => settings.originalFieldOfView = value, minFieldOfView, maxFieldOfView);
        BindZoom(reconstructionZoomSlider, reconstructionZoomValueLabel, settings.reconstructionFieldOfView,
            value => settings.reconstructionFieldOfView = value, minFieldOfView, maxFieldOfView);

        // The black screen mode belongs to the trial position, not to this scene: it only
        // applies to scenes that are included, and swapping the order keeps it in place.
        bool hasPosition = trialPosition >= 0;
        fixedBlackScreenToggle.interactable = hasPosition;
        fixedBlackScreenToggle.SetIsOnWithoutNotify(hasPosition && SettingsStore.GetFixedBlackScreen(trialPosition));
        if (hasPosition)
            fixedBlackScreenToggle.onValueChanged.AddListener(value => SettingsStore.SetFixedBlackScreen(trialPosition, value));
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
