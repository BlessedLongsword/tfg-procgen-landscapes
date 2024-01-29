using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using static TerrainAlgorithms;
using static TerrainTexturing;
using static FineTuning;
using GD.MinMaxSlider;
using Unity.Mathematics;
using JetBrains.Annotations;
using Unity.VisualScripting.Dependencies.Sqlite;

public class LoadChunks : MonoBehaviour
{

    enum Direction
    {
        North,
        East,
        South,
        West
    };

    public enum SkyMode
    {
        None,
        Skybox,
        ChunkClouds
    }

    [Header("Generation")]

    public int seed = 1;

    [Range(5, 9)] public int N = 5;
    [MinMaxSlider(-500.0f, 500.0f)] public UnityEngine.Vector2 terrainHeights = new(-500.0f, 500.0f);
    public Algorithm algorithm = Algorithm.MidpointDisplacement;

    [Header("Biomes")]
    [Range(0.0f, 1.0f)] public float chanceNeighbour = 0.9f;
    public Biome[] biomes = {
                                new Biome("Plain", minAmplitude: 0.1f, maxAmplitude: 0.5f, minRoughness: 0.05f, maxRoughness: 0.2f, minInitialAltitudes: 0, maxInitialAltitudes: 0.25f),
                                new Biome("Hill", minAmplitude: 1, maxAmplitude: 1.5f, minRoughness: 0.05f, maxRoughness: 0.25f, minInitialAltitudes: 0, maxInitialAltitudes: 1, minMinRandomRange: -0.5f, maxMinRandomRange: 0f),
                                new Biome("Mountain", minAmplitude: 1, maxAmplitude: 2.0f, minRoughness: 0.25f, maxRoughness: 0.4f, minInitialAltitudes: 0, maxInitialAltitudes: 1, minHeight: 35, maxHeight: 300),
                                new Biome("Coast", minAmplitude: 0.1f, maxAmplitude: 0.5f, minRoughness: 0.05f, maxRoughness: 0.2f, minInitialAltitudes: 0, maxInitialAltitudes: 0.1f, minHeight: -100, maxHeight: 1)
                            };

    public string startingBiome = "Plain";
    [Header("Water")]
    public bool generateWater = true;
    [Range(0.0f, 1.0f)] public float waterLevelPercentage = .0f;

    public GameObject water;

    [Header("Sky")]
    public SkyMode skyMode = SkyMode.None;
    public GameObject skybox;
    public GameObject sky;


    SplatHeights[] splatHeights =
    {
        new SplatHeights(0, 0.0f, 1, 0f, false, new UnityEngine.Vector2(0.0f, 0.1f)),
        new SplatHeights(1, 0.1f, 3, 0.1f, true, new UnityEngine.Vector2(0.0f, 26.0f)),
        new SplatHeights(2, 0.1f, 3, 0.1f, true, new UnityEngine.Vector2(20.0f, 100.0f)),
        new SplatHeights(3, 0.5f, 3, 0.1f, false, new UnityEngine.Vector2(0.5f, 0.5f))
    };
    Vector3 playerPosition;
    Direction playerFacingDirection = Direction.North;
    Tuple<int, int> terrainAtPlayerPosition = new Tuple<int, int>(0, 0);


    Dictionary<Tuple<int, int>, Chunk> chunks;
    int maxChunks = 100;
    int storedChunks = 0;
    int chunkCounter = 0;
    Dictionary<Tuple<int, int>, int> seeds;

    class Chunk
    {
        public string biome;
        public int seed;
        public float[,] heightMap;
        public bool[,] plantMap;
        public TerrainData terrainData;
        public Terrain terrain;
        public GameObject waterChunk;
        public GameObject skyChunk;
        public List<GameObject> plants;
        readonly TerrainGenerator terrainGenerator;
        public bool created = false;
        public bool active = false;
        public int number;
        public Chunk(AlgorithmParameters parameters, Algorithm algorithm, int seed, int number, string biome = null)
        {
            this.biome = biome;
            this.seed = seed;
            terrainGenerator = new TerrainGenerator();
            heightMap = terrainGenerator.GenerateHeightMap(parameters, algorithm: algorithm, seed: seed);
        }

