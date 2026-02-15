using UnityEngine;

/// <summary>
/// Modular terrain helper script that modifies the terrain heightmap to 
/// create a smooth linear ramp down to 0 around all edges.
/// Useful for blending terrain into surrounding water planes or flat ground.
/// Attach to any GameObject with a Terrain component, set falloffDistance, and press
/// "Apply Edge Falloff" in the inspector.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainEdgeFalloff : MonoBehaviour
{
    [Tooltip("World-space distance (meters) from each edge where the falloff ramp begins.")]
    [SerializeField] float falloffDistance = 50f;

    [Tooltip("Controls the shape of the falloff. X axis: 0 = terrain edge, 1 = falloff boundary. Y axis: 0 = ground level, 1 = full height.")]
    [SerializeField] AnimationCurve falloffCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    /// <summary>
    /// Returns a copy of the current heightmap from the attached Terrain.
    /// Used by the editor to snapshot heights before destructive operations.
    /// </summary>
    public float[,] GetCurrentHeights()
    {
        TerrainData td = GetComponent<Terrain>().terrainData;
        int res = td.heightmapResolution;
        return td.GetHeights(0, 0, res, res);
    }

    /// <summary>
    /// Writes the given heightmap array back to the terrain and flushes.
    /// Used by the editor to restore a previously cached snapshot.
    /// </summary>
    public void SetHeights(float[,] heights)
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData.SetHeights(0, 0, heights);
        terrain.Flush();
    }

    /// <summary>
    /// Applies a height falloff shaped by falloffCurve from the falloff boundary down to 0
    /// at every terrain edge. Uses absolute reference heights from the boundary so that
    /// repeated applications produce the same result.
    /// </summary>
    public void ApplyFalloff()
    {
        if (falloffDistance <= 0f)
        {
            Debug.LogWarning("TerrainEdgeFalloff: falloffDistance must be greater than 0.");
            return;
        }

        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;
        int res = terrainData.heightmapResolution;

        float halfTerrainWidth = terrainData.size.x * 0.5f;
        if (falloffDistance > halfTerrainWidth)
        {
            Debug.LogWarning(
                $"TerrainEdgeFalloff: falloffDistance ({falloffDistance}) exceeds half the terrain width " +
                $"({halfTerrainWidth}). The entire terrain will be affected.");
        }

        float sampleSpacing = terrainData.size.x / (res - 1);
        float falloffSamples = falloffDistance / sampleSpacing;
        int boundaryIndex = Mathf.Min(Mathf.CeilToInt(falloffSamples), res - 1);

        float[,] original = terrainData.GetHeights(0, 0, res, res);
        float[,] heights = (float[,])original.Clone();

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float distToEdge = Mathf.Min(x, y, res - 1 - x, res - 1 - y);

                if (distToEdge >= falloffSamples)
                    continue;

                float t = distToEdge / falloffSamples;
                float curveFactor = falloffCurve.Evaluate(t);

                // Find the reference height at the falloff boundary for each nearby edge.
                // For corners where two edges overlap, take the minimum.
                float refHeight = float.MaxValue;

                if (x < falloffSamples)
                    refHeight = Mathf.Min(refHeight, original[y, boundaryIndex]);
                if (res - 1 - x < falloffSamples)
                    refHeight = Mathf.Min(refHeight, original[y, res - 1 - boundaryIndex]);
                if (y < falloffSamples)
                    refHeight = Mathf.Min(refHeight, original[boundaryIndex, x]);
                if (res - 1 - y < falloffSamples)
                    refHeight = Mathf.Min(refHeight, original[res - 1 - boundaryIndex, x]);

                heights[y, x] = refHeight * curveFactor;
            }
        }

        terrainData.SetHeights(0, 0, heights);
        terrain.Flush();
    }
}

#if UNITY_EDITOR
namespace TerrainEdgeFalloffEditorNS
{
    using UnityEditor;

    [CustomEditor(typeof(TerrainEdgeFalloff))]
    public class TerrainEdgeFalloffEditor : Editor
    {
        // Manual undo via cached snapshot. Unity's built-in Undo.RecordObject on TerrainData
        // serializes the entire asset (heightmap, splatmaps, trees, etc.) which crashes the editor.
        float[,] cachedHeights;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Edge Falloff"))
            {
                TerrainEdgeFalloff falloff = (TerrainEdgeFalloff)target;
                cachedHeights = falloff.GetCurrentHeights();
                falloff.ApplyFalloff();
            }

            EditorGUI.BeginDisabledGroup(cachedHeights == null);
            if (GUILayout.Button("Revert"))
            {
                TerrainEdgeFalloff falloff = (TerrainEdgeFalloff)target;
                falloff.SetHeights(cachedHeights);
                cachedHeights = null;
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
