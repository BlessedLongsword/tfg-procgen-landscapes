using System;
using System.Numerics;
using UnityEngine;

public static class Utils
{

    public static float GetRandom(float min, float max)
    {
        return UnityEngine.Random.Range(min, max);
    }

    public static Complex[][] ToComplex(float[,] input, int size)
    {
        var result = new Complex[size][];

        for (var i = 0; i < size; i++)
        {
            result[i] = new Complex[size];

            for (var j = 0; j < size; j++)
            {
                var pixel = new Complex(input[i, j], 0);

                result[i][j] = pixel;
            }
        }
        return result;
    }

    public static void ToFloat(Complex[][] input, float[,] output, int size)
    {
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                output[i, j] = (float)Math.Abs(input[i][j].Real);
            }
        }
    }


    public static float NormalizeHeightmap(float[,] heightMap, int size)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (heightMap[x, z] < min)
                {
                    min = heightMap[x, z];
                }
                if (heightMap[x, z] > max)
                {
                    max = heightMap[x, z];
                }
            }
        }
        float range = max - min;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                heightMap[x, z] = (heightMap[x, z] - min) / range;
            }
        }

        return range;
    }

    public static float NormalizeHeightmap(float[,] heightMap, int size, float minValue, float maxValue)
    {
        float range = maxValue - minValue;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                heightMap[x, z] = (heightMap[x, z] - minValue) / range;
            }
        }
        return range;
    }

    public static void Normalize(float[] v)
    {
        float sum = 0;
        for (int i = 0; i < v.Length; i++)
        {
            sum += v[i];
        }
        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= sum;
        }
    }

    public static Tuple<float, float> UpdateGlobalMinMax(float[,] heightMap, int size, float currentGlobalMinHeight, float currentGlobalMaxHeight)
    {
        float min = currentGlobalMinHeight, max = currentGlobalMaxHeight;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                min = (heightMap[x, z] < min) ? heightMap[x, z] : min;
                max = (heightMap[x, z] > max) ? heightMap[x, z] : max;
            }
        }
        return new Tuple<float, float>(min, max);
    }

    public static float Map(float value, float min1, float max1, float min2, float max2)
    {
        return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
    }
}
