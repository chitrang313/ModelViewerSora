using UnityEngine;

public class SceneViewLikeController:MonoBehaviour, IControllable {
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask focusMask = ~0;
    [SerializeField] private float fallbackFocusDistance = 100f;
    [SerializeField] private MovementClampBounds movementClamp;

    [Header("Orbit")]
    [SerializeField] private float orbitSensitivity = 3f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private float orbitDragThresholdPixels = 2f;

    [Header("Pan")]
    [SerializeField] private float panSensitivity = 0.02f;

    [Header("Zoom")]
    [SerializeField] private float scrollZoomSensitivity = 8f;
    [SerializeField] private float dragZoomSensitivity = 0.25f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float minOrthoSize = 0.1f;

    [Header("Flythrough")]
    [SerializeField] private float flySpeed = 30f;
    [SerializeField] private float fastFlyMultiplier = 3f;
    [SerializeField] private float slowFlyMultiplier = 0.3f;
    [SerializeField] private float lookSensitivity = 2f;

    private Transform camTransform;
    private Vector3 pivot;
    private float distanceToPivot;
    private float yaw;
    private float pitch;
    private bool isInitialized;

    private Vector3 orbitStartMousePosition;
    private bool orbitDragActive;

    // Initial State Variables
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialIsOrthographic;
    private float initialOrthographicSize;
    private float initialFieldOfView;
    private bool hasSavedInitialState;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (targetCamera == null) {
            enabled = false;
            Debug.LogError("SceneViewLikeController needs a target camera.");
            return;
        }

        if (movementClamp == null) {
            movementClamp = GetComponent<MovementClampBounds>();
        }

        camTransform = targetCamera.transform;

