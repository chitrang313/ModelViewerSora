#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Brownie.Tools;

[CustomEditor(typeof(SmartCameraController))]
public class SmartCameraControllerEditor:Editor {
    public override void OnInspectorGUI() {
        SmartCameraController script = (SmartCameraController)target;

        serializedObject.Update();

        EditorGUILayout.LabelField("📷 Camera Controller",EditorStyles.boldLabel);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Projection Settings",EditorStyles.boldLabel);

        var projectionProp = serializedObject.FindProperty("projectionMode");
        EditorGUILayout.PropertyField(projectionProp);

        var fovProp = serializedObject.FindProperty("fieldOfView");
        var orthoProp = serializedObject.FindProperty("orthographicSize");

        if ((SmartCameraController.ProjectionMode)projectionProp.enumValueIndex ==
            SmartCameraController.ProjectionMode.Perspective) {
            EditorGUILayout.Slider(fovProp,30f,120f);
        } else {
            EditorGUILayout.Slider(orthoProp,1f,20f);
        }

        if (GUILayout.Button("Apply Projection")) {
            script.ApplyProjection();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif