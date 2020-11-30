using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class BiomeHeightMapSettings : UpdatableData //add these to the biomes array in the parent biomeNoiseSettings object
{

	public BiomeNoiseSettings biomeNoiseSettings;

	public bool useFalloff;

	//heightMultiplier is for making the map not flat, heightCurve is for making it so the water doesn't also increase in height. heightCurve should match the level of the water near exactly.
	public float heightMultiplier;
	public AnimationCurve heightCurve;

	public float minHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(0);
		}
	}

	public float maxHeight
	{
		get
		{
			return heightMultiplier * heightCurve.Evaluate(1);
		}
	}

#if UNITY_EDITOR

	protected override void OnValidate()
	{
		biomeNoiseSettings.ValidateValues();
		base.OnValidate();
	}
#endif

}