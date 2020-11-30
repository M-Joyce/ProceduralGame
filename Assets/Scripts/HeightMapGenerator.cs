using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

	static float[,] falloffMap;

	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter) {
		float[,] values = Noise.GenerateNoiseMap (width, height, settings.noiseSettings, sampleCenter);
		float[,] biomeValues = BiomeNoiseMap.GenerateBiomeNoiseMap(width, height, settings.biomeNoiseSettings, sampleCenter);

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

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

				if (settings.biomeNoiseSettings.useBiomes == true) //make the map using biomes if true
				{
					if (biomeValues[i, j] >= 0 && biomeValues[i, j] < 0.3)
					{
						values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier;

					}
					else if (biomeValues[i, j] >= 0.3 && biomeValues[i, j] < 0.6)
					{

					}
					else if (biomeValues[i, j] >= 0.6 && biomeValues[i, j] < 0.9)
					{

					}
					else if (biomeValues[i, j] >= 0.9)
					{

					}
					else
					{
						values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier;
					}
				}
                else
                {
					values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (settings.useFalloff ? falloffMap[i, j] : 0)) * settings.heightMultiplier;
				}

					



				
				if (values [i, j] > maxValue) {
					maxValue = values [i, j];
				}
				if (values [i, j] < minValue) {
					minValue = values [i, j];
				}
			}
		}

		return new HeightMap (values, minValue, maxValue,biomeValues);
	}

}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float[,] biomeValues;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue, float[,] biomeValues)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.biomeValues = biomeValues;
	}

}

