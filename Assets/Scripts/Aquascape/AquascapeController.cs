using System.IO;
using UnityEngine;

namespace Aquascape
{
    [DisallowMultipleComponent]
    public sealed class AquascapeController : MonoBehaviour
    {
        private const string FishPrefabResourcePath = "Aquascape/FishEntity";
        private const string TrashPrefabResourcePath = "Aquascape/TrashEntity";
        private const string FoodPrefabResourcePath = "Aquascape/FoodEntity";

        [Header("Scene References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform entityRoot;
        [SerializeField] private Transform effectsRoot;

        [Header("Aquarium Layout")]
        [SerializeField] private Vector2 aquariumCenter = new(0f, 0f);
        [SerializeField] private Vector2 aquariumSize = new(16f, 9f);
        [SerializeField] private float cameraHalfHeight = 4.5f;
        [SerializeField] private float spawnPadding = 0.3f;
        [SerializeField] private int spawnAttempts = 40;

        [Header("Ambient Effects")]
        [SerializeField] private float bubbleDensity = 1f;

        [Header("Prefab Templates")]
        [SerializeField] private FishAgent fishPrefab;
        [SerializeField] private TrashAgent trashPrefab;
        [SerializeField] private FoodItem foodPrefab;
        [SerializeField] private bool loadPrefabsFromResources = true;

        [Header("Runtime Options")]
        [SerializeField] private bool autoSeedRuntimeImports = true;
        [SerializeField] private bool showHud = true;

        [Header("Editor Preview")]
        [SerializeField] private bool syncCameraInEditor = true;

        private ProceduralSpriteLibrary spriteLibrary;
        private AquariumFeedback feedback;
        private AquariumWorld world;
        private SpawnService spawnService;
        private RuntimeAssetScanner scanner;
        private PlayerInteractionController interaction;
        private AquascapeHud hud;
        private AmbientBubbleSpawner bubbleSpawner;
        private bool initialized;

        public Rect AquariumRect => new(aquariumCenter - (aquariumSize * 0.5f), aquariumSize);
        public float SpawnPadding => Mathf.Max(0f, spawnPadding);
        public int SpawnAttempts => Mathf.Max(8, spawnAttempts);

        private void Reset()
        {
            EnsureReferences();
            ApplyEditorPreview();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureReferences();
                ApplyEditorPreview();
            }
        }

        private void Awake()
        {
            EnsureReferences();
            ConfigureCamera();
        }

        private void Start()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Application.runInBackground = true;
            Application.targetFrameRate = 120;

            var rootPath = AquariumPathResolver.GetRootPath();
            var config = AquariumConfigLoader.LoadOrCreate(rootPath);
            var paths = AquariumPathResolver.Resolve(rootPath, config.scanner.folderName);
            Directory.CreateDirectory(paths.RuntimeImportsPath);

            if (autoSeedRuntimeImports)
            {
                SeedRuntimeImportsIfNeeded(paths);
            }

            ConfigureCamera();
            world.Initialize(AquariumRect, SpawnPadding, SpawnAttempts);
            feedback.Initialize(spriteLibrary, effectsRoot);
            spawnService.Initialize(world, config, spriteLibrary, feedback, fishPrefab, trashPrefab, foodPrefab, entityRoot);
            interaction.Initialize(targetCamera != null ? targetCamera : Camera.main, world, spawnService);
            scanner.Initialize(paths.RuntimeImportsPath, config.scanner.pollIntervalSeconds, spawnService);
            scanner.Begin();
            bubbleSpawner.Initialize(world, spriteLibrary, Mathf.Max(0.2f, bubbleDensity), effectsRoot);
            hud.Initialize(world, paths);
            hud.enabled = showHud;
        }

        private void EnsureReferences()
        {
            targetCamera ??= Camera.main;
            targetCamera ??= FindFirstObjectByType<Camera>();
            entityRoot = EnsureChild(entityRoot, "Entities");
            effectsRoot = EnsureChild(effectsRoot, "Effects");

            spriteLibrary = GetOrAddComponent<ProceduralSpriteLibrary>();
            feedback = GetOrAddComponent<AquariumFeedback>();
            world = GetOrAddComponent<AquariumWorld>();
            spawnService = GetOrAddComponent<SpawnService>();
            scanner = GetOrAddComponent<RuntimeAssetScanner>();
            interaction = GetOrAddComponent<PlayerInteractionController>();
            hud = GetOrAddComponent<AquascapeHud>();
            bubbleSpawner = GetOrAddComponent<AmbientBubbleSpawner>();

            ResolvePrefabs();
        }

        private void ResolvePrefabs()
        {
            if (!loadPrefabsFromResources)
            {
                return;
            }

            fishPrefab ??= Resources.Load<FishAgent>(FishPrefabResourcePath);
            trashPrefab ??= Resources.Load<TrashAgent>(TrashPrefabResourcePath);
            foodPrefab ??= Resources.Load<FoodItem>(FoodPrefabResourcePath);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            if (TryGetComponent<T>(out var existing))
            {
                return existing;
            }

            return gameObject.AddComponent<T>();
        }

        private Transform EnsureChild(Transform current, string childName)
        {
            if (current != null)
            {
                return current;
            }

            var child = transform.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            return childObject.transform;
        }

        private void ApplyEditorPreview()
        {
            if (syncCameraInEditor)
            {
                ConfigureCamera();
            }
        }

        [ContextMenu("Sync Camera Preview")]
        public void SyncCameraPreview()
        {
            EnsureReferences();
            ConfigureCamera();
        }

        [ContextMenu("Fit Camera To Aquarium Height")]
        public void FitCameraToAquariumHeight()
        {
            cameraHalfHeight = Mathf.Max(0.1f, aquariumSize.y * 0.5f);
            EnsureReferences();
            ConfigureCamera();
        }

        [ContextMenu("Apply Full Editor Preview")]
        public void ApplyFullEditorPreview()
        {
            EnsureReferences();
            ApplyEditorPreview();
        }

        private void ConfigureCamera()
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = cameraHalfHeight;

            var targetTransform = targetCamera.transform;
            targetTransform.position = new Vector3(aquariumCenter.x, aquariumCenter.y, -10f);
            targetTransform.rotation = Quaternion.identity;
        }

        private void SeedRuntimeImportsIfNeeded(AquariumPaths paths)
        {
            if (HasAnyFiles(paths.RuntimeImportsPath) || !Directory.Exists(paths.SeedImportsPath))
            {
                return;
            }

            var seedFiles = Directory.GetFiles(paths.SeedImportsPath, "*.png", SearchOption.AllDirectories);
            for (var index = 0; index < seedFiles.Length; index++)
            {
                var sourcePath = seedFiles[index];
                var relativePath = Path.GetRelativePath(paths.SeedImportsPath, sourcePath);
                var destinationPath = Path.Combine(paths.RuntimeImportsPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? paths.RuntimeImportsPath);
                File.Copy(sourcePath, destinationPath, true);
            }
        }

        private bool HasAnyFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return false;
            }

            using var enumerator = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories).GetEnumerator();
            return enumerator.MoveNext();
        }

        private void OnDrawGizmosSelected()
        {
            var rect = AquariumRect;
            Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
            Gizmos.DrawWireCube(rect.center, rect.size);

            Gizmos.color = new Color(1f, 1f, 1f, 0.12f);
            Gizmos.DrawCube(rect.center, rect.size);
        }
    }
}
