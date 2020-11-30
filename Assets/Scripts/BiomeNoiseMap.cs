using UnityEngine;
using System.Collections;

public static class BiomeNoiseMap {


	public static float[,] GenerateBiomeNoiseMap(int mapWidth, int mapHeight, BiomeNoiseSettings settings, Vector2 sampleCenter) { //terrain/heightmap noise
		float[,] biomeNoiseMap = new float[mapWidth,mapHeight];

		System.Random prng = new System.Random (settings.seed);
		Vector2[] octaveOffsets = new Vector2[settings.octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < settings.octaves; i++) {
			float offsetX = prng.Next (-10000, 10000) + settings.offset.x + sampleCenter.x;
			float offsetY = prng.Next (-10000, 10000) - settings.offset.y - sampleCenter.y;
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

				for (int i = 0; i < settings.octaves; i++) {
					float sampleX = (x-halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y-halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.persistance;
					frequency *= settings.lacunarity;
				}

				biomeNoiseMap [x, y] = noiseHeight;

				
				 float normalizedHeight = (biomeNoiseMap [x, y] + 1) / (2f * maxPossibleHeight / 2f); //the last division here can be changed a bit from 1-3, just play with it. 2f is nice.
				biomeNoiseMap [x, y] = Mathf.SmoothStep(0, 1,normalizedHeight);
				
			}
		}

		

		return biomeNoiseMap;
	}

}


[System.Serializable]
public class BiomeNoiseSettings
{

	public float scale = 50;
	public bool useBiomes;

	public int octaves = 6;
	[Range(0, 1)]
	public float persistance = .6f;
	public float lacunarity = 2;

	public int seed;
	public Vector2 offset;

	public void ValidateValues()
	{
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}