        public void UpdateTerrainData(float minTerrainHeight, float maxTerrainHeight, int N, Biome biome)
        {
            terrainData = terrainGenerator.GenerateTerrainData(heightMap, minTerrainHeight, maxTerrainHeight, N: N,
                                                                biome.terrainLayerNames, biome.splatHeights);
            if (biome.plants.Length > 0)
                plantMap = terrainGenerator.GeneratePlantMap(heightMap, terrainData, N, biome, seed);
        }
        public void UpdateTerrain(int size, int i, int j)
        {
            if (!created)
                terrain = terrainGenerator.GenerateTerrain(terrainData, size, i, j);
            else
                terrain.transform.position = new Vector3(i * size, 0, j * size);
            active = true;
        }
        public void UpdateWaterChunk(int N, float minTerrainHeight, float maxTerrainHeight, float waterLevelPercentage, GameObject water)
        {
            WaterPrefabData waterData = calculateWaterPrefabData(N, minTerrainHeight, maxTerrainHeight, waterLevelPercentage);
            float flatPositionOffset = waterData.flatPosition / 2;
            if (!created)
            {
                waterChunk = Instantiate(water, new Vector3(terrain.transform.position.x + flatPositionOffset,
                                                            waterData.verticalPosition,
                                                            terrain.transform.position.z + flatPositionOffset),
                                                    Quaternion.identity);
                waterChunk.transform.localScale = new Vector3(waterData.flatScale, 1, waterData.flatScale);
            }
            else
                waterChunk.transform.position = new Vector3(terrain.transform.position.x + flatPositionOffset,
                                                            waterData.verticalPosition,
                                                            terrain.transform.position.z + flatPositionOffset);
        }

        public void UpdateSkyChunk(int N, float maxTerrainHeight, GameObject sky)
        {
            SkyPrefabData skyData = calculateSkyPrefabData(N, maxTerrainHeight);
            float flatPositionOffset = skyData.flatScale / 2;
            skyChunk = Instantiate(sky, new Vector3(terrain.transform.position.x + flatPositionOffset,
                                                        skyData.verticalPosition,
                                                        terrain.transform.position.z + flatPositionOffset),
                                                Quaternion.identity);
            skyChunk.transform.localScale = new Vector3(skyData.flatScale, skyData.verticalScale, skyData.flatScale);
        }

        public void UpdatePlants(int N, Biome biome, float minTerrainHeight, float waterLevelPercentage)
        {
            float size = (float)Math.Pow(2,N) + 1;
            plants = new List<GameObject>();
            for (int i = 0; i < plantMap.GetLength(0); i++)
            {
                for (int j = 0; j < plantMap.GetLength(1); j++)
                {
                    if (plantMap[i, j])
                    {
                        int x = (int)terrain.transform.position.x + i;
                        int z = (int)terrain.transform.position.z + j;
                        float height = terrainData.GetHeight(i, j) - terrainData.GetSteepness(i / size, j / size) * 0.01f;
                        GameObject plant = RollPlant(biome, height < -minTerrainHeight * waterLevelPercentage);
                        if (plant != null)
                        {
                            plants.Add(Instantiate(plant, new Vector3(x, height, z), Quaternion.identity));
                        }
                    }
                }
            }
        }

