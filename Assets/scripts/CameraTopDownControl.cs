using UnityEngine;

public class CameraTopDownControl:MonoBehaviour, IControllable {
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private MovementClampBounds movementClamp;
    [SerializeField] private float cameraHeight = 500f;
    [SerializeField] private float defaultOrthographicSize = 250f;
    [SerializeField] private float minOrthographicSize = 25f;
    [SerializeField] private float maxOrthographicSize = 1000f;
    [SerializeField] private float zoomStep = 25f;

    [Header("Pan")]
    [SerializeField] private float dragPanMultiplier = 1f;

    private Transform camTransform;
    private Vector3 lastMousePosition;
    private bool isDragging;

    private void Awake() {
        if (targetCamera == null) {
            targetCamera = Camera.main;
        }

        if (targetCamera == null) {
            enabled = false;
            Debug.LogError("CameraTopDownControl needs a target camera.");
            return;
        }

        if (movementClamp == null) {
            movementClamp = GetComponent<MovementClampBounds>();
        }

        camTransform = targetCamera.transform;
    }

    private void OnEnable() {
        if (camTransform == null) {
            return;
        }

        CenterOnModel();
    }

    private void OnDisable() {
        isDragging = false;
    }

    private void Update() {
        if (camTransform == null) {
            return;
        }

        HandleZoom();
        HandlePan();
        MaintainTopDownView();
        ApplyClampIfAvailable();
    }

    public void CenterOnModel() {
        ConfigureCamera();

        Vector3 modelCenter = ResolveModelCenter();
        Vector3 cameraPosition = new Vector3(modelCenter.x,cameraHeight,modelCenter.z);
        camTransform.SetPositionAndRotation(cameraPosition,Quaternion.Euler(90f,0f,0f));

        ApplyClampIfAvailable();
    }

    private void ConfigureCamera() {
        targetCamera.orthographic = true;
        targetCamera.orthographicSize = defaultOrthographicSize;
        camTransform.rotation = Quaternion.Euler(90f,0f,0f);
    }

    private Vector3 ResolveModelCenter() {
        if (TryGetBounds(out Bounds bounds)) {
            return bounds.center;
        }

        if (movementClamp != null) {
            return movementClamp.transform.TransformPoint(movementClamp.ClampBoundsCenter);
        }

        return Vector3.zero;
    }

    private bool TryGetBounds(out Bounds bounds) {
        Renderer[] renderers = modelRoot != null ?
            modelRoot.GetComponentsInChildren<Renderer>() :
            FindObjectsOfType<Renderer>();

        bool hasBounds = false;
        bounds = default;

        for (int i = 0;i < renderers.Length;i++) {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null || !currentRenderer.enabled || !currentRenderer.gameObject.activeInHierarchy) {
                continue;
            }

            if (currentRenderer.GetComponentInParent<Camera>() != null) {
                continue;
            }

            if (!hasBounds) {
                bounds = currentRenderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(currentRenderer.bounds);
        }

        return hasBounds;
    }

    private void HandleZoom() {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll,0f)) {
            return;
        }

        targetCamera.orthographicSize = Mathf.Clamp(
            targetCamera.orthographicSize - (scroll * zoomStep),
            minOrthographicSize,
            maxOrthographicSize);
    }

    private void HandlePan() {
        if (Input.GetMouseButtonDown(1)) {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1)) {
            isDragging = false;
        }

        if (!isDragging || !Input.GetMouseButton(1)) {
            return;
        }

        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;

        if (mouseDelta.sqrMagnitude <= 0f) {
            return;
        }

        float worldUnitsPerPixelY = (targetCamera.orthographicSize * 2f) / Mathf.Max(1,Screen.height);
        float worldUnitsPerPixelX = worldUnitsPerPixelY * targetCamera.aspect;

        Vector3 worldDelta =
            (-camTransform.right * mouseDelta.x * worldUnitsPerPixelX) -
            (camTransform.up * mouseDelta.y * worldUnitsPerPixelY);

        camTransform.position += worldDelta * dragPanMultiplier;
    }

    private void MaintainTopDownView() {
        Vector3 currentPosition = camTransform.position;
        currentPosition.y = cameraHeight;
        camTransform.SetPositionAndRotation(currentPosition,Quaternion.Euler(90f,0f,0f));
        targetCamera.orthographic = true;
    }

    private void ApplyClampIfAvailable() {
        if (movementClamp == null || !movementClamp.ClampCameraToBounds) {
            return;
        }

        Vector3 localCameraPosition = movementClamp.transform.InverseTransformPoint(camTransform.position);
        Vector3 halfBounds = movementClamp.ClampBoundsSize * 0.5f;

        localCameraPosition.x = Mathf.Clamp(
            localCameraPosition.x,
            movementClamp.ClampBoundsCenter.x - halfBounds.x,
            movementClamp.ClampBoundsCenter.x + halfBounds.x);
        localCameraPosition.z = Mathf.Clamp(
            localCameraPosition.z,
            movementClamp.ClampBoundsCenter.z - halfBounds.z,
            movementClamp.ClampBoundsCenter.z + halfBounds.z);

        camTransform.position = movementClamp.transform.TransformPoint(localCameraPosition);
    }

    public void EnableControl() {
        this.enabled = true;
    }

    public void DisableControl() {
        this.enabled = false;
    }
}
