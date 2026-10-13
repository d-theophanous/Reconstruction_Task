using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

// Shows the "original scene" screenshot next to the "reconstruction" screenshot after
// a trial, with a Continue button. MainManager waits on the callback before moving on.
public class ComparisonUI : MonoBehaviour
{
    public GameObject panel;
    public RawImage originalImage;
    public RawImage reconstructionImage;
    public Button continueButton;

    [Tooltip("LocalizeStringEvent on the label text (its Update String event should set the TMP text). Show() swaps its string reference, so the text follows the active language.")]
    public LocalizeStringEvent labelLocalizer;

    Action onContinue;

    public void Show(Texture2D original, Texture2D reconstruction, LocalizedString labelText, Action onContinueCallback)
    {
        originalImage.texture = original;
        reconstructionImage.texture = reconstruction;
        if (labelLocalizer != null && labelText != null) labelLocalizer.StringReference = labelText;

        onContinue = onContinueCallback;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(HandleContinue);

        panel.SetActive(true);
    }

    void HandleContinue()
    {
        panel.SetActive(false);
        var callback = onContinue;
        onContinue = null;
        callback?.Invoke();
    }
}
