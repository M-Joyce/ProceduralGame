using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

	static float[,] falloffMap;

	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter, BiomeNoiseSettings biomeNoiseSettings) {
		float[,] values = Noise.GenerateNoiseMap (width, height, settings.noiseSettings, sampleCenter);
		float[,] biomeNoiseMapValues = BiomeNoiseMap.GenerateBiomeNoiseMap(width, height, biomeNoiseSettings, sampleCenter);

		AnimationCurve heightCurve_threadsafe = new AnimationCurve (settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		if (settings.useFalloff)
		{
			if (falloffMap == null)
			{
				falloffMap = FalloffGenerator.GenerateFalloffMap(width);
			}
		}

		if (biomeNoiseSettings.useBiomes == true) //make the map using biomes if true
		{
			BiomeHeightMapSettings plainsBiome = biomeNoiseSettings.biomes[0];
			AnimationCurve plainsHeightCurve_threadsafe = new AnimationCurve(plainsBiome.heightCurve.keys);
			//TODO Add biomes and use below

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (biomeNoiseMapValues[i, j] >= 0 && biomeNoiseMapValues[i, j] < 0.3)
					{
						values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier;

					}
					else if (biomeNoiseMapValues[i, j] >= 0.3 && biomeNoiseMapValues[i, j] < 0.6)
					{
						//plains
						values[i, j] *= plainsHeightCurve_threadsafe.Evaluate(values[i, j] - (plainsBiome.useFalloff ? falloffMap[i, j] : 0)) * plainsBiome.heightMultiplier;

					}
					else if (biomeNoiseMapValues[i, j] >= 0.6 && biomeNoiseMapValues[i, j] < 0.9)
					{
						
					}
					else if (biomeNoiseMapValues[i, j] >= 0.9)
					{
						
					}
					else
					{
						values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier;
					}

					if (values[i, j] > maxValue)
					{
						maxValue = values[i, j];
					}
					if (values[i, j] < minValue)
					{
						minValue = values[i, j];
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier; //default, no biomes
					if (values[i, j] > maxValue)
					{
						maxValue = values[i, j];
					}
					if (values[i, j] < minValue)
					{
						minValue = values[i, j];
					}
				}
			}
					
		}


		return new HeightMap (values, minValue, maxValue,biomeNoiseMapValues);
	}

}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float[,] biomeNoiseMapValues;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue, float[,] biomeNoiseMapValues)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.biomeNoiseMapValues = biomeNoiseMapValues;
	}

}

