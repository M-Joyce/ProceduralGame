using UnityEngine;
using System.Collections;

public static class TextureGenerator {

	public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}


	public static Texture2D TextureFromHeightMap(HeightMap heightMap) {
		int width = heightMap.values.GetLength (0);
		int height = heightMap.values.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue,heightMap.maxValue,heightMap.values [x, y]));
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}

	public static Texture2D TextureFromBiomeMap(float[,] biomeValues)
	{
		int width = biomeValues.GetLength(0);
		int height = biomeValues.GetLength(1);

		Color[] colorMap = new Color[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				//Color color;
				//if (biomeValues[x, y] >= 0 && biomeValues[x, y] < 0.33)
				//{
				//	color = new Color(0.8f, 0.6f, 0.2f, 1f); //sand color
				//}
				//else if (biomeValues[x, y] >= 0.33 && biomeValues[x, y] < 0.66)
				//{
				//	color = Color.green;
				//}
				//else if (biomeValues[x, y] >= 0.66 && biomeValues[x, y] <= 1)
				//{
				//	color = Color.gray;
				//}
				//else {
				//	color = Color.black;
				//}

				colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(0, 1, biomeValues[x, y])); ;
			}
		}

		return TextureFromColourMap(colorMap, width, height);
	}

}
