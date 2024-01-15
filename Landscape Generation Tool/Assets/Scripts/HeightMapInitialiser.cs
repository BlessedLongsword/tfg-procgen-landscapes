using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TerrainAlgorithms;
using static Utils;

public class HeightMapInitialiser
{
    public static float[,] InitializeCorners(AlgorithmParameters parameters, bool[,] visited)
    {

        if (parameters.presetSides != null)
            return InitializeCornersWithPresetSides(parameters, visited);

        float[,] heightMap = new float[parameters.size, parameters.size];
        heightMap[0, 0] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
        heightMap[0, parameters.size - 1] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
        heightMap[parameters.size - 1, 0] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
        heightMap[parameters.size - 1, parameters.size - 1] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;

        visited[0, 0] = true;
        visited[0, parameters.size - 1] = true;
        visited[parameters.size - 1, 0] = true;
        visited[parameters.size - 1, parameters.size - 1] = true;
        return heightMap;
    }

    public static float[,] InitializeCornersWithPresetSides(AlgorithmParameters parameters, bool[,] visited)
    {
        float[,] heightMap = new float[parameters.size, parameters.size];
        foreach (int side in parameters.sides)
        {
            for (int j = 0; j < parameters.size; j++)
            {
                switch (side)
                {
                    case 0:
                        heightMap[0, j] = parameters.presetSides[side][j];
                        visited[0, j] = true;
                        break;
                    case 1:
                        heightMap[j, 0] = parameters.presetSides[side][j];
                        visited[j, 0] = true;
                        break;
                    case 2:
                        heightMap[parameters.size - 1, j] = parameters.presetSides[side][j];
                        visited[parameters.size - 1, j] = true;
                        break;
                    case 3:
                        heightMap[j, parameters.size - 1] = parameters.presetSides[side][j];
                        visited[j, parameters.size - 1] = true;
                        break;
                }
            }
        }

        if (!visited[0, 0])
        {
            heightMap[0, 0] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
            visited[0, 0] = true;
        }

        if (!visited[0, parameters.size - 1])
        {
            heightMap[0, parameters.size - 1] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
            visited[0, parameters.size - 1] = true;
        }

        if (!visited[parameters.size - 1, 0])
        {
            heightMap[parameters.size - 1, 0] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
            visited[parameters.size - 1, 0] = true;
        }

        if (!visited[parameters.size - 1, parameters.size - 1])
        {
            heightMap[parameters.size - 1, parameters.size - 1] = GetRandom(parameters.minRandomRange, parameters.maxRandomRange) * parameters.initialAltitudes * parameters.size;
            visited[parameters.size - 1, parameters.size - 1] = true;
        }
        return heightMap;
    }




    /* Generate white noise with mean 0 and set variance */
    public static float[,] InitializeHeightMapWithWhiteNoise(int size, float variance)
    {
        float[,] heightMap = new float[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                heightMap[x, z] = GetRandom(0.0f, 1.0f) * variance;
            }
        }
        return heightMap;
    }
}
