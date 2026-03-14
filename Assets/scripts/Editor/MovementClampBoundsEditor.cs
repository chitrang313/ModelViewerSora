#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(MovementClampBounds))]
public class MovementClampBoundsEditor : Editor
{
    private readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

    private void OnSceneGUI()
    {
        var clamp = (MovementClampBounds)target;
        if (!clamp.ClampCameraToBounds)
        {
            return;
        }

        Transform t = clamp.transform;

        using (new Handles.DrawingScope(t.localToWorldMatrix))
        {
            boundsHandle.center = clamp.ClampBoundsCenter;
            boundsHandle.size = clamp.ClampBoundsSize;

            EditorGUI.BeginChangeCheck();
            boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(clamp, "Adjust Camera Clamp Bounds");
                clamp.ClampBoundsCenter = boundsHandle.center;
                clamp.ClampBoundsSize = boundsHandle.size;
                EditorUtility.SetDirty(clamp);
            }
        }
    }
}
#endif
