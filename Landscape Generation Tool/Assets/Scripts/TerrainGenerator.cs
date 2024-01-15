using System;
using UnityEngine;

using static TerrainAlgorithms;
using static Utils;
using static TerrainTexturing;

public class TerrainGenerator
{
    public float[,] GenerateHeightMap(AlgorithmParameters algorithmParameters, int seed = 0, Algorithm algorithm = Algorithm.MidpointDisplacement)
    {
        UnityEngine.Random.InitState((seed == 0) ? UnityEngine.Random.Range(0, int.MaxValue) : seed);
        float[,] heightMap;
        switch (algorithm)
        {
            case Algorithm.MidpointDisplacement:
                heightMap = MidpointDisplacement(algorithmParameters);
                break;
            case Algorithm.DiamondSquares:
                heightMap = DiamondSquares(algorithmParameters);
                break;
            default:
                heightMap = MidpointDisplacement(algorithmParameters);
                break;
        }
        return heightMap;
    }

    public bool[,] GeneratePlantMap(float[,] heightMap, TerrainData terrainData, int N, Biome biome, int seed = 0)
    {
        UnityEngine.Random.InitState((seed == 0) ? UnityEngine.Random.Range(0, int.MaxValue) : seed);
        int size = (int)Math.Pow(2, N) + 1;
        bool[,] plantMap = new bool[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                plantMap[x, z] = heightMap[x, z] < biome.maxHeight &&
                                    UnityEngine.Random.Range(0f, 1f) < biome.density &&
                                    terrainData.GetSteepness((float)x / size, (float)z / size) < biome.maxSteepness &&
                                    PlantIsolated(plantMap, biome.isolationRadius, x, z);
            }
        }
        return plantMap;
    }

    private bool PlantIsolated(bool[,] plantMap, float radius, int x, int z)
    {
        int size = plantMap.GetLength(0);
        int radiusInt = (int)radius;
        for (int i = Math.Clamp(x - radiusInt, 0, size); i < x + radiusInt; i++)
        {
            for (int j = Math.Clamp(z - radiusInt, 0, size); j < Math.Clamp(z - radiusInt, 0, size); j++)
            {
                if (plantMap[i, j])
                    return false;
            }
        }
        return true;
    }

    public TerrainData GenerateTerrainData(float[,] heightMap, float minTerrainHeight, float maxTerrainHeight,
                                                    int N = 5, string[] terrainLayerNames = null, SplatHeights[] splatHeights = null)
    {
        float[,] heightMapCopy = (float[,])heightMap.Clone();
        int terrainSize = (int)Math.Pow(2, N) + 1;
        float terrainHeight = NormalizeHeightmap(heightMapCopy, terrainSize, minTerrainHeight, maxTerrainHeight);
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = terrainSize,
            alphamapResolution = terrainSize,
            baseMapResolution = terrainSize,
            size = new UnityEngine.Vector3(terrainSize, terrainHeight, terrainSize),
            terrainLayers = GetTerrainLayers(terrainLayerNames)
        };
        terrainData.SetHeights(0, 0, heightMapCopy);
        AssignSplatMap(terrainData, splatHeights);
        return terrainData;
    }

    public Terrain GenerateTerrain(TerrainData terrainData, int size, int i, int j)
    {
        Terrain terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.materialTemplate = Resources.Load<Material>("Materials/TerrainMaterial");
        terrain.transform.position = new UnityEngine.Vector3(i * size, 0, j * size);
        terrain.gameObject.name = "Terrain " + i + " " + j;
        return terrain;
    }

    private TerrainLayer[] GetTerrainLayers(string[] terrainLayerNames)
    {
        // Load Terrain Layers from Resources
        TerrainLayer[] terrainLayers;
        if (terrainLayerNames == null)
        {
            terrainLayers = new TerrainLayer[4];
            terrainLayers[0] = Resources.Load<TerrainLayer>("TerrainLayers/coast_layer");
            terrainLayers[1] = Resources.Load<TerrainLayer>("TerrainLayers/ground_layer");
            terrainLayers[2] = Resources.Load<TerrainLayer>("TerrainLayers/rock_layer");
            terrainLayers[3] = Resources.Load<TerrainLayer>("TerrainLayers/snow_layer");
        }
        else
        {
            terrainLayers = new TerrainLayer[terrainLayerNames.Length];
            for (int i = 0; i < terrainLayerNames.Length; i++)
                terrainLayers[i] = Resources.Load<TerrainLayer>("TerrainLayers/" + terrainLayerNames[i].ToLower() + "_layer");
        }

        return terrainLayers;
    }


    /* private void CreateTerrainCollider(Terrain terrain)
    {
        TerrainCollider terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = terrain.terrainData;
    } */
}
