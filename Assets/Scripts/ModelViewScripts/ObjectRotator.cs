// ============================================================
// ObjectRotator.cs
// Namespace : InteractiveToolkit.Rotation
// Purpose   : Smoothly rotates a GameObject around a chosen
//             local axis with optional easing and direction
//             control, all configurable from the Inspector.
// ============================================================

using UnityEngine;

namespace InteractiveToolkit.Rotation
{
    /// <summary>
    /// Continuously rotates a GameObject around its LOCAL axis.
    /// Supports preset axis selection, custom axis, speed control,
    /// clockwise / counterclockwise toggle, and optional easing.
    /// All Quaternion-based — no gimbal lock.
    /// </summary>
    [AddComponentMenu("InteractiveToolkit/Object Rotator")]
    [DisallowMultipleComponent]
    public sealed class ObjectRotator : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────
        #region Enums

        /// <summary>Preset rotation axes available in the Inspector.</summary>
        public enum RotationAxis
        {
            X,
            Y,
            Z,
            Custom
        }

        /// <summary>Optional easing curve applied to the rotation speed.</summary>
        public enum EasingMode
        {
            None,
            SineWave,
            PingPong
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Inspector Fields — Rotation Settings

        [Header("Rotation Settings")]

        [Tooltip("Select a preset axis or choose Custom to define your own.")]
        [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

        [Tooltip("Custom axis used when Rotation Axis is set to Custom. "
                 + "Does not need to be normalised — it is normalised at runtime.")]
        [SerializeField] private Vector3 customAxis = Vector3.up;

        [Tooltip("Degrees per second at which the object rotates.")]
        [Min(0f)]
        [SerializeField] private float rotationSpeed = 90f;

        [Tooltip("When enabled the object rotates clockwise (negative angle). "
                 + "When disabled it rotates counter-clockwise (positive angle).")]
        [SerializeField] private bool clockwise = false;

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Inspector Fields — State

        [Header("State")]

        [Tooltip("Enable or disable rotation at runtime without disabling the component.")]
        [SerializeField] private bool isRotating = true;

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Inspector Fields — Easing (Advanced)

        [Header("Easing (Advanced)")]

        [Tooltip("None = constant speed. SineWave = breathes in and out smoothly. "
                 + "PingPong = linearly accelerates then reverses.")]
        [SerializeField] private EasingMode easingMode = EasingMode.None;

        [Tooltip("How many full easing cycles occur per second.")]
        [Min(0.01f)]
        [SerializeField] private float easingFrequency = 1f;

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Private Runtime State

        /// <summary>Resolved, normalised axis used each frame.</summary>
        private Vector3 _resolvedAxis;

        /// <summary>Accumulated time used by the easing functions.</summary>
        private float _easingTime;

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            resolveAxis();
        }

        private void Update()
        {
            if (!isRotating) {
                return;
            }

            float speed = computeEasedSpeed();
            applyRotation(speed);
        }

        /// <summary>
        /// Recalculates the resolved axis if Inspector values change in Play Mode.
        /// Only relevant during editor iteration — stripped from release builds.
        /// </summary>
        private void OnValidate()
        {
            resolveAxis();
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Start rotation programmatically.</summary>
        public void StartRotation()
        {
            isRotating = true;
        }

        /// <summary>Stop rotation programmatically.</summary>
        public void StopRotation()
        {
            isRotating = false;
        }

        /// <summary>Toggle rotation on / off programmatically.</summary>
        public void ToggleRotation()
        {
            isRotating = !isRotating;
        }

        /// <summary>Change the rotation speed at runtime.</summary>
        /// <param name="newSpeed">New degrees-per-second value (clamped to ≥ 0).</param>
        public void SetSpeed(float newSpeed)
        {
            rotationSpeed = Mathf.Max(0f, newSpeed);
        }

        /// <summary>Reset the easing accumulator to zero.</summary>
        public void ResetEasing()
        {
            _easingTime = 0f;
        }

        #endregion

        // ─────────────────────────────────────────────────────────
        #region Private Helpers

        /// <summary>
        /// Maps the Inspector enum to a concrete normalised Vector3 axis.
        /// Called in Awake and OnValidate.
        /// </summary>
        private void resolveAxis()
        {
            switch (rotationAxis) {
                case RotationAxis.X:
                    _resolvedAxis = Vector3.right;
                    break;
                case RotationAxis.Y:
                    _resolvedAxis = Vector3.up;
                    break;
                case RotationAxis.Z:
                    _resolvedAxis = Vector3.forward;
                    break;
                case RotationAxis.Custom:
                    if (customAxis == Vector3.zero) {
                        Debug.LogWarning($"[{nameof(ObjectRotator)}] Custom axis is zero on "
                                         + $"'{gameObject.name}'. Defaulting to Vector3.up.");
                        _resolvedAxis = Vector3.up;
                    } else {
                        _resolvedAxis = customAxis.normalized;
                    }
                    break;
                default:
                    _resolvedAxis = Vector3.up;
                    break;
            }
        }

        /// <summary>
        /// Returns the frame speed after applying the selected easing mode.
        /// </summary>
        private float computeEasedSpeed()
        {
            _easingTime += Time.deltaTime;

            float multiplier;

            switch (easingMode) {
                case EasingMode.SineWave:
                    // Oscillates between 0 and 1 using a sine wave
                    multiplier = (Mathf.Sin(_easingTime * easingFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                    break;

                case EasingMode.PingPong:
                    // Linearly goes 0 → 1 → 0 at the given frequency
                    multiplier = Mathf.PingPong(_easingTime * easingFrequency, 1f);
                    break;

                case EasingMode.None:
                default:
                    multiplier = 1f;
                    break;
            }

            return rotationSpeed * multiplier;
        }

        /// <summary>
        /// Applies a Quaternion rotation around the resolved LOCAL axis for this frame.
        /// Using Quaternion multiplication avoids gimbal lock entirely.
        /// </summary>
        /// <param name="speed">Effective degrees-per-second for this frame.</param>
        private void applyRotation(float speed)
        {
            float direction = clockwise ? -1f : 1f;
            float angle = direction * speed * Time.deltaTime;

            // Build a delta rotation around the LOCAL axis
            Quaternion deltaRotation = Quaternion.AngleAxis(angle, _resolvedAxis);

            // Multiply current local rotation by the delta — stays in local space
            transform.localRotation = transform.localRotation * deltaRotation;
        }

        #endregion
    }
}
