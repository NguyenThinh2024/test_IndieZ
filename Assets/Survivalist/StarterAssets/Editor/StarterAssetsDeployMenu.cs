using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarterAssets
{
    // This class needs to be a scriptable object to support dynamic determination of StarterAssets install path
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
        public const string MenuRoot = "Tools/Starter Assets";

        // prefab names
        private const string MainCameraPrefabName = "MainCamera";
        private const string PlayerCapsulePrefabName = "PlayerCapsule";

        // names in hierarchy
        private const string CinemachineVirtualCameraName = "PlayerFollowCamera";

        // tags
        private const string PlayerTag = "Player";
        private const string MainCameraTag = "MainCamera";
        private const string CinemachineTargetTag = "CinemachineTarget";

        // Get the path to the template prefabs 
        private static string StarterAssetsPath => PathToThisFile;

        private static GameObject _cinemachineVirtualCamera;

        /// <summary>
        /// Get the relative root path of the StarterAssets install - works even if user has
        /// moved it within Assets, so long as user does not mess with the internal hierarchy
        /// of the StarterAssets folder
        /// </summary>
        public static string StarterAssetsInstallPath
        {
            get
            {
                string path = PathToThisFile;
                // where this file is relative to install path:
                return path.Substring(0, path.LastIndexOf("StarterAssets"));
            }
        }

        private static string PathToThisFile
        {
            get
            {
                var dummy = CreateInstance<StarterAssetsDeployMenu>();
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(dummy));
                DestroyImmediate(dummy);
                return path.Substring(0, path.LastIndexOf("/Editor/StarterAssetsDeployMenu.cs"));
            }
        }

        /// <summary>
        /// Deletes the scripting define set by the Package Checker.
        /// See Assets/Editor/PackageChecker/PackageChecker.cs for more information
        /// </summary>
        [MenuItem(MenuRoot + "/Reinstall Dependencies", false)]
        static void ResetPackageChecker()
        {
            ScriptingDefineUtils.RemoveScriptingDefine(PackageChecker.PackageCheckerScriptingDefine);
        }

#if STARTER_ASSETS_PACKAGES_CHECKED
        private static void CheckCameras(string prefabPath, Transform targetParent)
        {
            CheckMainCamera(prefabPath);

            GameObject vcam = GameObject.Find(CinemachineVirtualCameraName);

            if (!vcam)
            {
                HandleInstantiatingPrefab(StarterAssetsPath + prefabPath,
                    CinemachineVirtualCameraName,
                    out GameObject vcamPrefab);
                _cinemachineVirtualCamera = vcamPrefab;
            }
            else
            {
                _cinemachineVirtualCamera = vcam;
            }

            GameObject[] targets = GameObject.FindGameObjectsWithTag(CinemachineTargetTag);
            GameObject target = targets.FirstOrDefault(t => t.transform.IsChildOf(targetParent));
            if (target == null)
            {
                target = new GameObject("PlayerCameraRoot");
                target.transform.SetParent(targetParent);
                target.transform.localPosition = new Vector3(0f, 1.375f, 0f);
                target.tag = CinemachineTargetTag;
                Undo.RegisterCreatedObjectUndo(target, "Created new cinemachine target");
            }
            CheckVirtualCameraFollowReference(target, _cinemachineVirtualCamera);
        }

        private static void CheckMainCamera(string prefabPath)
        {
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag(MainCameraTag);

            if (mainCameras.Length < 1)
            {
                // if there are no MainCameras, add one
                HandleInstantiatingPrefab(StarterAssetsPath + prefabPath, MainCameraPrefabName,
                    out _);
            }
            else
            {
                Type brainType = FindCinemachineType("Cinemachine.CinemachineBrain");
                if (brainType != null && mainCameras[0].GetComponent(brainType) == null)
                {
                    mainCameras[0].AddComponent(brainType);
                }
            }
        }

        private static void CheckVirtualCameraFollowReference(GameObject target,
            GameObject cinemachineVirtualCamera)
        {
            Component cinemachineComponent = GetCinemachineCameraComponent(cinemachineVirtualCamera);
            if (cinemachineComponent == null)
            {
                Debug.LogWarning("Starter Assets could not find a Cinemachine camera component on the virtual camera prefab.", cinemachineVirtualCamera);
                return;
            }

            var serializedObject = new SerializedObject(cinemachineComponent);
            var serializedProperty = serializedObject.FindProperty("m_Follow") ?? serializedObject.FindProperty("Follow");
            if (serializedProperty == null)
            {
                serializedProperty = serializedObject.FindProperty("m_TrackingTarget") ?? serializedObject.FindProperty("TrackingTarget");
            }

            if (serializedProperty == null)
            {
                Debug.LogWarning("Starter Assets could not find a Cinemachine follow target property.", cinemachineVirtualCamera);
                return;
            }

            serializedProperty.objectReferenceValue = target.transform;
            serializedObject.ApplyModifiedProperties();
        }

        private static Component GetCinemachineCameraComponent(GameObject cameraObject)
        {
            Type virtualCameraType = FindCinemachineType("Cinemachine.CinemachineVirtualCamera");
            if (virtualCameraType != null)
            {
                Component virtualCamera = cameraObject.GetComponent(virtualCameraType);
                if (virtualCamera != null)
                {
                    return virtualCamera;
                }
            }

            Type cameraType = FindCinemachineType("Cinemachine.CinemachineCamera");
            return cameraType != null ? cameraObject.GetComponent(cameraType) : null;
        }

        private static Type FindCinemachineType(string typeName)
        {
            Type type = Type.GetType($"{typeName}, Unity.Cinemachine");
            if (type != null)
            {
                return type;
            }

            type = Type.GetType($"{typeName}, Cinemachine");
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void HandleInstantiatingPrefab(string path, string prefabName, out GameObject prefab)
        {
            prefab = (GameObject) PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{path}{prefabName}.prefab"));
            Undo.RegisterCreatedObjectUndo(prefab, "Instantiate Starter Asset Prefab");

            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localEulerAngles = Vector3.zero;
            prefab.transform.localScale = Vector3.one;
        }
#endif
    }
}