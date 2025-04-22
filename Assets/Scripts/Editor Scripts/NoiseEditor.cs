#if (UNITY_EDITOR)

using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;


[CustomEditor(typeof(MarchingCubes))]
[CanEditMultipleObjects]
public class NoiseEditor : Editor
{

    private MarchingCubes marchingCubes;
    private TerrainDensityData terrainDensityData;

    // Noise Values
    private string[] noiseTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2S", "Cellular", "Perlin", "ValueCubic", "Value"};

    private string[] noiseFractalTypeOptionLabels = new string[] {"FBm", "None", "Ping Pong", "Ridged"};

    // Domain Warp Values
    private string[] domainWarpTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2Reduced", "Basic Grid"};

    private string[] domainWarpFractalTypeOptionLabels = new string[] {"Domain Warp Independent", "Domain Warp Progressive", "None"};

    // Cellular Values
    private string[] cellularDistanceFunctionOptionLabels = new string[] {"Euclidean", "EuclideanSq", "Hybrid", "Manhattan"};

   
    private string[] cellularReturnTypeOptionLabels = new string[] {"Cell Value", "Distance", "Distance2", "Distance2Add", "Distance2Div", "Distance2Mul", "Distance2Sub"};

    // 2D or 3D Noise
    private string[] noiseDimensionOptionLabels = new string[] {"2D", "3D"};

