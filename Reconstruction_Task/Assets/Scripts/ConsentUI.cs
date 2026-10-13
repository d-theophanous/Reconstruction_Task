using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Consent gate. Agreeing records consent and (via OnAgreed -> OnAgree) moves on to the
// demographics screen. The consent copy itself is authored directly on the panel's TMP
// Text in the Editor.
public class ConsentUI : MonoBehaviour
{
    public Button agreeButton;
    public Button declineButton;

    [Tooltip("Shown after the participant agrees (asks for age and gender).")]
    public GameObject demographicsUI;

    public UnityEvent OnAgreed;
    public UnityEvent OnDeclined;

    void Start()
    {
        agreeButton.onClick.AddListener(HandleAgree);
        if (declineButton != null) declineButton.onClick.AddListener(HandleDecline);
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
        SessionInfo.RecordConsent();
        OnAgreed?.Invoke();
    }

    void HandleDecline()
    {
        OnDeclined?.Invoke();
    }
}