        private GameObject RollPlant(Biome biome, bool isBelowWater)
        {
            Tuple<GameObject, float>[] plants = new Tuple<GameObject, float>[biome.plants.Count()];
            if (plants.Length == 0)
                return null;
            float totalProbability = 0;
            for (int i = 0; i < biome.plants.Length; i++)
            {
                plants[i] = new Tuple<GameObject, float>(biome.plants[i].plant, (biome.plants[i].aquatic == isBelowWater) ? biome.plants[i].spawnProbability : 0);
                totalProbability += (biome.plants[i].aquatic == isBelowWater) ? biome.plants[i].spawnProbability : 0;
            }
            float roll = UnityEngine.Random.Range(0.0f, totalProbability);
            float currentProbability = 0;
            for (int i = 0; i < plants.Length; i++)
            {
                currentProbability += plants[i].Item2;
                if (roll < currentProbability)
                    return plants[i].Item1;
            }
            return plants[0].Item1;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(seed);
        chunks = new Dictionary<Tuple<int, int>, Chunk>();
        seeds = new Dictionary<Tuple<int, int>, int>
        {
            { terrainAtPlayerPosition, UnityEngine.Random.Range(0, int.MaxValue) }
        };
        int biomeIndex = GetBiomeIndex(startingBiome);
        biomeIndex = (biomeIndex == -1) ? UnityEngine.Random.Range(0, biomes.Length) : biomeIndex;
        AlgorithmParameters parameters = biomes[biomeIndex].GenerateParameters(N, terrainHeights.x, terrainHeights.y);

        chunks.Add(terrainAtPlayerPosition, new Chunk(parameters, algorithm: algorithm, seed: seeds[terrainAtPlayerPosition], chunkCounter++, biome: biomes[biomeIndex].name));
        storedChunks++;
        chunks[terrainAtPlayerPosition].UpdateTerrainData(terrainHeights.x, terrainHeights.y, N, biomes[biomeIndex]);
        chunks[terrainAtPlayerPosition].UpdateTerrain((int)Math.Pow(2, N) + 1, terrainAtPlayerPosition.Item1, terrainAtPlayerPosition.Item2);
        if (generateWater)
            chunks[terrainAtPlayerPosition].UpdateWaterChunk(N, terrainHeights.x, terrainHeights.y, waterLevelPercentage, water);
        switch (skyMode)
        {
            case SkyMode.Skybox:
                SkyPrefabData skyData = calculateSkyPrefabData(N, terrainHeights.y, true);
                skybox = Instantiate(skybox, transform.position, Quaternion.identity);
                skybox.transform.position = new Vector3(playerPosition.x, skyData.verticalPosition, playerPosition.z);
                skybox.transform.localScale = new Vector3(skyData.flatScale, skyData.verticalScale, skyData.flatScale);
                break;
            case SkyMode.ChunkClouds:
                chunks[terrainAtPlayerPosition].UpdateSkyChunk(N, terrainHeights.y, sky);
                break;
        }
        if (biomes[biomeIndex].plants.Length > 0)
            chunks[terrainAtPlayerPosition].UpdatePlants(N, biomes[biomeIndex], terrainHeights.x, waterLevelPercentage);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPlayerPosition = playerPosition;
        Tuple<int, int> currentTerrainAtPlayerPosition = terrainAtPlayerPosition;
        Direction currentFacingDirection = playerFacingDirection;
        UpdatePlayerPosition();
        if (currentPlayerPosition != playerPosition)
        {
            UpdateTerrainAtPlayerPosition();
            if (skyMode == SkyMode.Skybox)
                skybox.transform.position = new Vector3(playerPosition.x, skybox.transform.position.y, playerPosition.z);
        }
        if (currentTerrainAtPlayerPosition != terrainAtPlayerPosition || currentFacingDirection != playerFacingDirection)
        {
            LoadTerrain(terrainAtPlayerPosition.Item1, terrainAtPlayerPosition.Item2);
        }
    }

    private void UpdatePlayerPosition()
    {
        playerPosition = GameObject.Find("Player").transform.position;
        Vector3 playerRotation = GameObject.Find("Player").transform.rotation.eulerAngles;
        if ((0 <= playerRotation.y && playerRotation.y < 45) || (315 <= playerRotation.y && playerRotation.y < 360))
            playerFacingDirection = Direction.North;
        else if (45 <= playerRotation.y && playerRotation.y < 135)
            playerFacingDirection = Direction.East;
        else if (135 <= playerRotation.y && playerRotation.y < 225)
            playerFacingDirection = Direction.South;
        else
            playerFacingDirection = Direction.West;
    }

    private void UpdateTerrainAtPlayerPosition()
    {
        int chunkSize = (int)Math.Pow(2, N) + 1;
        terrainAtPlayerPosition = new Tuple<int, int>((int)playerPosition.x / chunkSize, (int)playerPosition.z / chunkSize);
    }

    private void DestroyChunk(int x, int z)
    {
        Tuple<int, int> key = new Tuple<int, int>(x, z);
        if (chunks.ContainsKey(key))
        {
            if (storedChunks > maxChunks)
            {
                Destroy(chunks[key].terrain.gameObject);
                chunks[key].created = false;
                if (generateWater)
                    Destroy(chunks[key].waterChunk);
            } else {
                int size = (int)Math.Pow(2, N) + 1;
                chunks[key].terrain.transform.position = new Vector3(x * size, -1000, z * size);
                if (generateWater)
                {
                    WaterPrefabData waterData = calculateWaterPrefabData(N, terrainHeights.x, terrainHeights.y, waterLevelPercentage);
                    float flatPositionOffset = waterData.flatPosition / 2;
                    chunks[key].waterChunk.transform.position = new Vector3(chunks[key].terrain.transform.position.x + flatPositionOffset,
                                                                    waterData.verticalPosition - 1000,
                                                                    chunks[key].terrain.transform.position.z + flatPositionOffset);
                }
            }
            if (skyMode == SkyMode.ChunkClouds)
                Destroy(chunks[key].skyChunk);
            if (chunks[key].plants != null)
            {
                foreach (GameObject plant in chunks[key].plants)
                    Destroy(plant);
                chunks[key].plants.Clear();
            }
            chunks[key].active = false;
        }
    }

