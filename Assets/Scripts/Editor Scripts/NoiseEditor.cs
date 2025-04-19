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
    private FastNoiseLite.NoiseType[] noiseTypeOptions = new FastNoiseLite.NoiseType[] {FastNoiseLite.NoiseType.OpenSimplex2,
                                                                                        FastNoiseLite.NoiseType.OpenSimplex2S,
                                                                                        FastNoiseLite.NoiseType.Cellular,
                                                                                        FastNoiseLite.NoiseType.Perlin,
                                                                                        FastNoiseLite.NoiseType.ValueCubic,
                                                                                        FastNoiseLite.NoiseType.Value};
    private string[] noiseTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2S", "Cellular", "Perlin", "ValueCubic", "Value"};

    private FastNoiseLite.FractalType[] noiseFractalTypeOptions = new FastNoiseLite.FractalType[] {FastNoiseLite.FractalType.FBm,
                                                                                                   FastNoiseLite.FractalType.None,
                                                                                                   FastNoiseLite.FractalType.PingPong,
                                                                                                   FastNoiseLite.FractalType.Ridged};
    private string[] noiseFractalTypeOptionLabels = new string[] {"FBm", "None", "Ping Pong", "Ridged"};

    // Domain Warp Values
    private FastNoiseLite.DomainWarpType[] domainWarpTypeOptions = new FastNoiseLite.DomainWarpType[] {FastNoiseLite.DomainWarpType.OpenSimplex2,
                                                                                                       FastNoiseLite.DomainWarpType.OpenSimplex2Reduced,
                                                                                                       FastNoiseLite.DomainWarpType.BasicGrid};
    private string[] domainWarpTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2Reduced", "Basic Grid"};

    private FastNoiseLite.FractalType[] domainWarpFractalTypeOptions = new FastNoiseLite.FractalType[] {FastNoiseLite.FractalType.DomainWarpIndependent,
                                                                                                        FastNoiseLite.FractalType.DomainWarpProgressive,
                                                                                                        FastNoiseLite.FractalType.None};
    private string[] domainWarpFractalTypeOptionLabels = new string[] {"Domain Warp Independent", "Domain Warp Progressive", "None"};

    // Cellular Values
    private FastNoiseLite.CellularDistanceFunction[] cellularDistanceFunctionOptions = new FastNoiseLite.CellularDistanceFunction[] {FastNoiseLite.CellularDistanceFunction.Euclidean,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.EuclideanSq,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.Hybrid,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.Manhattan};
    private string[] cellularDistanceFunctionOptionLabels = new string[] {"Euclidean", "EuclideanSq", "Hybrid", "Manhattan"};

    private FastNoiseLite.CellularReturnType[] cellularReturnTypeOptions = new FastNoiseLite.CellularReturnType[] {FastNoiseLite.CellularReturnType.CellValue,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Add,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Div,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Mul,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Sub};
    private string[] cellularReturnTypeOptionLabels = new string[] {"Cell Value", "Distance", "Distance2", "Distance2Add", "Distance2Div", "Distance2Mul", "Distance2Sub"};
    // 2D or 3D Noise
    private TerrainDensityData.NoiseDimension[] noiseDimensionOptions = new TerrainDensityData.NoiseDimension[] {TerrainDensityData.NoiseDimension._2D,
                                                                           TerrainDensityData.NoiseDimension._3D};
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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Dimensions", GUILayout.Width(230));
        terrainDensityData.selectedNoiseDimension = EditorGUILayout.Popup(terrainDensityData.selectedNoiseDimension, noiseDimensionOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.noiseDimension = noiseDimensionOptions[terrainDensityData.selectedNoiseDimension];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Types", GUILayout.Width(230));
        terrainDensityData.selectedNoiseType = EditorGUILayout.Popup(terrainDensityData.selectedNoiseType, noiseTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.noiseType = noiseTypeOptions[terrainDensityData.selectedNoiseType];
        terrainDensityData.noiseSeed = EditorGUILayout.IntField("Terrain Seed", terrainDensityData.noiseSeed);
        terrainDensityData.noiseFrequency = EditorGUILayout.FloatField("Noise Frequency", terrainDensityData.noiseFrequency);
        terrainDensityData.width = EditorGUILayout.IntField("Terrain width", terrainDensityData.width);
        terrainDensityData.height = EditorGUILayout.IntField("Terrain height", terrainDensityData.height);
        terrainDensityData.noiseScale = EditorGUILayout.FloatField("Noise Scale", terrainDensityData.noiseScale);
        terrainDensityData.isolevel = EditorGUILayout.FloatField("Isolevel", terrainDensityData.isolevel);
        terrainDensityData.lerp = GUILayout.Toggle(terrainDensityData.lerp, "Lerp");
        // Domain Warp Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Domain Warp Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        terrainDensityData.domainWarpToggle = GUILayout.Toggle(terrainDensityData.domainWarpToggle, "Domain Warp");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Types", GUILayout.Width(230));
        terrainDensityData.selectedDomainWarpType = EditorGUILayout.Popup(terrainDensityData.selectedDomainWarpType, domainWarpTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.domainWarpType = domainWarpTypeOptions[terrainDensityData.selectedDomainWarpType];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Fractal Types", GUILayout.Width(230));
        terrainDensityData.selectedDomainWarpFractalType = EditorGUILayout.Popup(terrainDensityData.selectedDomainWarpFractalType, domainWarpFractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.domainWarpFractalType = domainWarpFractalTypeOptions[terrainDensityData.selectedDomainWarpFractalType];
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
        terrainDensityData.noiseFractalType = noiseFractalTypeOptions[terrainDensityData.selectedNoiseFractalType];
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
        terrainDensityData.cellularDistanceFunction = cellularDistanceFunctionOptions[terrainDensityData.selectedCellularDistanceFunction];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cellular(Voronoi) Return Type", GUILayout.Width(230));
        terrainDensityData.selectedCellularReturnType = EditorGUILayout.Popup(terrainDensityData.selectedCellularReturnType, cellularReturnTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        terrainDensityData.cellularReturnType = cellularReturnTypeOptions[terrainDensityData.selectedCellularReturnType];
        terrainDensityData.cellularJitter = EditorGUILayout.FloatField("Jitter", terrainDensityData.cellularJitter);
        // Water
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
