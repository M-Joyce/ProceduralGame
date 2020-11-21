using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
	//From MapGenerator
	public Noise.NormalizeMode normalizeMode;
	public float noiseScale;
	public int octaves;
	[Range(0, 1)] //persistance clamped to 0-1
	public float persistance;
	public float lacunarity;
	public int seed;
	public Vector2 offset;

#if UNITY_EDITOR //Only compile if in the editor
	protected override void OnValidate()
    {
		if (lacunarity < 1)
		{
			lacunarity = 1;
		}
		if (octaves < 0)
		{
			octaves = 0;
		}

		base.OnValidate();
	}
#endif

}
