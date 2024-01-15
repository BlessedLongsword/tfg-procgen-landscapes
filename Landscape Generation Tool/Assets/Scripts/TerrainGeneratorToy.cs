using System;
using UnityEngine;
using Random = UnityEngine.Random;
using GD.MinMaxSlider;

using static TerrainAlgorithms;
using static Utils;
using static TerrainTexturing;

public class TerrainGeneratorToy : MonoBehaviour
{

    [System.Serializable]
    public class SeedSetter
    {
        public bool useRandomSeed = true;
        public int seed = 0;
    }

    [System.Serializable]
    public class MainParameters
    {
        [Header("Generation")]
        public bool dynamicGenerator = false;
        [Header("Terrain Parameters")]
        public int N = 5;

        [Header("Texturing")]
        public SplatHeights[] splatHeights;

        [Header("Seed")]
        public SeedSetter seedSetter = new SeedSetter();

        [Header("Algorithm")]
        public Algorithm algorithm = Algorithm.MidpointDisplacement;

        public MainParameters()
        {
            splatHeights = new SplatHeights[4];
            splatHeights[0] = new SplatHeights(0, 1);
            splatHeights[1] = new SplatHeights(1, 3);
            splatHeights[2] = new SplatHeights(2, 3);
            splatHeights[3] = new SplatHeights(3, 3);
        }

    }

    [System.Serializable]
    public class MidpointDisplacementParameters
    {
        [Range(0.0f, 5.0f)] public float initialAltitudes = 0.0f;
        [Range(0.0f, 5.0f)] public float amplitude = 1f;
        [Range(0.0f, 1.0f)] public float roughness = 0.5f;
        [MinMaxSlider(-500.0f, 500.0f)] public UnityEngine.Vector2 terrainHeights = new(-500.0f, 500.0f);
        [MinMaxSlider(-1.0f, 1.0f)] public UnityEngine.Vector2 randomRange = new(-1.0f, 1.0f);
    }

    [System.Serializable]
    public class DiamondSquaresParameters
    {
        [Range(0.0f, 5.0f)] public float initialAltitudes = 0.0f;
        [Range(0.0f, 5.0f)] public float amplitude = 1f;
        [Range(0.0f, 1.0f)] public float roughness = 0.5f;
        [MinMaxSlider(-500.0f, 500.0f)] public UnityEngine.Vector2 terrainHeights = new(-500.0f, 500.0f);

        [MinMaxSlider(-1.0f, 1.0f)] public UnityEngine.Vector2 randomRange = new(-1.0f, 1.0f);
    }

    [System.Serializable]
    public class FastFourierTransformParameters
    {
        [Range(0.0f, 100.0f)] public float amplitude = 25.0f;
        [Range(0.0f, 1.0f)] public float roughness = 0.5f;
        [Range(1.0f, 16.0f)] public float roughnessFactor = 2.0f;
    }

    public MainParameters mainParameters = new MainParameters();

    [Header("Algorithm Parameters")]
    public MidpointDisplacementParameters midpointDisplacementParameters = new MidpointDisplacementParameters();
    public DiamondSquaresParameters diamondSquaresParameters = new DiamondSquaresParameters();
    public FastFourierTransformParameters fastFourierTransformParameters = new FastFourierTransformParameters();

    // Start is called before the first frame update
    void Start()
    {
        GenerateTerrain();
    }

    private void OnValidate()
    {
        if (mainParameters.dynamicGenerator)
            GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Random.InitState(mainParameters.seedSetter.useRandomSeed ? System.DateTime.Now.Millisecond : mainParameters.seedSetter.seed);
        float[,] heightMap;
        AlgorithmParameters midpointParameters = new AlgorithmParameters(N: mainParameters.N,
                                                                            initialAltitudes: midpointDisplacementParameters.initialAltitudes,
                                                                            amplitude: midpointDisplacementParameters.amplitude,
                                                                            roughness: midpointDisplacementParameters.roughness,
                                                                            maxHeight: midpointDisplacementParameters.terrainHeights.y,
                                                                            minHeight: midpointDisplacementParameters.terrainHeights.x,
                                                                            minRandomRange: midpointDisplacementParameters.randomRange.x,
                                                                            maxRandomRange: midpointDisplacementParameters.randomRange.y);
        AlgorithmParameters diamondParameters = new AlgorithmParameters(N: mainParameters.N,
                                                                            initialAltitudes: diamondSquaresParameters.initialAltitudes,
                                                                            amplitude: diamondSquaresParameters.amplitude,
                                                                            roughness: diamondSquaresParameters.roughness,
                                                                            maxHeight: diamondSquaresParameters.terrainHeights.y,
                                                                            minHeight: diamondSquaresParameters.terrainHeights.x,
                                                                            minRandomRange: diamondSquaresParameters.randomRange.x,
                                                                            maxRandomRange: diamondSquaresParameters.randomRange.y);
        float terrainHeight = 0f;
        int terrainSize = (int)Math.Pow(2, mainParameters.N) + 1;
        switch (mainParameters.algorithm)
        {
            case Algorithm.MidpointDisplacement:
                heightMap = MidpointDisplacement(midpointParameters);
                terrainHeight = NormalizeHeightmap(heightMap, terrainSize, midpointParameters.minHeight, midpointParameters.maxHeight);
                break;
            case Algorithm.DiamondSquares:
                heightMap = DiamondSquares(diamondParameters);
                terrainHeight = NormalizeHeightmap(heightMap, terrainSize, diamondParameters.minHeight, diamondParameters.maxHeight);
                break;
            case Algorithm.FastFourierTransform:
                heightMap = FastFourierTransform(
                                        terrainSize,
                                        fastFourierTransformParameters.amplitude,
                                        fastFourierTransformParameters.roughness,
                                        fastFourierTransformParameters.roughnessFactor
                                        );
                terrainHeight = NormalizeHeightmap(heightMap, terrainSize);
                break;
            default:
                heightMap = MidpointDisplacement(midpointParameters);
                break;
        }
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData.size = new UnityEngine.Vector3(terrainSize, terrainHeight, terrainSize);
        terrain.terrainData.heightmapResolution = terrainSize;
        terrain.terrainData.alphamapResolution = terrainSize;
        terrain.terrainData.baseMapResolution = terrainSize;
        terrain.terrainData.SetHeights(0, 0, heightMap);
        AssignSplatMap(terrain.terrainData, mainParameters.splatHeights);
    }
}