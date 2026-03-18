using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshCollider))]
public class LidarDataMapper:MonoBehaviour {
    [Header("Data Source")]
    [Tooltip("Drag and drop your exported .csv file here.")]
    public TextAsset csvDataFile;

    [Header("Export Shifts (Applied during OBJ export)")]
    public double shiftX = -725559.499;
    public double shiftY = -940542.892;

    [Header("OBJ to Unity Local Axis Mapping")]
    [Tooltip("Flips the X axis locally. Unity often requires this when importing OBJs.")]
    public bool invertX = true;
    [Tooltip("Flips the Y axis locally.")]
    public bool invertY = false;
    [Tooltip("Flips the Z axis locally.")]
    public bool invertZ = false;
    [Tooltip("Swap Northing (Y) and Elevation (Z). If your parent model_00 is rotated X=-90, you likely need to UNCHECK this.")]
    public bool swapYandZ = false;

    [Header("Raycast Selection Settings")]
    [Tooltip("For a Top-Down X=90 Orthographic Camera, set this to NegativeY.")]
    public RaycastAxis raycastDirection = RaycastAxis.NegativeY;

    [Header("Gizmo Settings")]
    [Tooltip("Draw a yellow bounding box highlighting the CSV data area in the Scene view.")]
    public bool drawDataBoundsGizmo = true;

    public enum RaycastAxis {
        CameraDefault,
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY,
        PositiveZ,
        NegativeZ
    }

    [System.Serializable]
    public struct LidarPoint {
        public double originalX;
        public double originalY;
        public double elev;
        public string latitude;
        public string longitude;
        public string pointClass;

        // The mathematical position where this point should sit in Unity's local space
        public Vector3 expectedLocalPos;
    }

    private List<LidarPoint> lidarPoints = new List<LidarPoint>();
    private MeshCollider meshCollider;

    // Original GPS Space Bounds
    private double minOriginalX, maxOriginalX;
    private double minOriginalY, maxOriginalY;
    private double minElev, maxElev;
    private bool hasBounds = false;
    private void Awake() {
        meshCollider = GetComponent<MeshCollider>();
    }
    private void OnEnable() {
        AppManager.instance.onDataProcessedEvent.AddListener(ParseCSVData);
    }
    private void OnDisable() {
        AppManager.instance.onDataProcessedEvent.RemoveListener(ParseCSVData);
    }
    //[ContextMenu("Load CSV for Editor Preview")]
    void ParseCSVData() {
        if (csvDataFile == null) {
            Debug.LogError("CSV Data File is not assigned!");
            return;
        }

        lidarPoints.Clear();
        hasBounds = false;

        minOriginalX = double.MaxValue;
        maxOriginalX = double.MinValue;
        minOriginalY = double.MaxValue;
        maxOriginalY = double.MinValue;
        minElev = double.MaxValue;
        maxElev = double.MinValue;

        using (System.IO.StringReader reader = new System.IO.StringReader(csvDataFile.text)) {
            string line = reader.ReadLine(); // Skip header row

            while ((line = reader.ReadLine()) != null) {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] cols = line.Split(',');

                if (cols.Length >= 5) {
                    LidarPoint pt = new LidarPoint();

                    pt.originalX = double.Parse(cols[0],CultureInfo.InvariantCulture);
                    pt.originalY = double.Parse(cols[1],CultureInfo.InvariantCulture);
                    pt.elev = double.Parse(cols[2],CultureInfo.InvariantCulture);

                    pt.latitude = cols[3].Replace("\"","").Trim();
                    pt.longitude = cols[4].Replace("\"","").Trim();
                    pt.pointClass = cols.Length > 6 ? cols[6] : "Unknown";

                    // Use the helper method to map to Unity space
                    pt.expectedLocalPos = MapOriginalToUnity(pt.originalX,pt.originalY,pt.elev);
                    lidarPoints.Add(pt);

                    // Track exact min/max bounds based on Original GPS X and Y
                    if (pt.originalX < minOriginalX)
                        minOriginalX = pt.originalX;
                    if (pt.originalX > maxOriginalX)
                        maxOriginalX = pt.originalX;

                    if (pt.originalY < minOriginalY)
                        minOriginalY = pt.originalY;
                    if (pt.originalY > maxOriginalY)
                        maxOriginalY = pt.originalY;

                    if (pt.elev < minElev)
                        minElev = pt.elev;
                    if (pt.elev > maxElev)
                        maxElev = pt.elev;
                }
            }
        }

