using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Consent gate. The researcher types the participant ID; agreeing records consent and (via
// OnAgreed -> OnAgree) moves on to the demographics screen. The consent copy itself is
// authored directly on the panel's TMP Text in the Editor.
public class ConsentUI : MonoBehaviour
{
    public Button agreeButton;
    public Button declineButton;

    [Tooltip("Filled in by the researcher. Agree stays disabled until an ID is entered.")]
    public TMP_InputField participantIdField;

    [Tooltip("Shown after the participant agrees (asks for age and gender).")]
    public GameObject demographicsUI;

    public UnityEvent OnAgreed;
    public UnityEvent OnDeclined;

    // Fresh, empty field every time the screen is shown (a new participant after each session).
    void OnEnable()
    {
        participantIdField.text = "";
        Refresh();
    }

    void Start()
    {
        participantIdField.onValueChanged.AddListener(_ => Refresh());
        agreeButton.onClick.AddListener(HandleAgree);
        if (declineButton != null) declineButton.onClick.AddListener(HandleDecline);
        Refresh();
    }

    void Refresh()
    {
        agreeButton.interactable = !string.IsNullOrWhiteSpace(participantIdField.text);
    }

    // Wired to OnAgreed in the Inspector.
    public void OnAgree()
    {
        gameObject.SetActive(false);

        if (demographicsUI != null)
            demographicsUI.SetActive(true);
        else
            MainManager.Instance.MenuUI.SetActive(true); // no demographics screen assigned yet
    }

    void HandleAgree()
    {
        if (string.IsNullOrWhiteSpace(participantIdField.text)) return;

        SessionInfo.RecordConsent(participantIdField.text);
        OnAgreed?.Invoke();
    }

    void HandleDecline()
    {
        OnDeclined?.Invoke();
    }
}
