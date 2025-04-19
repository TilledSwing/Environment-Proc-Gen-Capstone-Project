#if (UNITY_EDITOR)

using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;


[CustomEditor(typeof(WaterPlaneGenerator))]
[CanEditMultipleObjects]
public class WaterEditor : Editor
{

    WaterPlaneGenerator waterGenerator;
    TerrainDensityData terrainDensityData;

    private void OnEnable()
    {
        waterGenerator = (WaterPlaneGenerator)target; // Cast the target to your script type
        terrainDensityData = waterGenerator.terrainDensityData;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();

        // Water
        terrainDensityData.waterLevel = EditorGUILayout.IntField("Water Level", terrainDensityData.waterLevel);
    
        if (GUILayout.Button("Update", GUILayout.Width(230)))
        {
            waterGenerator.UpdateMesh();
        }

    }
}

#endif
