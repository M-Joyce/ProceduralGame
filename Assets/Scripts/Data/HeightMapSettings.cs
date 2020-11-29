using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData {

	public NoiseSettings noiseSettings;
	public BiomeNoiseSettings biomeNoiseSettings;

	public bool useFalloff;

	//heightMultiplier is for making the map not flat, heightCurve is for making it so the water doesn't also increase in height. heightCurve should match the level of the water near exactly.
	public float heightMultiplier;
	public AnimationCurve heightCurve;

	public float minHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate (0);
		}
	}

	public float maxHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate (1);
		}
	}

	#if UNITY_EDITOR

	protected override void OnValidate() {
		noiseSettings.ValidateValues ();
		biomeNoiseSettings.ValidateValues();
		base.OnValidate();
	}
	#endif

}
