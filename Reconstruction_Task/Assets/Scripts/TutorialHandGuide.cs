using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows an animated "hand" UI icon that loops between two points to
/// demonstrate an interaction (e.g. "click and drag this object").
/// The loop stops the first time the player clicks anywhere.
///
/// Setup:
/// 1. Add a UI Image (your hand/cursor sprite) as a child of a Canvas.
/// 2. Add two empty RectTransforms (or reuse existing UI elements) to
///    mark the start and end screen positions.
/// 3. Drag all three into the fields below.
/// </summary>
public class TutorialHandGuide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform handIcon;
    public RectTransform startPoint;
    public RectTransform endPoint;
    [SerializeField] private CanvasGroup handCanvasGroup; // optional, for fade

    [Header("Timing")]
    [SerializeField] private float moveDuration = 1.2f;
    [SerializeField] private float pauseAtEnds = 0.35f;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start Point Pulse")]
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve pulseCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Behaviour")]
    [SerializeField] private bool stopOnAnyClick = true;
    [SerializeField] private float startDelay = 0.5f;

    private bool _isActive;
    private Coroutine _loopRoutine;
    private Vector3 _originalScale = Vector3.one;
    public bool PulseAtStart;
    public bool PulseAtEnd;

    private void OnEnable()
    {
        // The dim overlay, hand and any text must not swallow clicks - the very first click
        // should reach the object/palette underneath (it still dismisses the tutorial below).
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        _isActive = true;
        if (handIcon != null)
        {
            handIcon.position = startPoint.position;
            _originalScale = handIcon.localScale;
        }
        _loopRoutine = StartCoroutine(RunTutorial());
    }

    private void Update()
    {
        if (!_isActive || !stopOnAnyClick) return;

        if (Input.GetMouseButtonDown(0))
        {
            StopTutorial();
        }
    }

    private IEnumerator RunTutorial()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return FadeTo(1f);

        while (_isActive)
        {
            if (PulseAtStart)
                yield return PulseHand();
            yield return WaitWhileActive(pauseAtEnds);

            if (!_isActive) break;

            yield return MoveHand(startPoint.position, endPoint.position);

            if (PulseAtEnd)
                yield return PulseHand();
            yield return WaitWhileActive(pauseAtEnds);

            if (!_isActive) break;

            yield return MoveHand(endPoint.position, startPoint.position);
        }
    }

    private IEnumerator PulseHand()
    {
        if (handIcon == null) yield break;

        float halfDuration = pulseDuration / 2f;
        Vector3 peakScale = _originalScale * pulseScale;

        yield return ScaleHand(_originalScale, peakScale, halfDuration);
        if (!_isActive) yield break;
        yield return ScaleHand(peakScale, _originalScale, halfDuration);
    }

    private IEnumerator ScaleHand(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration && _isActive)
        {
            t += Time.deltaTime;
            float eased = pulseCurve.Evaluate(Mathf.Clamp01(t / duration));
            handIcon.localScale = Vector3.Lerp(from, to, eased);
            yield return null;
        }
        handIcon.localScale = to;
    }

    private IEnumerator MoveHand(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < 1f && _isActive)
        {
            t += Time.deltaTime / moveDuration;
            float eased = easeCurve.Evaluate(Mathf.Clamp01(t));
            handIcon.position = Vector3.Lerp(from, to, eased);
            yield return null;
        }
    }

    private IEnumerator WaitWhileActive(float seconds)
    {
        float t = 0f;
        while (t < seconds && _isActive)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (handCanvasGroup == null) yield break;

        float startAlpha = handCanvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            handCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }
        handCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// Call this manually if you want to stop the tutorial from another
    /// script (e.g. once the player actually performs the real drag),
    /// instead of relying only on "any click".
    /// </summary>
    public void StopTutorial()
    {
        if (!_isActive) return;
        _isActive = false;

        if (_loopRoutine != null)
            StopCoroutine(_loopRoutine);

        if (handIcon != null)
            handIcon.localScale = _originalScale;

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        yield return FadeTo(0f);
        gameObject.SetActive(false);
    }
}
