using UnityEngine;

namespace Brownie.Tools {
    [RequireComponent(typeof(Camera))]
    public class SmartCameraController:MonoBehaviour {
        public enum ProjectionMode {
            Perspective,
            Orthographic
        }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0,3,-6);

        [Header("Follow Settings")]
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private float followSpeed = 5f;

        [Header("Look Settings")]
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private float rotationSmooth = 5f;

        [Header("Projection")]
        [SerializeField] private ProjectionMode projectionMode = ProjectionMode.Perspective;
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private float orthographicSize = 5f;

        [Header("Advanced")]
        [SerializeField] private bool enableOrbit = false;
        [SerializeField] private float orbitSpeed = 50f;

        private Camera cam;

        private void Awake() {
            cam = GetComponent<Camera>();
            ApplyProjection();
        }

        private void LateUpdate() {
            if (target == null)
                return;

            HandleFollow();
            HandleLook();
            HandleOrbit();
        }

        private void HandleFollow() {
            Vector3 desiredPosition = target.position + offset;

            if (smoothFollow) {
                transform.position = Vector3.Lerp(transform.position,desiredPosition,followSpeed * Time.deltaTime);
            } else {
                transform.position = desiredPosition;
            }
        }

        private void HandleLook() {
            if (!lookAtTarget)
                return;

            Vector3 direction = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,rotationSmooth * Time.deltaTime);
        }

        private void HandleOrbit() {
            if (!enableOrbit)
                return;

            transform.RotateAround(target.position,Vector3.up,orbitSpeed * Time.deltaTime);
        }

        public void ApplyProjection() {
            if (cam == null)
                cam = GetComponent<Camera>();

            if (projectionMode == ProjectionMode.Perspective) {
                cam.orthographic = false;
                cam.fieldOfView = fieldOfView;
            } else {
                cam.orthographic = true;
                cam.orthographicSize = orthographicSize;
            }

            // Auto clipping optimization
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (target == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position,target.position);
            Gizmos.DrawSphere(target.position,0.2f);
        }
#endif
    }
}