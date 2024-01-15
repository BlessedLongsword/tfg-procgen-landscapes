using System.Numerics;
using Accord.Math;
using Accord.Math.Transforms;
using System;

using static Utils;
using static HeightMapInitialiser;
using UnityEngine;
using GD.MinMaxSlider;
using static TerrainTexturing;


public static class TerrainAlgorithms
{

    public enum Algorithm
    {
        MidpointDisplacement,
        DiamondSquares,
        FastFourierTransform
    };

    public class AlgorithmParameters
    {
        public int size;
        public float initialAltitudes;
        public float amplitude;
        public float maxHeight;
        public float minHeight;
        public float roughness;
        public float minRandomRange;
        public float maxRandomRange;
        public float[][] presetSides;
        public int[] sides;

        public AlgorithmParameters(int N = 5, float initialAltitudes = 0, float amplitude = 100, float maxHeight = 300, float minHeight = -100, float roughness = 0.5f,
                                    float minRandomRange = -1.0f, float maxRandomRange = 1.0f, float[][] presetSides = null, int[] sides = null)
        {
            this.size = (int)Math.Pow(2, N) + 1;
            this.initialAltitudes = initialAltitudes;
            this.amplitude = amplitude;
            this.maxHeight = maxHeight;
            this.minHeight = minHeight;
            this.roughness = roughness;
            this.minRandomRange = minRandomRange;
            this.maxRandomRange = maxRandomRange;
            this.presetSides = presetSides;
            this.sides = sides;
        }
    }

    [System.Serializable]
    public class Biome
    {
        public string name = "Biome";
        [Header("Textures")]
        public string[] terrainLayerNames = new string[] { "Coast", "Ground", "Rock", "Snow" };
        public SplatHeights[] splatHeights =
        {
            new SplatHeights(0, 0.0f, 1, 0f, false, new UnityEngine.Vector2(0.0f, 0.1f)),
            new SplatHeights(1, 0.1f, 3, 0.1f, true, new UnityEngine.Vector2(0.0f, 26.0f)),
            new SplatHeights(2, 0.1f, 3, 0.1f, true, new UnityEngine.Vector2(20.0f, 100.0f)),
            new SplatHeights(3, 0.5f, 3, 0.1f, false, new UnityEngine.Vector2(0.5f, 0.5f))
        };

        [Header("Plants")]
        public PlantGameObject[] plants = new PlantGameObject[0];
        [Range(0.0f, 1.0f)] public float density = 0.5f;
        [Range(0.0f, 10.0f)] public float isolationRadius = 1.0f;
        [Range(0.0f, 100.0f)] public float maxSteepness = 0.0f;
        [Range(0.0f, 100.0f)] public float maxHeight = 100.0f;

        [Header("Parameters")]
        [MinMaxSlider(0.0f, 5.0f)] public UnityEngine.Vector2 initialAltitudes = new(0.0f, 0.0f);
        [MinMaxSlider(0.0f, 5.0f)] public UnityEngine.Vector2 amplitude = new(0.0f, 0.0f);
        [MinMaxSlider(0.0f, 1.0f)] public UnityEngine.Vector2 roughness = new(0.0f, 0.0f);
        [MinMaxSlider(-1.0f, 1.0f)] public UnityEngine.Vector2 minRandomRange = new(-1.0f, 0.0f);
        [MinMaxSlider(-1.0f, 1.0f)] public UnityEngine.Vector2 maxRandomRange = new(0.0f, 1.0f);
        //[MinMaxSlider(-100.0f, 100.0f)] public UnityEngine.Vector2 height = new(0.0f, 100.0f);

        public Biome(string name)
        {
            this.name = name;
        }

