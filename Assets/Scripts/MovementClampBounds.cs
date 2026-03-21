using UnityEngine;

public class MovementClampBounds : MonoBehaviour
{
    [SerializeField] private bool clampCameraToBounds = true;
    [SerializeField] private Vector3 clampBoundsCenter = Vector3.zero;
    [SerializeField] private Vector3 clampBoundsSize = new Vector3(1200f, 600f, 1200f);
    [SerializeField] private Color clampGizmoColor = new Color(0.2f, 0.9f, 1f, 1f);

    public bool ClampCameraToBounds
    {
        get => clampCameraToBounds;
        set => clampCameraToBounds = value;
    }

    public Vector3 ClampBoundsCenter
    {
        get => clampBoundsCenter;
        set => clampBoundsCenter = value;
    }

    public Vector3 ClampBoundsSize
    {
        get => clampBoundsSize;
        set => clampBoundsSize = new Vector3(
            Mathf.Max(0.01f, value.x),
            Mathf.Max(0.01f, value.y),
            Mathf.Max(0.01f, value.z));
    }

    public void ClampCameraAndPivot(Transform cameraTransform, ref Vector3 pivot, ref float distanceToPivot, float minDistance)
    {
        if (!clampCameraToBounds || cameraTransform == null)
        {
            return;
        }

        Vector3 half = ClampBoundsSize * 0.5f;
        Vector3 localCamPos = transform.InverseTransformPoint(cameraTransform.position);
        localCamPos.x = Mathf.Clamp(localCamPos.x, clampBoundsCenter.x - half.x, clampBoundsCenter.x + half.x);
        localCamPos.y = Mathf.Clamp(localCamPos.y, clampBoundsCenter.y - half.y, clampBoundsCenter.y + half.y);
        localCamPos.z = Mathf.Clamp(localCamPos.z, clampBoundsCenter.z - half.z, clampBoundsCenter.z + half.z);

        Vector3 clampedWorldCam = transform.TransformPoint(localCamPos);
        Vector3 camDelta = clampedWorldCam - cameraTransform.position;
        if (camDelta.sqrMagnitude > 0f)
        {
            cameraTransform.position = clampedWorldCam;
            pivot += camDelta;
            distanceToPivot = Mathf.Max(minDistance, Vector3.Distance(cameraTransform.position, pivot));
        }
    }

    private void OnDrawGizmos()
    {
        if (!clampCameraToBounds)
        {
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = clampGizmoColor;
        Gizmos.DrawWireCube(clampBoundsCenter, clampBoundsSize);
    }
}
