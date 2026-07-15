#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZombieWar.CameraSystem;

namespace ZombieWar.EditorTools
{
    public static class GameCameraSceneSetup
    {
        private const string MenuPath = "Zombie War/Setup Game Cameras";

        [MenuItem(MenuPath)]
        public static void SetupGameCameras()
        {
            GameObject playerRoot = findPlayerRoot();
            if (playerRoot == null)
            {
                Debug.LogError("[Zombie War] Player not found.");
                return;
            }

            Transform playerCameraTarget = ensureChildTarget(playerRoot.transform, "PlayerCameraTarget", new Vector3(0f, 1.35f, 0f));
            ensureMainCamera();

            Component worldCamera = ensureWorldCamera(
                "CM_WorldOverview",
                new Vector3(0f, 40f, -30f),
                new Vector3(55f, 0f, 0f),
                50f);

            Component playerCamera = ensureCinemachineRig(
                "CM_PlayerFollow",
                playerCameraTarget,
                new Vector3(0f, 8f, -6f),
                new Vector3(45f, 0f, 0f),
                45f);

            GameCameraController controller = ensureCameraController(playerCameraTarget, worldCamera, playerCamera);
            Selection.activeGameObject = controller.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Zombie War] Game cameras setup completed: CM_WorldOverview + CM_PlayerFollow.");
        }

        private static GameObject findPlayerRoot()
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            return tagged != null ? tagged : GameObject.Find("player__root") ?? GameObject.Find("ZW_BoxPlayer");
        }

        private static Transform ensureChildTarget(Transform parent, string childName, Vector3 localPosition)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            GameObject targetObject = new GameObject(childName);
            targetObject.transform.SetParent(parent, false);
            targetObject.transform.localPosition = localPosition;
            Undo.RegisterCreatedObjectUndo(targetObject, "Create Player Camera Target");
            return targetObject.transform;
        }

        private static void ensureMainCamera()
        {
            Camera camera = Camera.main;
            GameObject cameraObject = camera != null ? camera.gameObject : null;
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
            }

            ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
        }

        private static Component ensureWorldCamera(string objectName, Vector3 position, Vector3 eulerRotation, float fieldOfView)
        {
            GameObject cameraObject = GameObject.Find(objectName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create World Overview Camera");
            }

            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.Euler(eulerRotation);

            Component cinemachineCamera = ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (cinemachineCamera != null)
            {
                setFieldOfView(cinemachineCamera, fieldOfView);
            }

            return cinemachineCamera;
        }

        private static Component ensureCinemachineRig(
            string objectName,
            Transform trackingTarget,
            Vector3 followOffset,
            Vector3 eulerRotation,
            float fieldOfView)
        {
            GameObject cameraObject = GameObject.Find(objectName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(objectName);
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Cinemachine Camera");
            }

            cameraObject.transform.rotation = Quaternion.Euler(eulerRotation);

            Component cinemachineCamera = ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            Component follow = ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineFollow, Unity.Cinemachine");
            ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineRotationComposer, Unity.Cinemachine");

            if (trackingTarget != null && cinemachineCamera != null)
            {
                assignTrackingTarget(cinemachineCamera, trackingTarget);
            }

            if (follow != null)
            {
                setFollowOffset(follow, followOffset);
            }

            if (cinemachineCamera != null)
            {
                setFieldOfView(cinemachineCamera, fieldOfView);
            }

            return cinemachineCamera;
        }

        private static GameCameraController ensureCameraController(
            Transform playerTarget,
            Component worldCamera,
            Component playerCamera)
        {
            GameObject controllerObject = GameObject.Find("GameCameraSystem");
            if (controllerObject == null)
            {
                controllerObject = new GameObject("GameCameraSystem");
                Undo.RegisterCreatedObjectUndo(controllerObject, "Create Game Camera System");
            }

            GameCameraController controller = controllerObject.GetComponent<GameCameraController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<GameCameraController>(controllerObject);
            }

            SerializedObject controllerSo = new SerializedObject(controller);
            SerializedProperty playerCamProp = controllerSo.FindProperty("playerFollowCamera");
            if (playerCamProp != null)
            {
                playerCamProp.objectReferenceValue = playerCamera;
            }

            // Older builds had worldOverviewCamera; keep CM_WorldOverview in scene for overview shots.
            SerializedProperty worldCamProp = controllerSo.FindProperty("worldOverviewCamera");
            if (worldCamProp != null)
            {
                worldCamProp.objectReferenceValue = worldCamera;
            }

            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            if (playerTarget != null && playerCamera != null)
            {
                controller.SetPlayerTarget(playerTarget);
            }

            return controller;
        }

        private static Component ensureComponent(GameObject targetObject, string typeName)
        {
            Type componentType = FindType(typeName);
            if (componentType == null)
            {
                Debug.LogError($"[Zombie War] Missing type: {typeName}");
                return null;
            }

            Component component = targetObject.GetComponent(componentType);
            return component != null ? component : Undo.AddComponent(targetObject, componentType);
        }

        private static void assignTrackingTarget(Component cinemachineCamera, Transform target)
        {
            SerializedObject serializedObject = new SerializedObject(cinemachineCamera);
            SerializedProperty targetProperty = serializedObject.FindProperty("Target");
            if (targetProperty == null)
            {
                return;
            }

            SerializedProperty trackingTarget = targetProperty.FindPropertyRelative("TrackingTarget");
            if (trackingTarget != null)
            {
                trackingTarget.objectReferenceValue = target;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void setFollowOffset(Component follow, Vector3 followOffset)
        {
            SerializedObject serializedObject = new SerializedObject(follow);
            SerializedProperty offsetProperty = serializedObject.FindProperty("FollowOffset");
            if (offsetProperty != null)
            {
                offsetProperty.vector3Value = followOffset;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void setFieldOfView(Component cinemachineCamera, float fieldOfView)
        {
            SerializedObject serializedObject = new SerializedObject(cinemachineCamera);
            SerializedProperty lens = serializedObject.FindProperty("Lens");
            if (lens == null)
            {
                return;
            }

            SerializedProperty fov = lens.FindPropertyRelative("FieldOfView");
            if (fov != null)
            {
                fov.floatValue = fieldOfView;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Type FindType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            if (type != null)
            {
                return type;
            }

            string typeName = assemblyQualifiedName.Split(',')[0];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
#endif
