using System;

public static class FineTuning
{

    public class WaterPrefabData
    {
        public float flatPosition;
        public float verticalPosition;
        public float flatScale;

        public WaterPrefabData(float verticalPosition, float flatScale, int N)
        {
            this.flatPosition = (int)Math.Pow(2, N);
            this.verticalPosition = verticalPosition;
            this.flatScale = flatScale;
        }

        public WaterPrefabData(float flatPositionOffset, float verticalPosition, float flatScale)
        {
            this.flatPosition = flatPositionOffset;
            this.verticalPosition = verticalPosition;
            this.flatScale = flatScale;
        }
    }

    public class SkyPrefabData
    {
        public float flatPosition;
        public float verticalPosition;
        public float flatScale;
        public float verticalScale;

        public SkyPrefabData(float flatPosition, float verticalPosition, float flatScale, float verticalScale)
        {
            this.flatPosition = flatPosition;
            this.verticalPosition = verticalPosition;
            this.flatScale = flatScale;
            this.verticalScale = verticalScale;
        }
    }

    public static WaterPrefabData calculateWaterPrefabData(int N, float minTerrainHeight, float maxTerrainHeight, float waterLevelPercentage)
    {
        float verticalPosition = -minTerrainHeight * waterLevelPercentage;
        float flatScale = 0;
        switch (N)
        {
            case 5:
                flatScale = 3.6f;
                break;
            case 6:
                flatScale = 7.2f;
                break;
            case 7:
                flatScale = 14.4f;
                break;
            case 8:
                flatScale = 28.8f;
                break;
            case 9:
                flatScale = 57.6f;
                break;
        }

        return new WaterPrefabData(verticalPosition, flatScale, N);
    }

    public static SkyPrefabData calculateSkyPrefabData(int N, float maxTerrainHeight, bool skybox = false)
    {
        float flatPosition = (int)Math.Pow(2, N);
        float verticalPosition = maxTerrainHeight * (skybox ? 0 : 0.25f);
        float flatScale = 0;
        float verticalScale = 0;
        switch (N)
        {
            case 5:
                flatScale = skybox ? 0.6f : 5f;
                verticalScale = 1.0f;
                break;
            case 6:
                flatScale = skybox ? 1.2f : 10f;
                verticalScale = 2.0f;
                break;
            case 7:
                flatScale = skybox ? 2.4f : 20f;
                verticalScale = 4.0f;
                break;
            case 8:
                flatScale = skybox ? 4.8f : 40f;
                verticalScale = 8.0f;
                break;
            case 9:
                flatScale = skybox ? 9.6f : 80f;
                verticalScale = 16.0f;
                break;
        }
        return new SkyPrefabData(flatPosition, verticalPosition, flatScale, skybox ? verticalScale : 0);
    }
}
