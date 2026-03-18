using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CameraControlModeSwitcher:MonoBehaviour {
    private const int SceneViewModeIndex = 0;
    private const int TopDownModeIndex = 1;

    [SerializeField] private SceneViewLikeController sceneViewController;
    [SerializeField] private CameraTopDownControl topDownController;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Vector2 dropdownSize = new Vector2(220f,36f);
    [SerializeField] private Vector2 dropdownOffset = new Vector2(-10f,-10f);

    private Dropdown modeDropdown;
    private MethodInfo initializeSceneViewMethod;

    private void Awake() {
        if (sceneViewController == null) {
            sceneViewController = FindObjectOfType<SceneViewLikeController>();
        }

        if (topDownController == null) {
            topDownController = FindObjectOfType<CameraTopDownControl>(true);
        }

        if (targetCanvas == null) {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        initializeSceneViewMethod = typeof(SceneViewLikeController).GetMethod(
            "InitializeFromCamera",
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void Start() {
        if (sceneViewController == null || topDownController == null || targetCanvas == null) {
            enabled = false;
            Debug.LogError("CameraControlModeSwitcher needs both camera controllers and a target canvas.");
            return;
        }

        CreateDropdown();

        int initialMode = topDownController.enabled ? TopDownModeIndex : SceneViewModeIndex;
        modeDropdown.SetValueWithoutNotify(initialMode);
        ApplyMode(initialMode);
    }

    private void OnDestroy() {
        if (modeDropdown != null) {
            modeDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    private void OnDropdownValueChanged(int selectedIndex) {
        ApplyMode(selectedIndex);
    }

    private void ApplyMode(int selectedIndex) {
        bool useTopDown = selectedIndex == TopDownModeIndex;

        if (useTopDown) {
            if (sceneViewController.enabled) {
                sceneViewController.enabled = false;
            }

            if (!topDownController.enabled) {
                topDownController.enabled = true;
            }

            return;
        }

        if (topDownController.enabled) {
            topDownController.enabled = false;
        }

        if (!sceneViewController.enabled) {
            sceneViewController.enabled = true;
        }

        initializeSceneViewMethod?.Invoke(sceneViewController,null);
    }

    private void CreateDropdown() {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject dropdownObject = CreateUIObject("Camera Control Dropdown",targetCanvas.transform);
        RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        ConfigureRect(dropdownRect,new Vector2(1f,1f),new Vector2(1f,1f),new Vector2(1f,1f),dropdownOffset,dropdownSize);

        Image backgroundImage = dropdownObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.95f,0.95f,0.95f,0.96f);

        modeDropdown = dropdownObject.AddComponent<Dropdown>();
        modeDropdown.targetGraphic = backgroundImage;

        Text captionText = CreateText("Label",dropdownObject.transform,font,TextAnchor.MiddleLeft);
        ConfigureRect(
            captionText.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f,0.5f),
            new Vector2(-8f,0f),
            new Vector2(-40f,0f));
        captionText.color = new Color(0.1f,0.1f,0.1f,1f);

        Text arrowText = CreateText("Arrow",dropdownObject.transform,font,TextAnchor.MiddleCenter);
        ConfigureRect(
            arrowText.rectTransform,
            new Vector2(1f,0.5f),
            new Vector2(1f,0.5f),
            new Vector2(0.5f,0.5f),
            new Vector2(-16f,0f),
            new Vector2(20f,20f));
        arrowText.text = "v";
        arrowText.color = new Color(0.2f,0.2f,0.2f,1f);

        RectTransform templateRect = CreateTemplate(dropdownObject.transform,font);

        modeDropdown.template = templateRect;
        modeDropdown.captionText = captionText;
        modeDropdown.itemText = templateRect.GetComponentInChildren<Toggle>(true).GetComponentInChildren<Text>(true);
        modeDropdown.options = new List<Dropdown.OptionData> {
            new Dropdown.OptionData("Scene View"),
            new Dropdown.OptionData("Top Down")
        };
        modeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        modeDropdown.RefreshShownValue();
    }

    private RectTransform CreateTemplate(Transform parent,Font font) {
        GameObject templateObject = CreateUIObject("Template",parent);
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        ConfigureRect(
            templateRect,
            new Vector2(0f,0f),
            new Vector2(1f,0f),
            new Vector2(0.5f,1f),
            new Vector2(0f,-2f),
            new Vector2(0f,72f));

        Image templateImage = templateObject.AddComponent<Image>();
        templateImage.color = new Color(1f,1f,1f,0.98f);

        ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 15f;

        GameObject viewportObject = CreateUIObject("Viewport",templateObject.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        ConfigureRect(viewportRect,Vector2.zero,Vector2.one,new Vector2(0.5f,0.5f),Vector2.zero,Vector2.zero);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f,1f,1f,0.01f);
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = CreateUIObject("Content",viewportObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        ConfigureRect(
            contentRect,
            new Vector2(0f,1f),
            new Vector2(1f,1f),
            new Vector2(0.5f,1f),
            Vector2.zero,
            new Vector2(0f,30f));

        VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.spacing = 2f;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject itemObject = CreateUIObject("Item",contentObject.transform);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        ConfigureRect(
            itemRect,
            new Vector2(0f,1f),
            new Vector2(1f,1f),
            new Vector2(0.5f,1f),
            Vector2.zero,
            new Vector2(0f,30f));

        Image itemBackground = itemObject.AddComponent<Image>();
        itemBackground.color = new Color(0.92f,0.92f,0.92f,1f);

        Toggle itemToggle = itemObject.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;

        GameObject checkmarkObject = CreateUIObject("Checkmark",itemObject.transform);
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        ConfigureRect(
            checkmarkRect,
            new Vector2(0f,0.5f),
            new Vector2(0f,0.5f),
            new Vector2(0.5f,0.5f),
            new Vector2(12f,0f),
            new Vector2(14f,14f));

        Image checkmarkImage = checkmarkObject.AddComponent<Image>();
        checkmarkImage.color = new Color(0.18f,0.48f,0.85f,1f);
        itemToggle.graphic = checkmarkImage;

        Text itemText = CreateText("Item Label",itemObject.transform,font,TextAnchor.MiddleLeft);
        ConfigureRect(
            itemText.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f,0.5f),
            new Vector2(14f,0f),
            new Vector2(-28f,0f));
        itemText.color = new Color(0.1f,0.1f,0.1f,1f);

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        templateObject.SetActive(false);

        return templateRect;
    }

    private static GameObject CreateUIObject(string objectName,Transform parent) {
        GameObject uiObject = new GameObject(objectName,typeof(RectTransform));
        uiObject.transform.SetParent(parent,false);
        return uiObject;
    }

    private static Text CreateText(string objectName,Transform parent,Font font,TextAnchor alignment) {
        GameObject textObject = CreateUIObject(objectName,parent);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 18;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void ConfigureRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta) {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }
}
