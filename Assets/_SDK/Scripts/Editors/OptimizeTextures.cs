#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class OptimizeTextures : EditorWindow
{
    private string folderPath = "Assets/";

    [MenuItem("Tools/Optimize Sprites and Textures (By Size)")]
    public static void ShowWindow()
    {
        GetWindow<OptimizeTextures>("Optimize Textures");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Optimization Tool (By Size)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("Folder Path:");
        folderPath = EditorGUILayout.TextField(folderPath);

        if (GUILayout.Button("Select Folder"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets/", "");
            if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
            {
                folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Optimize Textures"))
        {
            OptimizeTexturesInFolder(folderPath);
        }
    }

    private void OptimizeTexturesInFolder(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Folder path is empty!");
            return;
        }

        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(s =>
                s.EndsWith(".png") ||
                s.EndsWith(".jpg") ||
                s.EndsWith(".jpeg") ||
                s.EndsWith(".psd"))
            .ToArray();

        int optimizedCount = 0;

        foreach (string file in files)
        {
            string assetPath = file.Replace(Application.dataPath, "Assets");
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) continue;

            int maxSide = Mathf.Max(tex.width, tex.height);

            // ===== Common settings =====
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;

            // ===== Android settings =====
            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true
            };

            if (maxSide <= 128)
            {
                android.maxTextureSize = 128;
                android.format = TextureImporterFormat.ASTC_5x5;
                android.compressionQuality = 10;
            }
            else if (maxSide <= 256)
            {
                android.maxTextureSize = 256;
                android.format = TextureImporterFormat.ASTC_5x5;
                android.compressionQuality = 15;
            }
            else if (maxSide <= 512)
            {
                android.maxTextureSize = 512;
                android.format = TextureImporterFormat.ASTC_5x5;
                android.compressionQuality = 20;
            }
            else if (maxSide <= 1024)
            {
                android.maxTextureSize = 1024;
                android.format = TextureImporterFormat.ASTC_5x5;
                android.compressionQuality = 25;
            }
            else
            {
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_4x4;
                android.compressionQuality = 30;
            }

            importer.SetPlatformTextureSettings(android);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            optimizedCount++;
        }

        Debug.Log($"Optimized {optimizedCount} textures by size in {path}");
    }
}
#endif
