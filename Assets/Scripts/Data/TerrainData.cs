using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
	//From MapGenerator
	public bool useFlatShading;
	public bool useFalloff;
	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	//From Infinite Terrain
	public float uniformScale = 5f; //This can be changed to alter to scale of the map, ie if you want it to better fit your players scale


	//For shader
	public float minHeight
    {
        get
        {
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }
	//For shader
	public float maxHeight
	{
		get
		{
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
		}
	}
}
