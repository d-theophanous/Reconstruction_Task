using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Self-contained consent gate. Doesn't know about MainManager or the menu - wire
// OnAgreed/OnDeclined in the Inspector to show/hide whatever panels this project needs.
// The consent copy itself is authored directly on the panel's TMP Text in the Editor.
public class ConsentUI : MonoBehaviour
{
    public Button agreeButton;
    public Button declineButton;

    [Tooltip("Optional - leave unassigned to auto-generate an anonymous participant id.")]
    public TMP_InputField participantIdField;

    public UnityEvent OnAgreed;
    public UnityEvent OnDeclined;

    void Start()
    {
        agreeButton.onClick.AddListener(HandleAgree);
        if (declineButton != null) declineButton.onClick.AddListener(HandleDecline);
    }

    public void OnAgree()
    {
        gameObject.SetActive(false);
        MainManager.Instance.MenuUI.SetActive(true);
    }

    void HandleAgree()
    {
        string id = participantIdField != null ? participantIdField.text : null;
        SessionInfo.RecordConsent(id);
        OnAgreed?.Invoke();
    }

    void HandleDecline()
    {
        OnDeclined?.Invoke();
    }
}
