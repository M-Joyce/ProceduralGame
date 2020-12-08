using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public static class BiomeNoiseMap {


	public static float[,] GenerateBiomeNoiseMap(int mapWidth, int mapHeight, BiomeNoiseSettings settings, Vector2 sampleCenter) { //biome noise
		float[,] biomeNoiseMap = new float[mapWidth,mapHeight];

		System.Random prng = new System.Random (settings.seed);
        int seed = prng.Next(0,int.MaxValue);

        FastNoise fastNoise = new FastNoise(seed); //FastNoise
        fastNoise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural); //use natural distance function
		fastNoise.SetCellularJitter(0.5f);

        Vector2[] octaveOffsets = new Vector2[settings.octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < settings.octaves; i++) {
			float offsetX = prng.Next (-1000000, 1000000) + settings.offset.x + sampleCenter.x;
			float offsetY = prng.Next (-1000000, 1000000) - settings.offset.y - sampleCenter.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.persistance;
		}

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;


		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				float cellularValue = 0;

				for (int i = 0; i < settings.octaves; i++) {
					float sampleX = (x-halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y-halfHeight + octaveOffsets[i].y) / settings.scale * frequency;
                    
                    cellularValue = fastNoise.GetCellular(sampleX,sampleY); //get cellular noise value

					noiseHeight += cellularValue * amplitude;

					amplitude *= settings.persistance;
					frequency *= settings.lacunarity;
				}

				biomeNoiseMap[x, y] = Mathf.Clamp01(noiseHeight);

			}
		}

		

		return biomeNoiseMap;
	}

}
