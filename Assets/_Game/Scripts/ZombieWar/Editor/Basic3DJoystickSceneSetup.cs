#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieWar.Player;
using ZombieWar.Shooting;
namespace ZombieWar.EditorTools
{
    public static class Basic3DJoystickSceneSetup
    {
        private const string MenuPath = "Zombie War/Setup Basic 3D Joystick Scene";

        [MenuItem(MenuPath)]
        public static void SetupScene()
        {
            GameObject plane = CreatePlane();
            GameObject player = CreatePlayer();
            Camera mainCamera = CreateMainCamera();
            CreateJoystickCanvas(player);
            CreateShootingSetup(player);
            EnsureEventSystem();

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Zombie War] Basic scene setup done. Run 'Zombie War/Setup Game Cameras' to add overview + player cameras.");
        }

        private static GameObject CreatePlane()
        {
            GameObject plane = GameObject.Find("ZW_GroundPlane");
            if (plane != null)
            {
                return plane;
            }

            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "ZW_GroundPlane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(4f, 1f, 4f);

            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial("ZW_Ground_Mat", new Color(0.25f, 0.45f, 0.25f));
            }

            Undo.RegisterCreatedObjectUndo(plane, "Create Zombie War Ground Plane");
            return plane;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = GameObject.Find("ZW_BoxPlayer");
            if (player != null)
            {
                return player;
            }

            player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "ZW_BoxPlayer";
            player.transform.position = new Vector3(0f, 0.6f, 0f);
            player.transform.localScale = new Vector3(1f, 1.2f, 1f);
            player.tag = "Player";