        SaveInitialState();
    }

    private void OnEnable() {
        if (!hasSavedInitialState || targetCamera == null) {
            return;
        }

        RestoreInitialState();
        InitializeFromCamera();
    }

    private void Update() {
        if (!isInitialized) {
            return;
        }

        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool rightMouse = Input.GetMouseButton(1);
        bool middleMouse = Input.GetMouseButton(2);
        bool orbiting = alt && Input.GetMouseButton(0);
        bool panning = middleMouse;
        bool dragZooming = alt && rightMouse;
        bool flying = rightMouse && !alt;

        if (Input.GetKeyDown(KeyCode.F)) {
            RecenterPivot();
        }

        if (alt && Input.GetMouseButtonDown(0)) {
            orbitStartMousePosition = Input.mousePosition;
            orbitDragActive = false;
        }

        if (!orbiting) {
            orbitDragActive = false;
        }

        if (orbiting) {
            if (!orbitDragActive) {
                Vector3 mouseDeltaFromPress = Input.mousePosition - orbitStartMousePosition;
                float sqrThreshold = orbitDragThresholdPixels * orbitDragThresholdPixels;
                orbitDragActive = mouseDeltaFromPress.sqrMagnitude >= sqrThreshold;
            }

            if (orbitDragActive) {
                Orbit(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
            }
        }

        if (panning) {
            Pan(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
        }

        if (dragZooming) {
            Zoom(Input.GetAxisRaw("Mouse Y") * dragZoomSensitivity);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scroll,0f)) {
            Zoom(scroll * scrollZoomSensitivity);
        }

        if (flying) {
            Fly(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
        }

        ApplyClampIfAvailable();
    }

    private void SaveInitialState() {
        if (hasSavedInitialState)
            return;

        initialPosition = camTransform.position;
        initialRotation = camTransform.rotation;
        initialIsOrthographic = targetCamera.orthographic;
        initialOrthographicSize = targetCamera.orthographicSize;
        initialFieldOfView = targetCamera.fieldOfView;

        hasSavedInitialState = true;
    }

    private void RestoreInitialState() {
        camTransform.SetPositionAndRotation(initialPosition,initialRotation);
        targetCamera.orthographic = initialIsOrthographic;
        targetCamera.orthographicSize = initialOrthographicSize;
        targetCamera.fieldOfView = initialFieldOfView;
    }

    private void InitializeFromCamera() {
        Vector3 euler = camTransform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);

        if (!TryGetWorldPointUnderScreenPoint(Input.mousePosition,out pivot)) {
            pivot = camTransform.position + camTransform.forward * fallbackFocusDistance;
        }

        distanceToPivot = Mathf.Max(minDistance,Vector3.Distance(camTransform.position,pivot));
        ApplyClampIfAvailable();
        isInitialized = true;
    }

    private void RecenterPivot() {
        if (TryGetWorldPointUnderScreenPoint(Input.mousePosition,out Vector3 hitPoint)) {
            pivot = hitPoint;
            distanceToPivot = Mathf.Max(minDistance,Vector3.Distance(camTransform.position,pivot));
        }
    }

    private bool TryGetWorldPointUnderScreenPoint(Vector3 screenPoint,out Vector3 worldPoint) {
        Ray ray = targetCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray,out RaycastHit hit,Mathf.Infinity,focusMask,QueryTriggerInteraction.Ignore)) {
            worldPoint = hit.point;
            return true;
        }

        worldPoint = default;
        return false;
    }

    private void Orbit(float mouseX,float mouseY) {
        yaw += mouseX * orbitSensitivity;
        pitch = Mathf.Clamp(pitch - mouseY * orbitSensitivity,minPitch,maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch,yaw,0f);
        camTransform.rotation = rotation;
        camTransform.position = pivot - (rotation * Vector3.forward * distanceToPivot);
    }

    private void Pan(float mouseX,float mouseY) {
        float panScale = targetCamera.orthographic ? targetCamera.orthographicSize : Mathf.Max(1f,distanceToPivot);
        Vector3 delta =
            (-camTransform.right * mouseX - camTransform.up * mouseY) *
            panSensitivity * panScale;

        camTransform.position += delta;
        pivot += delta;
    }

    private void Zoom(float zoomInput) {
        if (targetCamera.orthographic) {
            targetCamera.orthographicSize = Mathf.Max(minOrthoSize,targetCamera.orthographicSize - zoomInput);
            return;
        }

        distanceToPivot = Mathf.Max(minDistance,distanceToPivot - zoomInput);
        camTransform.position = pivot - (camTransform.forward * distanceToPivot);
    }

    private void Fly(float mouseX,float mouseY) {
        yaw += mouseX * lookSensitivity;
        pitch = Mathf.Clamp(pitch - mouseY * lookSensitivity,minPitch,maxPitch);
        camTransform.rotation = Quaternion.Euler(pitch,yaw,0f);

        Vector3 moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            moveInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            moveInput += Vector3.back;
        if (Input.GetKey(KeyCode.D))
            moveInput += Vector3.right;
        if (Input.GetKey(KeyCode.A))
            moveInput += Vector3.left;
        if (Input.GetKey(KeyCode.E))
            moveInput += Vector3.up;
        if (Input.GetKey(KeyCode.Q))
            moveInput += Vector3.down;

        if (moveInput.sqrMagnitude > 1f) {
            moveInput.Normalize();
        }

        float speed = flySpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            speed *= fastFlyMultiplier;
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
            speed *= slowFlyMultiplier;
        }

        Vector3 worldMove = camTransform.TransformDirection(moveInput) * speed * Time.unscaledDeltaTime;
        camTransform.position += worldMove;
        pivot += worldMove;
        distanceToPivot = Mathf.Max(minDistance,Vector3.Distance(camTransform.position,pivot));
    }

    private void ApplyClampIfAvailable() {
        if (movementClamp == null) {
            return;
        }

        movementClamp.ClampCameraAndPivot(camTransform,ref pivot,ref distanceToPivot,minDistance);
    }

    private static float NormalizePitch(float rawPitch) {
        if (rawPitch > 180f) {
            rawPitch -= 360f;
        }

        return rawPitch;
    }

    public void EnableControl() {
        this.enabled = true;
    }

    public void DisableControl() {
        this.enabled = false;
    }
}