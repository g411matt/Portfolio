using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Snippet of manipulating and generating Unity textures using newer experimental Unity utilities
/// Author: Matt Gall
/// </summary>

public class TextureManipSnippet
{
    public static void TextureCreate(Texture2D texture) 
    {
        string path = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) 
        {
            return;
        }
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
        
        // don't use the imported texture it likely has texture compression applied to it plus resizing
        // load the bytes, create an empty texture without mipmaps or compression, use LoadImage to convert the raw bytes directly into a unity texture
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D rawTex = new Texture2D(2, 2, textureImporter.DoesSourceTextureHaveAlpha() ? TextureFormat.ARGB32 : TextureFormat.RGB24, false, !textureImporter.sRGBTexture);
        rawTex.LoadImage(bytes);

        // grab the texture data native array for modifying
        var colorArr = rawTex.GetRawTextureData<Color32>();
        
        // alternatively instead of using the raw texture directly, set up TextureGenerationSettings like the ones below, but without compression or mipmaps,
        // and use TextureGenerator to create an imported texture to pull pixel data from to operate on
        // this is slower because its an extra step and uses the importer, but has the benefit of applying importer settings prior to touching the pixels

        // horizontal flip for the pixels to do something
        for (int i = 0; i < rawTex.height / 2; i++)
        {
            int rowIndex = i * rawTex.width;
            for (int j = 0; j < rawTex.width; j++)
            {
                Color32 leftColor = colorArr[rowIndex + j];
                colorArr[rowIndex + j] = colorArr[rowIndex + rawTex.width - j];
                colorArr[rowIndex + rawTex.width - j] = leftColor;
            }
        }

        // can modify the way the new texture is imported, just copying it from the source for this
        TextureGenerationSettings generationSettings = new TextureGenerationSettings(textureImporter.textureType);
        textureImporter.ReadTextureSettings(generationSettings.textureImporterSettings);
        generationSettings.platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
        generationSettings.sourceTextureInformation.containsAlpha = textureImporter.DoesSourceTextureHaveAlpha();
        generationSettings.sourceTextureInformation.height = rawTex.height;
        generationSettings.sourceTextureInformation.width = rawTex.width;

        generationSettings.assetPath = path + "(modified).asset"; // leave empty to prevent asset creation
        generationSettings.enablePostProcessor = false; // enable to run post processes

        var output = TextureGenerator.GenerateTexture(generationSettings, colorArr);

        // now we have a fresh texture in output.texture to work with

        // alternatively can use encoding functions available in ImageConversion or Texture2D to output a PNG/JPG instead of a texture asset with TextureGenerator

        // cleanup
        Object.DestroyImmediate(rawTex);
        Object.DestroyImmediate(output.thumbNail);
        Object.DestroyImmediate(output.output);
    }
}
