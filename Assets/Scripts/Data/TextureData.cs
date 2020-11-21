using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{

    public Color[] baseColors;
    
    [Range(0,1)]
    public float[] baseStartHeights; //determine the start height for each color Range 0-1

    float savedMaxHeight;
    float savedMinHeight;
   
    public void ApplyToMaterial(Material material)
    {
        material.SetInt("baseColorCount",baseColors.Length);
        material.SetColorArray("baseColors",baseColors);
        material.SetFloatArray("baseStartHeights", baseStartHeights);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight; //set preview heights

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }


}
