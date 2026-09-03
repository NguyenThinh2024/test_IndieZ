#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Thinh.Base.Editor
{
    public static class TemplatePackageExporter
    {
        private const string PackageRoot = "Assets/_SDK";
        private const string ExportDirectoryName = "Exports";
        private const string PackageFileName = "ThinhTemplate.unitypackage";
        private const string FullPackageFileName = "ThinhTemplateFull.unitypackage";
        private const string PuzzleGamePackageFileName = "ThinhTemplatePuzzleGame.unitypackage";

        private static readonly string[] FullPackageRoots =
        {
            "Assets/_SDK",
            "Assets/_Game/Scripts",
            "Assets/_Game/Prefabs",
            "Assets/Plugins/Demigiant",
            "Assets/Plugins/Sirenix",
            "Assets/Plugins/CW",
            "Assets/_ThirdPartty/ShinyEffectForUGUI",
            "Assets/TextMesh Pro"
        };

        private static readonly string[] PuzzleGamePackageRoots =
        {
            "Assets/_SDK",
            "Assets/_Game",
            "Assets/Plugins/Demigiant",
            "Assets/Plugins/Sirenix",
            "Assets/Plugins/CW",
            "Assets/Plugins/IngameDebugConsole",
            "Assets/JMO Assets/Toony Colors Pro",
            "Assets/_ThirdPartty/ShinyEffectForUGUI",
            "Assets/TextMesh Pro",
            "Packages/manifest.json",
            "ProjectSettings/ProjectVersion.txt"
        };

        [MenuItem("Tools/Thinh Template/Export Package")]
        public static void ExportPackage()
        {
            ExportPaths(new[] { PackageRoot }, PackageFileName);
        }

        [MenuItem("Tools/Thinh Template/Export Full Package")]
        public static void ExportFullPackage()
        {
            ExportPackageWithDependencies(FullPackageRoots, FullPackageFileName);
        }

        [MenuItem("Tools/Thinh Template/Export Puzzle Game Package")]
        public static void ExportPuzzleGamePackage()
        {
            ExportPackageWithDependencies(PuzzleGamePackageRoots, PuzzleGamePackageFileName);
        }

        private static void ExportPackageWithDependencies(string[] packageRoots, string packageFileName)
        {
            string[] existingRoots = packageRoots
                .Where(AssetDatabase.AssetPathExists)
                .ToArray();

            string[] dependencies = AssetDatabase
                .GetDependencies(existingRoots, true)
                .Where(IsExportableAssetPath)
                .Distinct()
                .ToArray();

            ExportPaths(dependencies, packageFileName);
        }

        private static bool IsExportableAssetPath(string path)
        {
            return !path.StartsWith("Assets/VoodooPackages") &&
                   !path.EndsWith(".unitypackage") &&
                   !path.Contains("/Library/") &&
                   !path.Contains("/Temp/");
        }

        private static void ExportPaths(string[] assetPaths, string packageFileName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[Template Package] Project root could not be resolved.");
                return;
            }

            string exportDirectory = Path.Combine(projectRoot, ExportDirectoryName);
            Directory.CreateDirectory(exportDirectory);

            string outputPath = Path.Combine(exportDirectory, packageFileName);
            AssetDatabase.ExportPackage(
                assetPaths,
                outputPath,
                ExportPackageOptions.Recurse);

            Debug.Log($"[Template Package] Exported to: {outputPath}");
        }
    }
}
#endif