    private void OnEnable()
    {
        marchingCubes = (MarchingCubes)target; // Cast the target to your script type
        terrainDensityData = marchingCubes.terrainDensityData;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();

        // Section header styling
        GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.label);
        sectionHeaderStyle.fontStyle = FontStyle.Bold;
        sectionHeaderStyle.fontSize = 16;
        // Large header styling
        GUIStyle largeHeaderStyle = new GUIStyle(EditorStyles.label);
        largeHeaderStyle.alignment = TextAnchor.MiddleCenter;
        largeHeaderStyle.fontStyle = FontStyle.Bold;
        largeHeaderStyle.fontSize = 20;
        // Main header
        EditorGUILayout.LabelField("Settings", largeHeaderStyle);
        // Noise Settings
        EditorGUILayout.LabelField("Noise Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        terrainDensityData.polygonizationVisualization = GUILayout.Toggle(terrainDensityData.polygonizationVisualization, "Visualize Terrain Generation");
        terrainDensityData.polygonizationVisualizationRate = EditorGUILayout.IntField("Visualization Speed", terrainDensityData.polygonizationVisualizationRate);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Dimensions", GUILayout.Width(230));
        terrainDensityData.selectedNoiseDimension = EditorGUILayout.Popup(terrainDensityData.selectedNoiseDimension, noiseDimensionOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.noiseDimension = terrainDensityData.noiseDimensionOptions[terrainDensityData.selectedNoiseDimension];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Types", GUILayout.Width(230));
        terrainDensityData.selectedNoiseType = EditorGUILayout.Popup(terrainDensityData.selectedNoiseType, noiseTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.noiseType = terrainDensityData.noiseTypeOptions[terrainDensityData.selectedNoiseType];
        terrainDensityData.noiseSeed = EditorGUILayout.IntField("Terrain Seed", terrainDensityData.noiseSeed);
        terrainDensityData.noiseFrequency = EditorGUILayout.FloatField("Noise Frequency", terrainDensityData.noiseFrequency);
        terrainDensityData.width = EditorGUILayout.IntField("Terrain width", terrainDensityData.width);
        terrainDensityData.height = EditorGUILayout.IntField("Terrain height", terrainDensityData.height);
        terrainDensityData.noiseScale = EditorGUILayout.FloatField("Noise Scale", terrainDensityData.noiseScale);
        terrainDensityData.isolevel = EditorGUILayout.FloatField("Isolevel", terrainDensityData.isolevel);
        terrainDensityData.lerp = GUILayout.Toggle(terrainDensityData.lerp, "Lerp");
        terrainDensityData.terracing = GUILayout.Toggle(terrainDensityData.terracing, "Terracing");
        terrainDensityData.terraceHeight = EditorGUILayout.IntField("Terrace Height", terrainDensityData.terraceHeight);
        // Domain Warp Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Domain Warp Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        terrainDensityData.domainWarpToggle = GUILayout.Toggle(terrainDensityData.domainWarpToggle, "Domain Warp");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Types", GUILayout.Width(230));
        terrainDensityData.selectedDomainWarpType = EditorGUILayout.Popup(terrainDensityData.selectedDomainWarpType, domainWarpTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.domainWarpType = terrainDensityData.domainWarpTypeOptions[terrainDensityData.selectedDomainWarpType];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Fractal Types", GUILayout.Width(230));
        terrainDensityData.selectedDomainWarpFractalType = EditorGUILayout.Popup(terrainDensityData.selectedDomainWarpFractalType, domainWarpFractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.domainWarpFractalType = terrainDensityData.domainWarpFractalTypeOptions[terrainDensityData.selectedDomainWarpFractalType];
        terrainDensityData.domainWarpAmplitude = EditorGUILayout.FloatField("Domain Warp Amplitude", terrainDensityData.domainWarpAmplitude);
        terrainDensityData.domainWarpSeed = EditorGUILayout.IntField("Domain Warp Seed", terrainDensityData.domainWarpSeed);
        terrainDensityData.domainWarpFrequency = EditorGUILayout.FloatField("Domain Warp Frequency", terrainDensityData.domainWarpFrequency);
        terrainDensityData.domainWarpFractalOctaves = EditorGUILayout.IntField("Domain Warp Fractal Octaves", terrainDensityData.domainWarpFractalOctaves);
        terrainDensityData.domainWarpFractalLacunarity = EditorGUILayout.FloatField("Domain Warp Fractal Lacunarity", terrainDensityData.domainWarpFractalLacunarity);
        terrainDensityData.domainWarpFractalGain = EditorGUILayout.FloatField("Domain Warp Fractal Gain", terrainDensityData.domainWarpFractalGain);
        // Fractal Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fractal Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Fractal Types", GUILayout.Width(230));
        terrainDensityData.selectedNoiseFractalType = EditorGUILayout.Popup(terrainDensityData.selectedNoiseFractalType, noiseFractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.noiseFractalType = terrainDensityData.noiseFractalTypeOptions[terrainDensityData.selectedNoiseFractalType];
        terrainDensityData.noiseFractalOctaves = EditorGUILayout.IntField("Fractal Octaves", terrainDensityData.noiseFractalOctaves);
        terrainDensityData.noiseFractalLacunarity = EditorGUILayout.FloatField("Fractal Lacunarity", terrainDensityData.noiseFractalLacunarity);
        terrainDensityData.noiseFractalGain = EditorGUILayout.FloatField("Fractal Gain", terrainDensityData.noiseFractalGain);
        terrainDensityData.fractalWeightedStrength = EditorGUILayout.FloatField("Fractal Weighted Strength", terrainDensityData.fractalWeightedStrength);
        // Cellular(Voronoi) Noise Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cellular(Voronoi) Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cellular(Voronoi) Distance Functions", GUILayout.Width(230));
        terrainDensityData.selectedCellularDistanceFunction = EditorGUILayout.Popup(terrainDensityData.selectedCellularDistanceFunction, cellularDistanceFunctionOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.cellularDistanceFunction = terrainDensityData.cellularDistanceFunctionOptions[terrainDensityData.selectedCellularDistanceFunction];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cellular(Voronoi) Return Type", GUILayout.Width(230));
        terrainDensityData.selectedCellularReturnType = EditorGUILayout.Popup(terrainDensityData.selectedCellularReturnType, cellularReturnTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.cellularReturnType = terrainDensityData.cellularReturnTypeOptions[terrainDensityData.selectedCellularReturnType];
        terrainDensityData.cellularJitter = EditorGUILayout.FloatField("Jitter", terrainDensityData.cellularJitter);
        // Water
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Water Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        terrainDensityData.waterLevel = EditorGUILayout.IntField("Water Level", terrainDensityData.waterLevel);

        if (GUILayout.Button("Reset To Default", GUILayout.Width(230)))
        {
            terrainDensityData.ResetToDefault();
            marchingCubes.UpdateMesh();
        }

    
        if (GUILayout.Button("Update", GUILayout.Width(230)))
        {
            marchingCubes.UpdateMesh();
        }

    }
}

#endif