            Renderer renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial("ZW_Player_Mat", new Color(0.1f, 0.45f, 1f));
            }

            BoxCollider boxCollider = player.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.center = Vector3.zero;
                boxCollider.size = Vector3.one;
            }

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("visualRoot").objectReferenceValue = player.transform;
            movementSo.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(player, "Create Zombie War Box Player");
            return player;
        }

        private static Camera CreateMainCamera()
        {
            Camera camera = Camera.main;
            GameObject cameraObject;
            if (camera != null)
            {
                cameraObject = camera.gameObject;
            }
            else
            {
                cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
            }

            camera.transform.position = new Vector3(0f, 12f, -8f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;

            EnsureCinemachineBrain(cameraObject);
            return camera;
        }

        private static void EnsureCinemachineBrain(GameObject cameraObject)
        {
            Type brainType = FindType("Cinemachine.CinemachineBrain, Unity.Cinemachine");
            if (brainType == null)
            {
                brainType = FindType("Cinemachine.CinemachineBrain, Cinemachine");
            }

            if (brainType == null || cameraObject.GetComponent(brainType) != null)
            {
                return;
            }

            cameraObject.AddComponent(brainType);
        }

        private static void CreateJoystickCanvas(GameObject player)
        {
            GameObject canvasObject = GameObject.Find("ZW_JoystickCanvas");
            Canvas canvas;
            if (canvasObject == null)
            {
                canvasObject = new GameObject("ZW_JoystickCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;

                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Joystick Canvas");
            }
            else
            {
                canvas = canvasObject.GetComponent<Canvas>();
            }

            GameObject joystickObject = GameObject.Find("ZW_FixedJoystick");
            if (joystickObject == null)
            {
                joystickObject = CreateFixedJoystick(canvas.transform);
            }

            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("joystick").objectReferenceValue = joystickObject.GetComponent<Joystick>();
            movementSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateFixedJoystick(Transform parent)
        {
            GameObject root = new GameObject("ZW_FixedJoystick", typeof(RectTransform), typeof(FixedJoystick));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(190f, 190f);
            rootRect.sizeDelta = new Vector2(220f, 220f);

            GameObject background = CreateUiImage("Background", root.transform, new Color(0f, 0f, 0f, 0.35f));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject handle = CreateUiImage("Handle", background.transform, new Color(1f, 1f, 1f, 0.65f));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(90f, 90f);
            handleRect.anchoredPosition = Vector2.zero;

            FixedJoystick fixedJoystick = root.GetComponent<FixedJoystick>();
            SerializedObject joystickSo = new SerializedObject(fixedJoystick);
            joystickSo.FindProperty("background").objectReferenceValue = backgroundRect;
            joystickSo.FindProperty("handle").objectReferenceValue = handleRect;
            joystickSo.FindProperty("handleRange").floatValue = 1f;
            joystickSo.FindProperty("deadZone").floatValue = 0.05f;
            joystickSo.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Fixed Joystick");
            return root;
        }

        private static GameObject CreateUiImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return imageObject;
        }

        private static void CreateShootingSetup(GameObject player)
        {
            Transform firePoint = CreateFirePoint(player.transform);
            ProjectilePool projectilePool = CreateProjectilePool();
            PlayerSimpleShooter shooter = player.GetComponent<PlayerSimpleShooter>();
            if (shooter == null)
            {
                shooter = player.AddComponent<PlayerSimpleShooter>();
            }

            SerializedObject shooterSo = new SerializedObject(shooter);
            shooterSo.FindProperty("projectilePool").objectReferenceValue = projectilePool;
            shooterSo.FindProperty("firePoint").objectReferenceValue = firePoint;
            shooterSo.FindProperty("projectileSpeed").floatValue = 14f;
            shooterSo.FindProperty("cooldown").floatValue = 0.2f;
            shooterSo.ApplyModifiedPropertiesWithoutUndo();

            CreateShootButton(shooter);
        }

        private static Transform CreateFirePoint(Transform player)
        {
            Transform existing = player.Find("ZW_FirePoint");
            if (existing != null)
            {
                return existing;
            }

            GameObject firePointObject = new GameObject("ZW_FirePoint");
            firePointObject.transform.SetParent(player, false);
            firePointObject.transform.localPosition = new Vector3(0f, 0.2f, 0.75f);
            firePointObject.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(firePointObject, "Create Shoot Fire Point");
            return firePointObject.transform;
        }

        private static ProjectilePool CreateProjectilePool()
        {
            GameObject poolObject = GameObject.Find("ZW_ProjectilePool");
            if (poolObject == null)
            {
                poolObject = new GameObject("ZW_ProjectilePool", typeof(ProjectilePool));
                Undo.RegisterCreatedObjectUndo(poolObject, "Create Projectile Pool");
            }

            ProjectilePool pool = poolObject.GetComponent<ProjectilePool>();
            SimpleProjectile[] projectiles = new SimpleProjectile[12];
            for (int i = 0; i < projectiles.Length; i++)
            {
                projectiles[i] = CreateProjectile(poolObject.transform, i);
            }

            SerializedObject poolSo = new SerializedObject(pool);
            SerializedProperty projectilesProperty = poolSo.FindProperty("projectiles");
            projectilesProperty.arraySize = projectiles.Length;
            for (int i = 0; i < projectiles.Length; i++)
            {
                projectilesProperty.GetArrayElementAtIndex(i).objectReferenceValue = projectiles[i];
            }
            poolSo.ApplyModifiedPropertiesWithoutUndo();
            return pool;
        }

        private static SimpleProjectile CreateProjectile(Transform parent, int index)
        {
            string projectileName = $"ZW_Projectile_{index:00}";
            Transform existing = parent.Find(projectileName);
            if (existing != null)
            {
                return existing.GetComponent<SimpleProjectile>();
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = projectileName;
            projectileObject.transform.SetParent(parent, false);
            projectileObject.transform.localScale = Vector3.one * 0.18f;
            projectileObject.SetActive(false);

            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Collider projectileCollider = projectileObject.GetComponent<Collider>();
            SimpleProjectile projectile = projectileObject.AddComponent<SimpleProjectile>();
            SerializedObject projectileSo = new SerializedObject(projectile);
            projectileSo.FindProperty("body").objectReferenceValue = body;
            projectileSo.FindProperty("projectileCollider").objectReferenceValue = projectileCollider;
            projectileSo.FindProperty("lifeTime").floatValue = 2f;
            projectileSo.ApplyModifiedPropertiesWithoutUndo();

            Renderer renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial("ZW_Projectile_Mat", new Color(1f, 0.75f, 0.1f));
            }

            Undo.RegisterCreatedObjectUndo(projectileObject, "Create Projectile Pool Item");
            return projectile;
        }

        private static void CreateShootButton(PlayerSimpleShooter shooter)
        {
            GameObject canvasObject = GameObject.Find("ZW_JoystickCanvas");
            if (canvasObject == null)
            {
                return;
            }

            GameObject buttonObject = GameObject.Find("ZW_ShootButton");
            if (buttonObject == null)
            {
                buttonObject = new GameObject("ZW_ShootButton", typeof(RectTransform), typeof(Image), typeof(ShootButtonInput));
                buttonObject.transform.SetParent(canvasObject.transform, false);
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(1f, 0f);
                buttonRect.anchorMax = new Vector2(1f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(-190f, 190f);
                buttonRect.sizeDelta = new Vector2(170f, 170f);

                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(1f, 0.25f, 0.15f, 0.75f);

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                Text label = labelObject.GetComponent<Text>();
                label.text = "SHOOT";
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.fontSize = 32;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                Undo.RegisterCreatedObjectUndo(buttonObject, "Create Shoot Button");
            }

            ShootButtonInput shootInput = buttonObject.GetComponent<ShootButtonInput>();
            SerializedObject shootInputSo = new SerializedObject(shootInput);
            shootInputSo.FindProperty("shooter").objectReferenceValue = shooter;
            shootInputSo.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"Assets/_Game/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Type FindType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(assemblyQualifiedName.Split(',')[0]);
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




