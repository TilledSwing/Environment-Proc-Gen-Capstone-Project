using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MarchingCubes))]
[CanEditMultipleObjects]
public class NoiseEditor : Editor
{

    MarchingCubes marchingCubes;
    private int selectedNoiseType = 0;
    private FastNoiseLite.NoiseType[] noiseTypeOptions = new FastNoiseLite.NoiseType[] {FastNoiseLite.NoiseType.OpenSimplex2,
                                                                                        FastNoiseLite.NoiseType.OpenSimplex2S,
                                                                                        FastNoiseLite.NoiseType.Cellular,
                                                                                        FastNoiseLite.NoiseType.Perlin,
                                                                                        FastNoiseLite.NoiseType.ValueCubic,
                                                                                        FastNoiseLite.NoiseType.Value};
    private string[] noiseTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2S", "Cellular", "Perlin", "ValueCubic", "Value"};

    private int selectedFractalType = 2;
    private FastNoiseLite.FractalType[] fractalTypeOptions = new FastNoiseLite.FractalType[] {FastNoiseLite.FractalType.DomainWarpIndependent,
                                                                                              FastNoiseLite.FractalType.DomainWarpProgressive,
                                                                                              FastNoiseLite.FractalType.FBm,
                                                                                              FastNoiseLite.FractalType.None,
                                                                                              FastNoiseLite.FractalType.PingPong,
                                                                                              FastNoiseLite.FractalType.Ridged};
    private string[] fractalTypeOptionLabels = new string[] {"Domain Warp Independent", "Domain Warp Progressive", "FBm", "None", "Ping Pong", "Ridged"};

    private void OnEnable()
    {
        marchingCubes = (MarchingCubes)target; // Cast the target to your script type

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Types", GUILayout.Width(230));
        selectedNoiseType = EditorGUILayout.Popup(selectedNoiseType, noiseTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.noiseType = noiseTypeOptions[selectedNoiseType];

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Fractal Types", GUILayout.Width(230));
        selectedFractalType = EditorGUILayout.Popup(selectedFractalType, fractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.fractalType = fractalTypeOptions[selectedFractalType];

        marchingCubes.seed = EditorGUILayout.IntField("Terrain Seed", marchingCubes.seed);
        marchingCubes.fractalOctaves = EditorGUILayout.IntField("Fractal Octaves", marchingCubes.fractalOctaves);
        marchingCubes.fractalLacunarity = EditorGUILayout.FloatField("Fractal Lacunarity", marchingCubes.fractalLacunarity);
        marchingCubes.fractalGain = EditorGUILayout.FloatField("Fractal Gain", marchingCubes.fractalGain);
        marchingCubes.frequency = EditorGUILayout.FloatField("Frequency", marchingCubes.frequency);
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