        public Biome(string name, float minAmplitude, float maxAmplitude, float minRoughness, float maxRoughness,
                    float minInitialAltitudes, float maxInitialAltitudes, float minMinRandomRange = -1.0f,
                    float maxMinRandomRange = 0.0f, float minMaxRandomRange = 0.0f, float maxMaxRandomRange = 1.0f,
                    float minHeight = 0.0f, float maxHeight = 100.0f)
        {
            this.name = name;
            this.initialAltitudes = new UnityEngine.Vector2(minInitialAltitudes, maxInitialAltitudes);
            this.amplitude = new UnityEngine.Vector2(minAmplitude, maxAmplitude);
            this.roughness = new UnityEngine.Vector2(minRoughness, maxRoughness);
            this.minRandomRange = new UnityEngine.Vector2(minMinRandomRange, maxMinRandomRange);
            this.maxRandomRange = new UnityEngine.Vector2(minMaxRandomRange, maxMaxRandomRange);
            //this.height = new UnityEngine.Vector2(minHeight, maxHeight);
        }

        public AlgorithmParameters GenerateParameters(int N, float minTerrainHeight, float maxTerrainHeight)
        {
            float initialAltitudes = UnityEngine.Random.Range(this.initialAltitudes.x, this.initialAltitudes.y);
            float amplitude = UnityEngine.Random.Range(this.amplitude.x, this.amplitude.y);
            float roughness = UnityEngine.Random.Range(this.roughness.x, this.roughness.y);
            float minRandomRange = UnityEngine.Random.Range(this.minRandomRange.x, this.minRandomRange.y);
            float maxRandomRange = UnityEngine.Random.Range(this.maxRandomRange.x, this.maxRandomRange.y);
            return new AlgorithmParameters(N: N, initialAltitudes: initialAltitudes, amplitude: amplitude, roughness: roughness,
                                            minRandomRange: minRandomRange, maxRandomRange: maxRandomRange,
                                            minHeight: minTerrainHeight, maxHeight: maxTerrainHeight);
        }
    }

    [System.Serializable]
    public class PlantGameObject
    {
        public GameObject plant;
        [Range(0.0f, 1.0f)] public float spawnProbability = 0.5f;
        public bool aquatic = false;
    }

    public static float[,] MidpointDisplacement(AlgorithmParameters parameters)
    {
        float[,] heightMap;
        bool[,] visited = new bool[parameters.size, parameters.size];
        heightMap = InitializeCorners(parameters, visited);

        int x1 = 0;
        int y1 = 0;
        int x2 = parameters.size - 1;
        int y2 = parameters.size - 1;
        float scale = parameters.size * parameters.roughness;

        Displace(heightMap, x1, y1, x2, y2, parameters, scale, visited);

        return heightMap;
    }

