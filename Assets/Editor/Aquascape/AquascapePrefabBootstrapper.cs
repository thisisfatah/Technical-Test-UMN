#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aquascape.Editor
{
    [InitializeOnLoad]
    public static class AquascapePrefabBootstrapper
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string AquascapeResourcesFolder = "Assets/Resources/Aquascape";
        private const string FishPrefabPath = "Assets/Resources/Aquascape/FishEntity.prefab";
        private const string TrashPrefabPath = "Assets/Resources/Aquascape/TrashEntity.prefab";
        private const string FoodPrefabPath = "Assets/Resources/Aquascape/FoodEntity.prefab";

        static AquascapePrefabBootstrapper()
        {
            EditorApplication.delayCall += () => EnsurePrefabs();
        }

        [MenuItem("Aquascape/Rebuild Template Prefabs")]
        public static void RebuildPrefabs()
        {
            EnsurePrefabs(true);
        }

        private static void EnsurePrefabs(bool force = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureFolder(ResourcesFolder);
            EnsureFolder(AquascapeResourcesFolder);

            CreateFishPrefab(force);
            CreateTrashPrefab(force);
            CreateFoodPrefab(force);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateFishPrefab(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(FishPrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("FishEntity");
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            root.AddComponent<RuntimeSpriteHandle>();
            var agent = root.AddComponent<FishAgent>();

            CreateVisualChild(root.transform, "Visual", 20);
            agent.CaptureAuthoringReferences();

            PrefabUtility.SaveAsPrefabAsset(root, FishPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateTrashPrefab(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(TrashPrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("TrashEntity");
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            root.AddComponent<RuntimeSpriteHandle>();
            var agent = root.AddComponent<TrashAgent>();

            CreateVisualChild(root.transform, "Visual", 14);
            agent.CaptureAuthoringReferences();

            PrefabUtility.SaveAsPrefabAsset(root, TrashPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateFoodPrefab(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(FoodPrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("FoodEntity");
            var food = root.AddComponent<FoodItem>();

            CreateVisualChild(root.transform, "Visual", 30);
            food.CaptureAuthoringReferences();

            PrefabUtility.SaveAsPrefabAsset(root, FoodPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateVisualChild(Transform parent, string childName, int sortingOrder)
        {
            var visual = new GameObject(childName);
            visual.transform.SetParent(parent, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return visual;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
