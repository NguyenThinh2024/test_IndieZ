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
                Debug.LogError("[Zombie War] Player not found (player__root).");
                return;
            }

            // Keep a slight height offset under player__root for framing.
            Transform followTarget = ensureChildTarget(playerRoot.transform, "PlayerCameraTarget", new Vector3(0f, 1.35f, 0f));
            ensureMainCameraWithBrain();

            // Only one live camera: CM_PlayerFollow → player__root.
            disableObject("CM_WorldOverview");

            Component playerCamera = ensureCinemachineRig(
                "CM_PlayerFollow",
                followTarget,
                new Vector3(0f, 8f, -6f),
                new Vector3(45f, 0f, 0f),
                45f);

            GameCameraController controller = ensureCameraController(followTarget, playerCamera);
            Selection.activeGameObject = controller.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Zombie War] Cinemachine follow ready: CM_PlayerFollow → player__root. Edit Follow Offset / FOV on GameCameraSystem.");
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

        private static void ensureMainCameraWithBrain()
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
            Type brainType = FindType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
            if (brainType != null)
            {
                Behaviour brain = cameraObject.GetComponent(brainType) as Behaviour;
                if (brain != null)
                {
                    brain.enabled = true;
                    EditorUtility.SetDirty(brain);
                }
            }

            camera.fieldOfView = 45f;
        }

        private static void disableObject(string objectName)
        {
            GameObject cameraObject = GameObject.Find(objectName);
            if (cameraObject == null)
            {
                return;
            }

            Type cmType = FindType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (cmType != null)
            {
                Behaviour cm = cameraObject.GetComponent(cmType) as Behaviour;
                if (cm != null)
                {
                    cm.enabled = false;
                    EditorUtility.SetDirty(cm);
                }
            }

            cameraObject.SetActive(false);
            EditorUtility.SetDirty(cameraObject);
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

            cameraObject.SetActive(true);
            cameraObject.transform.rotation = Quaternion.Euler(eulerRotation);

            Component cinemachineCamera = ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            Component follow = ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineFollow, Unity.Cinemachine");
            ensureComponent(cameraObject, "Unity.Cinemachine.CinemachineRotationComposer, Unity.Cinemachine");

            if (cinemachineCamera is Behaviour cmBehaviour)
            {
                cmBehaviour.enabled = true;
            }

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
                setPriority(cinemachineCamera, 10);
            }

            return cinemachineCamera;
        }

        private static GameCameraController ensureCameraController(
            Transform playerTarget,
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
            setObject(controllerSo, "mainCamera", Camera.main);
            setObject(controllerSo, "followTarget", playerTarget);
            setObject(controllerSo, "playerFollowCamera", playerCamera as UnityEngine.Object);
            setVector3(controllerSo, "followOffset", new Vector3(0f, 8f, -6f));
            setFloat(controllerSo, "fieldOfView", 45f);
            setBool(controllerSo, "enableScrollZoom", false);
            setInt(controllerSo, "playerFollowPriority", 10);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            if (playerTarget != null)
            {
                controller.SetPlayerTarget(playerTarget);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void setObject(SerializedObject so, string property, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void setBool(SerializedObject so, string property, bool value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.boolValue = value;
            }
        }

        private static void setFloat(SerializedObject so, string property, float value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.floatValue = value;
            }
        }

        private static void setInt(SerializedObject so, string property, int value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.intValue = value;
            }
        }

        private static void setVector3(SerializedObject so, string property, Vector3 value)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.vector3Value = value;
            }
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

        private static void setPriority(Component cinemachineCamera, int priority)
        {
            SerializedObject serializedObject = new SerializedObject(cinemachineCamera);
            SerializedProperty priorityProperty = serializedObject.FindProperty("Priority");
            if (priorityProperty == null)
            {
                return;
            }

            SerializedProperty enabled = priorityProperty.FindPropertyRelative("Enabled");
            SerializedProperty value = priorityProperty.FindPropertyRelative("Value");
            if (enabled != null)
            {
                enabled.boolValue = true;
            }

            if (value != null)
            {
                value.intValue = priority;
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
