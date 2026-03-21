using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIPanelSlider:MonoBehaviour {
    [Header("UI References")]
    [Tooltip("The RectTransform of the panel you want to move.")]
    [SerializeField] private RectTransform panelRect;

    [Tooltip("The standard UI Button that controls this panel.")]
    [SerializeField] private Button panelButton;

    [Header("Positions (Anchored Position)")]
    [Tooltip("The position of the panel when it is fully visible on screen.")]
    [SerializeField] private Vector2 visiblePosition = Vector2.zero;

    [Tooltip("The position of the panel when it is hidden off screen.")]
    [SerializeField] private Vector2 hiddenPosition = new Vector2(-500f,0f);

    [Header("State Settings")]
    [Tooltip("Should the panel start open when the scene loads?")]
    [SerializeField] private bool startOpen = false;

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("Arrow Icon Settings")]
    [Tooltip("The RectTransform of the arrow image (child of the Button).")]
    [SerializeField] private RectTransform arrowIcon;

    [Tooltip("The X scale of the arrow when the panel is visible (1 or -1).")]
    [SerializeField] private float visibleArrowScaleX = -1f;

    [Tooltip("The X scale of the arrow when the panel is hidden (1 or -1).")]
    [SerializeField] private float hiddenArrowScaleX = 1f;

    // Manually tracks the current state since we are using a Button instead of a Toggle
    private bool isOpen;

    private void Awake() {
        if (panelRect == null || panelButton == null) {
            Debug.LogWarning($"Missing references on {gameObject.name}. Please assign Panel Rect and Panel Button.");
            return;
        }

        // Set initial state
        isOpen = startOpen;

        // Snap to initial position and scale without animating
        panelRect.anchoredPosition = isOpen ? visiblePosition : hiddenPosition;
        UpdateArrowFlip(instant: true);

        // Listen for the button click
        panelButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked() {
        // Toggle the state
        isOpen = !isOpen;

        // Kill any ongoing tweens to prevent jittering if clicked rapidly
        panelRect.DOKill();
        if (arrowIcon != null)
            arrowIcon.DOKill();

        // Animate the panel
        Vector2 targetPosition = isOpen ? visiblePosition : hiddenPosition;
        panelRect.DOAnchorPos(targetPosition,slideDuration).SetEase(slideEase);

        // Animate the arrow icon flip
        UpdateArrowFlip(instant: false);
    }

    private void UpdateArrowFlip(bool instant) {
        if (arrowIcon == null)
            return;

        float targetScaleX = isOpen ? visibleArrowScaleX : hiddenArrowScaleX;

        if (instant) {
            Vector3 currentScale = arrowIcon.localScale;
            currentScale.x = targetScaleX;
            arrowIcon.localScale = currentScale;
        } else {
            // Smoothly animate the flip using DoTween
            arrowIcon.DOScaleX(targetScaleX,slideDuration).SetEase(slideEase);
        }
    }

    private void OnDestroy() {
        if (panelButton != null) {
            panelButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
}