    private void LoadTerrain(int x, int z)
    {
        List<Tuple<int, int>> activeChunks = new List<Tuple<int, int>>();
        SetActiveChunks(activeChunks);

        foreach (Tuple<int, int> chunk in activeChunks)
        {
            if (!chunks.ContainsKey(chunk))
            {
                int biomeIndex = ComputeBiome(chunk, 1 - chanceNeighbour);
                AlgorithmParameters parameters = biomes[biomeIndex].GenerateParameters(N, terrainHeights.x, terrainHeights.y);
                int[] sides = ComputeSides(chunk);
                if (sides != null)
                {
                    parameters.presetSides = ComputeSideHeights(chunk, sides);
                    parameters.sides = sides;
                }
                if (!seeds.ContainsKey(chunk))
                    seeds.Add(chunk, UnityEngine.Random.Range(0, int.MaxValue));
                chunks.Add(chunk, new Chunk(parameters, algorithm, seeds[chunk], chunkCounter++, biomes[biomeIndex].name));
                storedChunks++;
                chunks[chunk].UpdateTerrainData(terrainHeights.x, terrainHeights.y, N, biomes[biomeIndex]);
            }
            if (!chunks[chunk].active)
            {
                chunks[chunk].UpdateTerrain((int)Math.Pow(2, N) + 1, chunk.Item1, chunk.Item2);
                if (generateWater)
                    chunks[chunk].UpdateWaterChunk(N, terrainHeights.x, terrainHeights.y, waterLevelPercentage, water);
                if (skyMode == SkyMode.ChunkClouds)
                    chunks[chunk].UpdateSkyChunk(N, terrainHeights.y, sky);
                int biomeIndex = GetBiomeIndex(chunks[chunk].biome);
                if (biomes[biomeIndex].plants.Length > 0)
                    chunks[chunk].UpdatePlants(N, biomes[biomeIndex], terrainHeights.x, waterLevelPercentage);
                chunks[chunk].created = true;
            }
        }

        foreach (Tuple<int, int> chunk in chunks.Keys.ToList().Where(x => chunks[x].active && !activeChunks.Contains(x)).OrderBy(x => chunks[x].number))
            DestroyChunk(chunk.Item1, chunk.Item2);
    }

    private void SetActiveChunks(List<Tuple<int, int>> activeChunks)
    {
        Tuple<int, int> basePosition = terrainAtPlayerPosition;
        activeChunks.Add(basePosition);

        int[,] directionOffsets = GetDirectionOffsets(playerFacingDirection);

        for (int i = 0; i < directionOffsets.GetLength(0); i++)
        {
            int offsetX = directionOffsets[i, 0];
            int offsetY = directionOffsets[i, 1];
            activeChunks.Add(new Tuple<int, int>(basePosition.Item1 + offsetX, basePosition.Item2 + offsetY));
        }
    }

    private int[,] GetDirectionOffsets(Direction direction)
    {
        int[,] offsets;

        switch (direction)
        {
            case Direction.North:
                offsets = new int[,] {  { 0, -1 }, { -1, -1 }, { 1, -1 }, { -2, -1 }, { 2, -1 }, { -1, 0 }, { 1, 0 },
                                        { -2, 0 }, { 2, 0 }, { -3, 0 }, { 3, 0 }, { 0, 1 }, { -1, 1 }, { 1, 1 }, { -2, 1 },
                                        { 2, 1 }, { -3, 1 }, { 3, 1 }, { 0, 2 }, { -1, 2 }, { 1, 2 }, { -2, 2 }, { 2, 2 },
                                        { -3, 2 }, { 3, 2 }, { 0, 3 }, { -1, 3 }, { 1, 3 }, { -2, 3 }, { 2, 3 } };
                break;
            case Direction.East:
                offsets = new int[,] {  { -1, 0 }, { -1, -1 }, { -1, 1 }, { -1, -2 }, { -1, 2 }, { 0, -1 }, { 0, 1 },
                                        { 0, -2 }, { 0, 2 }, { 0, -3 }, { 0, 3 }, { 1, 0 }, { 1, -1 }, { 1, 1 }, { 1, -2 },
                                        { 1, 2 }, { 1, -3 }, { 1, 3 }, { 2, 0 }, { 2, -1 }, { 2, 1 }, { 2, -2 }, { 2, 2 },
                                        { 2, -3 }, { 2, 3 }, { 3, 0 }, { 3, -1 }, { 3, 1 }, { 3, -2 }, { 3, 2 } };
                break;
            case Direction.South:
                offsets = new int[,] {  { 0, 1 }, { -1, 1 }, { 1, 1 }, { -2, 1 }, { 2, 1 }, { -1, 0 }, { 1, 0 }, { -2, 0 },
                                        { 2, 0 }, { -3, 0 }, { 3, 0 }, { 0, -1 }, { -1, -1 }, { 1, -1 }, { -2, -1 }, { 2, -1 },
                                        { -3, -1 }, { 3, -1 }, { 0, -2 }, { -1, -2 }, { 1, -2 }, { -2, -2 }, { 2, -2 }, { -3, -2 },
                                        { 3, -2 }, { 0, -3 }, { 1, -3 }, { -1, -3 }, { -2, -3 }, { 2, -3 } };
                break;
            case Direction.West:
                offsets = new int[,] {  { 1, 0 }, { 1, -1 }, { 1, 1 }, { 1, -2 }, { 1, 2 }, { 0, -1 }, { 0, 1 }, { 0, -2 },
                                        { 0, 2 }, { 0, -3 }, { 0, 3 }, { -1, 0 }, { -1, -1 }, { -1, 1 }, { -1, -2 }, { -1, 2 },
                                        { -1, -3 }, { -1, 3 }, { -2, 0 }, { -2, -1 }, { -2, 1 }, { -2, -2 }, { -2, 2 }, { -2, -3 },
                                        { -2, 3 }, { -3, 0 }, { -3, -1 }, { -3, 1 }, { -3, -2 }, { -3, 2 } };
                break;
            default:
                offsets = new int[,] { };
                break;
        }

        return offsets;
    }

