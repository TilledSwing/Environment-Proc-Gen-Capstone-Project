using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MarchingCubes))]
[CanEditMultipleObjects]
public class NoiseEditor : Editor
{

    MarchingCubes marchingCubes;

    private void OnEnable()
    {
        marchingCubes = (MarchingCubes)target; // Cast the target to your script type

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // base.OnInspectorGUI();

        marchingCubes.seed = EditorGUILayout.IntField("Terrain Seed", marchingCubes.seed);
        marchingCubes.width = EditorGUILayout.IntField("Terrain width", marchingCubes.width);
        marchingCubes.height = EditorGUILayout.IntField("Terrain height", marchingCubes.height);
        marchingCubes.noiseScale = EditorGUILayout.FloatField("Noise Scale", marchingCubes.noiseScale);
        marchingCubes.isolevel = EditorGUILayout.FloatField("Isolevel", marchingCubes.isolevel);
        marchingCubes.lerp = GUILayout.Toggle(marchingCubes.lerp, "Lerp");

        if (GUILayout.Button("Update"))
        {
            marchingCubes.UpdateMesh();
        }

    }
}
