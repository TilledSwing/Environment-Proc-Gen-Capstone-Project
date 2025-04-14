using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MarchingCubes))]
[CanEditMultipleObjects]
public class NoiseEditor : Editor
{

    MarchingCubes marchingCubes;

    // Noise Values
    private int selectedNoiseType = 0;
    private FastNoiseLite.NoiseType[] noiseTypeOptions = new FastNoiseLite.NoiseType[] {FastNoiseLite.NoiseType.OpenSimplex2,
                                                                                        FastNoiseLite.NoiseType.OpenSimplex2S,
                                                                                        FastNoiseLite.NoiseType.Cellular,
                                                                                        FastNoiseLite.NoiseType.Perlin,
                                                                                        FastNoiseLite.NoiseType.ValueCubic,
                                                                                        FastNoiseLite.NoiseType.Value};
    private string[] noiseTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2S", "Cellular", "Perlin", "ValueCubic", "Value"};

    private int selectedNoiseFractalType = 0;
    private FastNoiseLite.FractalType[] noiseFractalTypeOptions = new FastNoiseLite.FractalType[] {FastNoiseLite.FractalType.FBm,
                                                                                                   FastNoiseLite.FractalType.None,
                                                                                                   FastNoiseLite.FractalType.PingPong,
                                                                                                   FastNoiseLite.FractalType.Ridged};
    private string[] noiseFractalTypeOptionLabels = new string[] {"FBm", "None", "Ping Pong", "Ridged"};

    // Domain Warp Values
    private int selectedDomainWarpType = 0;
    private FastNoiseLite.DomainWarpType[] domainWarpTypeOptions = new FastNoiseLite.DomainWarpType[] {FastNoiseLite.DomainWarpType.OpenSimplex2,
                                                                                                       FastNoiseLite.DomainWarpType.OpenSimplex2Reduced,
                                                                                                       FastNoiseLite.DomainWarpType.BasicGrid};
    private string[] domainWarpTypeOptionLabels = new string[] {"OpenSimplex2", "OpenSimplex2Reduced", "Basic Grid"};

    private int selectedDomainWarpFractalType = 2;
    private FastNoiseLite.FractalType[] domainWarpFractalTypeOptions = new FastNoiseLite.FractalType[] {FastNoiseLite.FractalType.DomainWarpIndependent,
                                                                                                        FastNoiseLite.FractalType.DomainWarpProgressive,
                                                                                                        FastNoiseLite.FractalType.None};
    private string[] domainWarpFractalTypeOptionLabels = new string[] {"Domain Warp Independent", "Domain Warp Progressive", "None"};

    // Cellular Values
    private int selectedCellularDistanceFunction = 1;
    private FastNoiseLite.CellularDistanceFunction[] cellularDistanceFunctionOptions = new FastNoiseLite.CellularDistanceFunction[] {FastNoiseLite.CellularDistanceFunction.Euclidean,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.EuclideanSq,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.Hybrid,
                                                                                                                                     FastNoiseLite.CellularDistanceFunction.Manhattan};
    private string[] cellularDistanceFunctionOptionLabels = new string[] {"Euclidean", "EuclideanSq", "Hybrid", "Manhattan"};

    private int selectedCellularReturnType = 1;
    private FastNoiseLite.CellularReturnType[] cellularReturnTypeOptions = new FastNoiseLite.CellularReturnType[] {FastNoiseLite.CellularReturnType.CellValue,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Add,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Div,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Mul,
                                                                                                                   FastNoiseLite.CellularReturnType.Distance2Sub};
    private string[] cellularReturnTypeOptionLabels = new string[] {"Cell Value", "Distance", "Distance2", "Distance2Add", "Distance2Div", "Distance2Mul", "Distance2Sub"};
    // 2D or 3D Noise
    private int selectedNoiseDimension = 1;
    private NoiseDimension[] noiseDimensionOptions = new NoiseDimension[] {NoiseDimension._2D,
                                                                           NoiseDimension._3D};
    private string[] noiseDimensionOptionLabels = new string[] {"2D", "3D"};

