using UnityEngine;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Compass3D:MonoBehaviour {
    [Header("Data Source")]
    [Tooltip("Assign your lidardata.csv file here.")]
    public TextAsset csvDataFile;

    [Header("Export Shifts (Must match LidarDataMapper)")]
    public double shiftX = -725559.499;
    public double shiftY = -940542.892;

    [Header("Axis Mapping (Must match LidarDataMapper)")]
    public bool invertX = true;
    public bool invertY = false;
    public bool invertZ = false;
    public bool swapYandZ = false;

    [Header("Compass Visuals")]
    [Tooltip("Size of the marker spheres.")]
    public float pointerSize = 2f;
    [Tooltip("How high above the terrain the compass should hover.")]
    public float hoverHeight = 10f;
    [Tooltip("How far past the model's edges the compass arms should extend.")]
    public float padding = 5f;

    // Cached boundary points
    private Vector3 centerPt, northPt, southPt, eastPt, westPt;
    private bool hasData = false;
    private void OnEnable() {
        AppManager.instance.onDataProcessedEvent.AddListener(GenerateCompassFromData);
    }
    private void OnDisable() {
        AppManager.instance.onDataProcessedEvent.RemoveListener(GenerateCompassFromData);
    }
    //[ContextMenu("Generate Compass From CSV")]
    public void GenerateCompassFromData() {
        if (csvDataFile == null) {
            Debug.LogError("Please assign your CSV data file to the Compass script!");
            return;
        }

        double minOriginalX = double.MaxValue, maxOriginalX = double.MinValue;
        double minOriginalY = double.MaxValue, maxOriginalY = double.MinValue;
        double maxElev = double.MinValue;

        int count = 0;

        using (System.IO.StringReader reader = new System.IO.StringReader(csvDataFile.text)) {
            string line = reader.ReadLine(); // Skip header

            while ((line = reader.ReadLine()) != null) {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] cols = line.Split(',');

                if (cols.Length >= 3) {
                    double oX = double.Parse(cols[0],CultureInfo.InvariantCulture);
                    double oY = double.Parse(cols[1],CultureInfo.InvariantCulture);
                    double elev = double.Parse(cols[2],CultureInfo.InvariantCulture);

                    // Track the furthest edges of the Original GPS data
                    if (oX < minOriginalX)
                        minOriginalX = oX;
                    if (oX > maxOriginalX)
                        maxOriginalX = oX;

                    if (oY < minOriginalY)
                        minOriginalY = oY;
                    if (oY > maxOriginalY)
                        maxOriginalY = oY;

                    if (elev > maxElev)
                        maxElev = elev;

                    count++;
                }
            }
        }

        if (count > 0) {
            // Calculate exact center in Original GPS Space
            double centerX = (minOriginalX + maxOriginalX) / 2.0;
            double centerY = (minOriginalY + maxOriginalY) / 2.0;
            double displayElev = maxElev + hoverHeight;

            // Map standard Cardinal GPS points into Unity Local Space
            centerPt = MapOriginalToUnity(centerX,centerY,displayElev);
            northPt = MapOriginalToUnity(centerX,maxOriginalY,displayElev);
            southPt = MapOriginalToUnity(centerX,minOriginalY,displayElev);
            eastPt = MapOriginalToUnity(maxOriginalX,centerY,displayElev);
            westPt = MapOriginalToUnity(minOriginalX,centerY,displayElev);

            // Apply visual padding to push the points outward from the center
            northPt += (northPt - centerPt).normalized * padding;
            southPt += (southPt - centerPt).normalized * padding;
            eastPt += (eastPt - centerPt).normalized * padding;
            westPt += (westPt - centerPt).normalized * padding;

            hasData = true;
            Debug.Log($"Compass successfully aligned using {count} LiDAR points!");
        }
    }

    // --- HELPER METHOD: Original Coordinates -> Unity Space (Must exactly match LidarDataMapper) ---
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

    void OnDrawGizmos() {
        if (!hasData)
            return;

        // Convert the cached local positions to world space so the compass moves with the GameObject
        Vector3 wCenter = transform.TransformPoint(centerPt);
        Vector3 wNorth = transform.TransformPoint(northPt);
        Vector3 wSouth = transform.TransformPoint(southPt);
        Vector3 wEast = transform.TransformPoint(eastPt);
        Vector3 wWest = transform.TransformPoint(westPt);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(wCenter,pointerSize * 0.5f);

        // NORTH (Max Original Y)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(wCenter,wNorth);
        Gizmos.DrawSphere(wNorth,pointerSize);

        // SOUTH (Min Original Y)
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(wCenter,wSouth);
        Gizmos.DrawSphere(wSouth,pointerSize * 0.5f);

        // EAST (Max Original X)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wCenter,wEast);
        Gizmos.DrawSphere(wEast,pointerSize * 0.8f);

        // WEST (Min Original X)
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(wCenter,wWest);
        Gizmos.DrawSphere(wWest,pointerSize * 0.5f);

#if UNITY_EDITOR
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.yellow;
        labelStyle.fontSize = 16;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.LowerCenter;

        Vector3 textOffset = Vector3.up * (pointerSize + 0.5f);

        Handles.Label(wNorth + textOffset,"NORTH",labelStyle);
        Handles.Label(wSouth + textOffset,"SOUTH",labelStyle);
        Handles.Label(wEast + textOffset,"EAST",labelStyle);
        Handles.Label(wWest + textOffset,"WEST",labelStyle);
#endif
    }
}