    public static void Displace(float[,] heightMap, int x1, int y1, int x2, int y2, AlgorithmParameters parameters, float scale, bool[,] visited)
    {
        if (x2 - x1 <= 1 || y2 - y1 <= 1)
            return;

        int mx = (x1 + x2) / 2;
        int my = (y1 + y2) / 2;

        float lAvg = (heightMap[x1, y1] + heightMap[x1, y2]) / 2;
        float tAvg = (heightMap[x1, y1] + heightMap[x2, y1]) / 2;
        float rAvg = (heightMap[x2, y1] + heightMap[x2, y2]) / 2;
        float bAvg = (heightMap[x2, y2] + heightMap[x1, y2]) / 2;

        if (!visited[x1, my])
        {
            visited[x1, my] = true;
            heightMap[x1, my] = lAvg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
            heightMap[x1, my] = Mathf.Clamp(heightMap[x1, my], parameters.minHeight, parameters.maxHeight);
        }
        if (!visited[mx, y1])
        {
            visited[mx, y1] = true;
            heightMap[mx, y1] = tAvg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
            heightMap[mx, y1] = Mathf.Clamp(heightMap[mx, y1], parameters.minHeight, parameters.maxHeight);
        }
        if (!visited[x2, my])
        {
            visited[x2, my] = true;
            heightMap[x2, my] = rAvg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
            heightMap[x2, my] = Mathf.Clamp(heightMap[x2, my], parameters.minHeight, parameters.maxHeight);
        }
        if (!visited[mx, y2])
        {
            visited[mx, y2] = true;
            heightMap[mx, y2] = bAvg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
            heightMap[mx, y2] = Mathf.Clamp(heightMap[mx, y2], parameters.minHeight, parameters.maxHeight);
        }

        float avg = (heightMap[x1, my] + heightMap[x2, my] + heightMap[mx, y1] + heightMap[mx, y2] +
                        heightMap[x1, y1] + heightMap[x1, y2] + heightMap[x2, y1] + heightMap[x2, y2]) / 8;

        if (!visited[mx, my])
        {
            heightMap[mx, my] = avg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
            heightMap[mx, my] = Mathf.Clamp(heightMap[mx, my], parameters.minHeight, parameters.maxHeight);
        }

        Displace(heightMap, x1, y1, mx, my, parameters, scale * parameters.roughness, visited);
        Displace(heightMap, mx, y1, x2, my, parameters, scale * parameters.roughness, visited);
        Displace(heightMap, x1, my, mx, y2, parameters, scale * parameters.roughness, visited);
        Displace(heightMap, mx, my, x2, y2, parameters, scale * parameters.roughness, visited);

    }
    public static float[,] DiamondSquares(AlgorithmParameters parameters)
    {
        float[,] heightMap;
        bool[,] visited = new bool[parameters.size, parameters.size];
        heightMap = InitializeCorners(parameters, visited);
        float scale = parameters.roughness * parameters.size;

        for (int squareSize = parameters.size - 1; squareSize > 1; squareSize /= 2)
        {
            int x, z;
            int mid = squareSize / 2;
            float avg;

            for (x = 0; x < parameters.size - 1; x += squareSize)
            {
                for (z = 0; z < parameters.size - 1; z += squareSize)
                {

                    if (visited[x + mid, z + mid])
                        continue;

                    avg = (heightMap[x, z] + heightMap[x + squareSize, z] +
                            heightMap[x + squareSize, z + squareSize] + heightMap[x, z + squareSize]) / 4;
                    heightMap[x + mid, z + mid] = avg + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
                    heightMap[x + mid, z + mid] = Mathf.Clamp(heightMap[x + mid, z + mid], parameters.minHeight, parameters.maxHeight);
                    visited[x + mid, z + mid] = true;
                }
            }

            for (x = 0; x < parameters.size; x += mid)
            {
                for (z = (x / mid % 2 == 0) ? mid : 0; z < parameters.size; z += squareSize)
                {

                    if (visited[x, z])
                        continue;

                    float a = 0, b = 0, c = 0, d = 0;
                    int count = 0;

                    if (x >= mid)
                    {
                        a = heightMap[x - mid, z];
                        count++;
                    }

                    if (z >= mid)
                    {
                        b = heightMap[x, z - mid];
                        count++;
                    }

                    if (x + mid < parameters.size - 1)
                    {
                        c = heightMap[x + mid, z];
                        count++;
                    }

                    if (z + mid < parameters.size - 1)
                    {
                        d = heightMap[x, z + mid];
                        count++;
                    }

                    heightMap[x, z] = ((a + b + c + d) / count) + GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.amplitude * scale;
                    heightMap[x, z] = Mathf.Clamp(heightMap[x, z], parameters.minHeight, parameters.maxHeight);
                    visited[x, z] = true;

                }
            }

            scale *= parameters.roughness;
        }
        return heightMap;
    }

    public static float[,] FastFourierTransform(int size, float amplitude, float roughness, float roughnessFactor)
    {
        float[,] heightMap = InitializeHeightMapWithWhiteNoise(size, amplitude * size);
        FFT2D fft = new FFT2D(size);
        Complex[][] complexHeightMap = ToComplex(heightMap, size);
        FourierTransform2.FFT2(complexHeightMap, FourierTransform.Direction.Forward);
        //ToFloat(complexHeightMap, heightMap, size);
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                double frequency = Math.Sqrt(x * x + z * z);
                frequency = (frequency == 0) ? 1 : frequency;
                complexHeightMap[x][z] /= Math.Pow(frequency, roughnessFactor * (1 - roughness));
            }
        }
        //ToFloat(complexHeightMap, heightMap, size);
        FourierTransform2.FFT2(complexHeightMap, FourierTransform.Direction.Backward);
        ToFloat(complexHeightMap, heightMap, size);
        return heightMap;
    }
}
