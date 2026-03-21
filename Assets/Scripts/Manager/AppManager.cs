using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public interface IControllable {
    void EnableControl();
    void DisableControl();
}
public enum ControlMode {
    SceneViewLike,
    TopDown
}
public class AppManager:MonoBehaviour {
    public static AppManager instance;
    [SerializeField] private Button processDataButton;
    [SerializeField] private TMP_Dropdown controlDropDownButton;
    [SerializeField] private ControlMode selectedControlType;
    
    [Header("Mvoement Controlls")]
    [SerializeField] private SceneViewLikeController sceneViewLikeController;
    [SerializeField] private CameraTopDownControl cameraTopDownControl;

    [SerializeField] private List<IControllable> controlls;

    [HideInInspector] public UnityEvent onDataProcessedEvent;

    [Space(4)]
    [SerializeField] private TextMeshProUGUI howToUseControlText;
    [SerializeField][TextArea(3,10)] private string scemeViewLikeControlText;
    [SerializeField][TextArea(3,10)] private string cameraTopDownControlText;

    private void Awake() {
        Singletone();
        processDataButton.onClick.RemoveAllListeners();
        processDataButton.onClick.AddListener(OnClickProcessData);

        controlls = new List<IControllable>() {
            sceneViewLikeController,
            cameraTopDownControl
        };
        InitializeDropdown();
    }

    private void OnControlDropDownValueChanged(int selectedControlType) {
        this.selectedControlType = (ControlMode)selectedControlType;
        foreach (var controll in controlls) {
            controll.DisableControl();
        }
        controlls[(int)this.selectedControlType].EnableControl();
        
        howToUseControlText.text = this.selectedControlType switch {
            ControlMode.SceneViewLike => scemeViewLikeControlText,
            ControlMode.TopDown => cameraTopDownControlText,
            _ => howToUseControlText.text
        };
    }

    private void Singletone() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    private void OnClickProcessData() {
        onDataProcessedEvent?.Invoke();
    }
    private void InitializeDropdown() {
        if (controlDropDownButton == null) {
            Debug.LogError("Control Dropdown Button is not assigned!");
            return;
        }

        // 1. Clear any placeholder data currently in the dropdown
        controlDropDownButton.ClearOptions();

        // 2. Fetch the string names from the enum
        string[] modeNames = Enum.GetNames(typeof(ControlMode));

        // 3. Convert the array to a List and apply it to the dropdown
        List<string> options = new List<string>(modeNames);
        controlDropDownButton.AddOptions(options);

        // 4. Set a default value and refresh the visual state
        controlDropDownButton.value = 0;
        controlDropDownButton.RefreshShownValue();

        controlDropDownButton.onValueChanged.RemoveAllListeners();
        controlDropDownButton.onValueChanged.AddListener(OnControlDropDownValueChanged);
    }
}//AppManager class end.