    private void OnEnable()
    {
        marchingCubes = (MarchingCubes)target; // Cast the target to your script type

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // base.OnInspectorGUI();

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
        EditorGUILayout.LabelField("Noise Types", GUILayout.Width(230));
        selectedNoiseDimension = EditorGUILayout.Popup(selectedNoiseDimension, noiseDimensionOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.noiseDimension = noiseDimensionOptions[selectedNoiseDimension];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Noise Types", GUILayout.Width(230));
        selectedNoiseType = EditorGUILayout.Popup(selectedNoiseType, noiseTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.noiseType = noiseTypeOptions[selectedNoiseType];
        marchingCubes.noiseSeed = EditorGUILayout.IntField("Terrain Seed", marchingCubes.noiseSeed);
        marchingCubes.noiseFrequency = EditorGUILayout.FloatField("Noise Frequency", marchingCubes.noiseFrequency);
        marchingCubes.width = EditorGUILayout.IntField("Terrain width", marchingCubes.width);
        marchingCubes.height = EditorGUILayout.IntField("Terrain height", marchingCubes.height);
        marchingCubes.noiseScale = EditorGUILayout.FloatField("Noise Scale", marchingCubes.noiseScale);
        marchingCubes.isolevel = EditorGUILayout.FloatField("Isolevel", marchingCubes.isolevel);
        marchingCubes.lerp = GUILayout.Toggle(marchingCubes.lerp, "Lerp");
        // Domain Warp Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Domain Warp Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        marchingCubes.domainWarpToggle = GUILayout.Toggle(marchingCubes.domainWarpToggle, "Domain Warp");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Types", GUILayout.Width(230));
        selectedDomainWarpType = EditorGUILayout.Popup(selectedDomainWarpType, domainWarpTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.domainWarpType = domainWarpTypeOptions[selectedDomainWarpType];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Domain Warp Fractal Types", GUILayout.Width(230));
        selectedDomainWarpFractalType = EditorGUILayout.Popup(selectedDomainWarpFractalType, domainWarpFractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.domainWarpFractalType = domainWarpFractalTypeOptions[selectedDomainWarpFractalType];
        marchingCubes.domainWarpAmplitude = EditorGUILayout.FloatField("Domain Warp Amplitude", marchingCubes.domainWarpAmplitude);
        marchingCubes.domainWarpSeed = EditorGUILayout.IntField("Domain Warp Seed", marchingCubes.domainWarpSeed);
        marchingCubes.domainWarpFrequency = EditorGUILayout.FloatField("Domain Warp Frequency", marchingCubes.domainWarpFrequency);
        marchingCubes.domainWarpFractalOctaves = EditorGUILayout.IntField("Domain Warp Fractal Octaves", marchingCubes.domainWarpFractalOctaves);
        marchingCubes.domainWarpFractalLacunarity = EditorGUILayout.FloatField("Domain Warp Fractal Lacunarity", marchingCubes.domainWarpFractalLacunarity);
        marchingCubes.domainWarpFractalGain = EditorGUILayout.FloatField("Domain Warp Fractal Gain", marchingCubes.domainWarpFractalGain);
        // Fractal Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fractal Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Fractal Types", GUILayout.Width(230));
        selectedNoiseFractalType = EditorGUILayout.Popup(selectedNoiseFractalType, noiseFractalTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.noiseFractalType = noiseFractalTypeOptions[selectedNoiseFractalType];
        marchingCubes.noiseFractalOctaves = EditorGUILayout.IntField("Fractal Octaves", marchingCubes.noiseFractalOctaves);
        marchingCubes.noiseFractalLacunarity = EditorGUILayout.FloatField("Fractal Lacunarity", marchingCubes.noiseFractalLacunarity);
        marchingCubes.noiseFractalGain = EditorGUILayout.FloatField("Fractal Gain", marchingCubes.noiseFractalGain);
        marchingCubes.fractalWeightedStrength = EditorGUILayout.FloatField("Fractal Weighted Strength", marchingCubes.fractalWeightedStrength);
        // Cellular(Voronoi) Noise Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cellular(Voronoi) Settings", sectionHeaderStyle);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cellular(Voronoi) Distance Functions", GUILayout.Width(230));
        selectedCellularDistanceFunction = EditorGUILayout.Popup(selectedCellularDistanceFunction, cellularDistanceFunctionOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.cellularDistanceFunction = cellularDistanceFunctionOptions[selectedCellularDistanceFunction];
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cellular(Voronoi) Return Type", GUILayout.Width(230));
        selectedCellularReturnType = EditorGUILayout.Popup(selectedCellularReturnType, cellularReturnTypeOptionLabels);
        EditorGUILayout.EndHorizontal();
        marchingCubes.cellularReturnType = cellularReturnTypeOptions[selectedCellularReturnType];
        marchingCubes.cellularJitter = EditorGUILayout.FloatField("Jitter", marchingCubes.cellularJitter);

        if (GUILayout.Button("Update", GUILayout.Width(230)))
        {
            marchingCubes.UpdateMesh();
        }

    }
}
