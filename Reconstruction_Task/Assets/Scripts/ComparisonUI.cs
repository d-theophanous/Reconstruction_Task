using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the "original scene" screenshot next to the "reconstruction" screenshot after
// a trial, with a Continue button. MainManager waits on the callback before moving on.
public class ComparisonUI : MonoBehaviour
{
    public GameObject panel;
    public RawImage originalImage;
    public RawImage reconstructionImage;
    public Button continueButton;
    public TMP_Text label;

    Action onContinue;

    public void Show(Texture2D original, Texture2D reconstruction, string labelText, Action onContinueCallback)
    {
        originalImage.texture = original;
        reconstructionImage.texture = reconstruction;
        if (label != null) label.text = labelText;

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
