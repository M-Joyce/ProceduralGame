using UnityEngine;

[CreateAssetMenu()]
public class BiomeNoiseSettings : UpdatableData
{

	public float scale = 50;
	public bool useBiomes;

	public int octaves = 6;
	[Range(0, 1)]
	public float persistance = .6f;
	public float lacunarity = 2;

	public int seed;
	public Vector2 offset;


	public BiomeHeightMapSettings[] biomes; //an array of biome hieght map settings to be accessed


	public void ValidateValues()
	{
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		lacunarity = Mathf.Max(lacunarity, 1);
		persistance = Mathf.Clamp01(persistance);
	}
}