    private int[] ComputeSides(Tuple<int, int> chunk)
    {
        List<int> sides = new List<int>();
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1, chunk.Item2 - 1)))
            sides.Add(0);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1 - 1, chunk.Item2)))
            sides.Add(1);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1, chunk.Item2 + 1)))
            sides.Add(2);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1 + 1, chunk.Item2)))
            sides.Add(3);
        return (sides.Count == 0) ? null : sides.ToArray();
    }

    private int ComputeBiome(Tuple<int, int> chunk, float chanceNeighbour = 0.9f)
    {
        List<string> neighbours = new List<string>();

        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1, chunk.Item2 - 1)))
            neighbours.Add(chunks[new Tuple<int, int>(chunk.Item1, chunk.Item2 - 1)].biome);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1 - 1, chunk.Item2)))
            neighbours.Add(chunks[new Tuple<int, int>(chunk.Item1 - 1, chunk.Item2)].biome);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1, chunk.Item2 + 1)))
            neighbours.Add(chunks[new Tuple<int, int>(chunk.Item1, chunk.Item2 + 1)].biome);
        if (chunks.ContainsKey(new Tuple<int, int>(chunk.Item1 + 1, chunk.Item2)))
            neighbours.Add(chunks[new Tuple<int, int>(chunk.Item1 + 1, chunk.Item2)].biome);

        if (neighbours.Count == 0 || !(UnityEngine.Random.Range(0.0f, 1.0f) < chanceNeighbour))
            return UnityEngine.Random.Range(0, biomes.Length);

        List<int> biomeIndexes = new List<int>();
        for (int i = 0; i < neighbours.Count; i++)
            biomeIndexes.Add(GetBiomeIndex(neighbours[i]));
        return biomeIndexes[UnityEngine.Random.Range(0, biomeIndexes.Count)];
    }

    private int GetBiomeIndex(string biomeName)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i].name == biomeName)
                return i;
        }
        return -1;
    }

    private float[][] ComputeSideHeights(Tuple<int, int> chunk, int[] sides)
    {
        int size = (int)Math.Pow(2, N) + 1;
        float[][] sideHeights = new float[4][];
        for (int i = 0; i < sideHeights.GetLength(0); i++)
        {
            if (sides.Contains<int>(i))
            {
                sideHeights[i] = new float[size];
                for (int j = 0; j < size; j++)
                {
                    switch (i)
                    {
                        case 0:
                            sideHeights[i][j] = chunks[new Tuple<int, int>(chunk.Item1, chunk.Item2 - 1)].heightMap[size - 1, j];
                            break;
                        case 1:
                            sideHeights[i][j] = chunks[new Tuple<int, int>(chunk.Item1 - 1, chunk.Item2)].heightMap[j, size - 1];
                            break;
                        case 2:
                            sideHeights[i][j] = chunks[new Tuple<int, int>(chunk.Item1, chunk.Item2 + 1)].heightMap[0, j];
                            break;
                        case 3:
                            sideHeights[i][j] = chunks[new Tuple<int, int>(chunk.Item1 + 1, chunk.Item2)].heightMap[j, 0];
                            break;
                    }
                }
            }
            else
                sideHeights[i] = null;
        }
        return sideHeights;
    }
}
