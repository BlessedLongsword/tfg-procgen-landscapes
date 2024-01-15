using System;
using GD.MinMaxSlider;
using UnityEngine;

using static Utils;

public class TerrainTexturing
{

    [System.Serializable]
    public class SplatHeights
    {
        public int textureIndex;
        [Header("Height")]
        [Range(0.0f, 1.0f)] public float startingHeightPercentage = 0.0f;
        public int nextHeightIndex;
        [Range(0.0f, 0.1f)] public float overlap = 0.025f;
        [Header("Steepness")]
        public bool useSteepness = false;
        [MinMaxSlider(0.0f, 100.0f)] public UnityEngine.Vector2 steepnessThreshold = new(0.0f, 0.0f);

        public SplatHeights(int textureIndex, int nextHeightIndex)
        {
            this.textureIndex = textureIndex;
            this.nextHeightIndex = nextHeightIndex;
        }

        public SplatHeights(int textureIndex, float startingHeightPercentage, int nextHeightIndex, float overlap, bool useSteepness, UnityEngine.Vector2 steepnessThreshold)
        {
            this.textureIndex = textureIndex;
            this.startingHeightPercentage = startingHeightPercentage;
            this.nextHeightIndex = nextHeightIndex;
            this.overlap = overlap;
            this.useSteepness = useSteepness;
            this.steepnessThreshold = steepnessThreshold;
        }
    }

    public static void AssignSplatMap(TerrainData terrainData, SplatHeights[] splatHeights)
    {
        float[,,] splatmapData;
        // Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
        try
        {
            splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float currentHeight = terrainData.GetHeight(y, x);
                float currentSteepness = terrainData.GetSteepness((float)y / terrainData.alphamapHeight, (float)x / terrainData.alphamapWidth);
                float[] splat = new float[splatHeights.Length];

                for (int i = 0; i < splatHeights.Length; i++)
                {
                    float noise = Map(Mathf.PerlinNoise(x * 0.01f, y * 0.01f), 0, 1, 0.8f, 1);
                    float thisHeightStart = (splatHeights[i].startingHeightPercentage - splatHeights[i].overlap) * terrainData.size.y * noise;

                    int nextHeightIndex = splatHeights[i].nextHeightIndex;
                    float nextHeightStart = 0;
                    if (i != splatHeights.Length - 1)
                        nextHeightStart = (splatHeights[nextHeightIndex].startingHeightPercentage +
                                            splatHeights[nextHeightIndex].overlap) * terrainData.size.y * noise;

                    float value = 0.0f;
                    if (i == splatHeights.Length - 1 && currentHeight >= thisHeightStart)
                        value = 1.0f;
                    else if (currentHeight >= thisHeightStart && currentHeight <= nextHeightStart)
                        value = 1.0f;

                    if (splatHeights[i].useSteepness)
                    {
                        splat[i] = (currentSteepness >= splatHeights[i].steepnessThreshold.x
                                && currentSteepness <= splatHeights[i].steepnessThreshold.y) ? value : 0.0f;
                    }
                    else
                    {
                        splat[i] = value;
                    }
                }

                Normalize(splat);
                for (int j = 0; j < splatHeights.Length; j++)
                {
                    splatmapData[x, y, j] = splat[j];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