        if (lidarPoints.Count > 0) {
            hasBounds = true;
        }

        Debug.Log($"Successfully mapped {lidarPoints.Count} Lidar points to Unity local space.");
    }

    // --- HELPER METHOD: Original Coordinates -> Unity Space ---
    private Vector3 MapOriginalToUnity(double origX,double origY,double elevation) {
        double shiftedX = origX + shiftX;
        double shiftedY = origY + shiftY;

        float uX = (float)shiftedX;
        float uY = (float)elevation;
        float uZ = (float)shiftedY;

        if (swapYandZ) {
            uY = (float)elevation;
            uZ = (float)shiftedY;
        } else {
            uY = (float)shiftedY;
            uZ = (float)elevation;
        }

        if (invertX)
            uX = -uX;
        if (invertY)
            uY = -uY;
        if (invertZ)
            uZ = -uZ;

        return new Vector3(uX,uY,uZ);
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Ray standardRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 overrideDirection = standardRay.direction;

            switch (raycastDirection) {
                case RaycastAxis.PositiveX:
                overrideDirection = Vector3.right;
                break;
                case RaycastAxis.NegativeX:
                overrideDirection = Vector3.left;
                break;
                case RaycastAxis.PositiveY:
                overrideDirection = Vector3.up;
                break;
                case RaycastAxis.NegativeY:
                overrideDirection = Vector3.down;
                break;
                case RaycastAxis.PositiveZ:
                overrideDirection = Vector3.forward;
                break;
                case RaycastAxis.NegativeZ:
                overrideDirection = Vector3.back;
                break;
            }

            Ray customRay = new Ray(standardRay.origin,overrideDirection);

            if (Physics.Raycast(customRay,out RaycastHit hit)) {
                if (hit.collider == meshCollider) {
                    FindAndLogExactVertex(hit);
                }
            }
        }
    }

    void FindAndLogExactVertex(RaycastHit hit) {
        Vector3 localHitPoint = hit.transform.InverseTransformPoint(hit.point);

        // --- STRICT BOUNDS CHECK (Based on Original Space) ---
        if (hasBounds) {
            // Reverse-map the Unity local point back into Original GPS X/Y space
            float tempX = localHitPoint.x;
            float tempY = localHitPoint.y;
            float tempZ = localHitPoint.z;

            // Reverse inversions
            if (invertX)
                tempX = -tempX;
            if (invertY)
                tempY = -tempY;
            if (invertZ)
                tempZ = -tempZ;

            double clickedOrigX, clickedOrigY;

            // Reverse swaps and shifts
            if (swapYandZ) {
                clickedOrigX = tempX - shiftX;
                clickedOrigY = tempZ - shiftY;
            } else {
                clickedOrigX = tempX - shiftX;
                clickedOrigY = tempY - shiftY;
            }

            // EXACT 2D Boundary Check: Is the click strictly inside the CSV limits?
            if (clickedOrigX < minOriginalX || clickedOrigX > maxOriginalX ||
                clickedOrigY < minOriginalY || clickedOrigY > maxOriginalY) {
                Debug.LogWarning("<color=orange><b>Click outside CSV Data Region ignored.</b></color>");
                return;
            }

            // Draw a yellow debug line from the camera to valid hits
            Debug.DrawLine(Camera.main.transform.position,hit.point,Color.yellow,3f);
        }

        Mesh mesh = meshCollider.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        int hitTriangleIndex = hit.triangleIndex * 3;
        Vector3 v0 = vertices[triangles[hitTriangleIndex]];
        Vector3 v1 = vertices[triangles[hitTriangleIndex + 1]];
        Vector3 v2 = vertices[triangles[hitTriangleIndex + 2]];

        Vector3 exactLocalVertex = v0;
        float minDist = Vector3.SqrMagnitude(localHitPoint - v0);

        float dist1 = Vector3.SqrMagnitude(localHitPoint - v1);
        if (dist1 < minDist) { minDist = dist1; exactLocalVertex = v1; }

        float dist2 = Vector3.SqrMagnitude(localHitPoint - v2);
        if (dist2 < minDist) { minDist = dist2; exactLocalVertex = v2; }

        int bestMatchIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0;i < lidarPoints.Count;i++) {
            float dist = (lidarPoints[i].expectedLocalPos - exactLocalVertex).sqrMagnitude;
            if (dist < closestDistance) {
                closestDistance = dist;
                bestMatchIndex = i;
            }
        }

        if (bestMatchIndex != -1) {
            LidarPoint bestMatch = lidarPoints[bestMatchIndex];

            string clipBoardText = $"{bestMatch.latitude},{bestMatch.longitude}";
            GUIUtility.systemCopyBuffer = clipBoardText;

            Debug.Log($"<color=#00FFFF><b>Point Copied to Clipboard!</b></color>\n" +
                      $"<b>LAT/LON:</b> {clipBoardText}\n" +
                      $"<b>X:</b> {bestMatch.originalX}\n" +
                      $"<b>Y:</b> {bestMatch.originalY}\n" +
                      $"<b>ELEV:</b> {bestMatch.elev}\n" +
                      $"<b>CLASS:</b> {bestMatch.pointClass}");

            Vector3 worldPos = hit.transform.TransformPoint(bestMatch.expectedLocalPos);
            Debug.DrawLine(Camera.main.transform.position,worldPos,Color.red,3f);
        }
    }

    void OnDrawGizmos() {
        if (drawDataBoundsGizmo && hasBounds) {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;

            // Generate the 4 corners at the base elevation
            Vector3 p00 = MapOriginalToUnity(minOriginalX,minOriginalY,minElev);
            Vector3 p10 = MapOriginalToUnity(maxOriginalX,minOriginalY,minElev);
            Vector3 p01 = MapOriginalToUnity(minOriginalX,maxOriginalY,minElev);
            Vector3 p11 = MapOriginalToUnity(maxOriginalX,maxOriginalY,minElev);

            // Generate the 4 corners at the max elevation to create a full 3D box
            Vector3 t00 = MapOriginalToUnity(minOriginalX,minOriginalY,maxElev);
            Vector3 t10 = MapOriginalToUnity(maxOriginalX,minOriginalY,maxElev);
            Vector3 t01 = MapOriginalToUnity(minOriginalX,maxOriginalY,maxElev);
            Vector3 t11 = MapOriginalToUnity(maxOriginalX,maxOriginalY,maxElev);

            // Draw Bottom Square
            Gizmos.DrawLine(p00,p10);
            Gizmos.DrawLine(p10,p11);
            Gizmos.DrawLine(p11,p01);
            Gizmos.DrawLine(p01,p00);

            // Draw Top Square
            Gizmos.DrawLine(t00,t10);
            Gizmos.DrawLine(t10,t11);
            Gizmos.DrawLine(t11,t01);
            Gizmos.DrawLine(t01,t00);

            // Draw Vertical Pillars
            Gizmos.DrawLine(p00,t00);
            Gizmos.DrawLine(p10,t10);
            Gizmos.DrawLine(p01,t01);
            Gizmos.DrawLine(p11,t11);

#if UNITY_EDITOR
            // Add custom labels showing the Grid corners matching your image
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;

            Handles.matrix = transform.localToWorldMatrix;
            Handles.Label(p00,"(0,0) Min X,Y",labelStyle);
            Handles.Label(p10,"(1,0) Max X",labelStyle);
            Handles.Label(p01,"(0,1) Max Y",labelStyle);
            Handles.Label(p11,"(1,1) Max X,Y",labelStyle);
#endif
        }
    }
}