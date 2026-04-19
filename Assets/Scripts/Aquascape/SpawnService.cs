using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Aquascape
{
    public sealed class SpawnService : MonoBehaviour
    {
        private const float RuntimePixelsPerUnit = 256f;

        private AquariumWorld world;
        private AquariumConfigData config;
        private ProceduralSpriteLibrary spriteLibrary;
        private AquariumFeedback feedback;
        private FishAgent fishPrefab;
        private TrashAgent trashPrefab;
        private FoodItem foodPrefab;
        private Transform entityRoot;

        public void Initialize(
            AquariumWorld aquariumWorld,
            AquariumConfigData aquariumConfig,
            ProceduralSpriteLibrary library,
            AquariumFeedback aquariumFeedback,
            FishAgent fishTemplate,
            TrashAgent trashTemplate,
            FoodItem foodTemplate,
            Transform runtimeEntityRoot)
        {
            world = aquariumWorld;
            config = aquariumConfig;
            spriteLibrary = library;
            feedback = aquariumFeedback;
            fishPrefab = fishTemplate;
            trashPrefab = trashTemplate;
            foodPrefab = foodTemplate;
            entityRoot = runtimeEntityRoot != null ? runtimeEntityRoot : transform;
        }

        public void QueueSpawn(RuntimeImportDescriptor descriptor)
        {
            StartCoroutine(LoadAndSpawnRoutine(descriptor));
        }

        public void SpawnFood(Vector2 position)
        {
            if (world == null || !world.ContainsPoint(position))
            {
                return;
            }

            FoodItem food;
            if (foodPrefab != null)
            {
                food = Instantiate(foodPrefab, entityRoot);
                food.name = "Food";
            }
            else
            {
                var fallback = new GameObject("Food");
                fallback.transform.SetParent(entityRoot, false);
                food = fallback.AddComponent<FoodItem>();
            }

            food.Initialize(world, config.food, feedback, spriteLibrary, position);
            feedback?.SpawnFoodRipple(position);
        }

        private IEnumerator LoadAndSpawnRoutine(RuntimeImportDescriptor descriptor)
        {
            var readTask = Task.Run(() => File.ReadAllBytes(descriptor.FilePath));
            while (!readTask.IsCompleted)
            {
                yield return null;
            }

            if (readTask.IsFaulted || readTask.IsCanceled)
            {
                Debug.LogWarning($"Failed to read runtime import: {descriptor.FileName}");
                yield break;
            }

            var bytes = readTask.Result;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = descriptor.FileName
            };

            if (!texture.LoadImage(bytes, false))
            {
                Destroy(texture);
                Debug.LogWarning($"Failed to decode PNG file: {descriptor.FileName}");
                yield break;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                RuntimePixelsPerUnit);
            sprite.name = Path.GetFileNameWithoutExtension(descriptor.FileName);

            switch (descriptor.Kind)
            {
                case RuntimeImportKind.Fish:
                    SpawnFish(descriptor, sprite, texture);
                    break;
                case RuntimeImportKind.Trash:
                    SpawnTrash(descriptor, sprite, texture);
                    break;
            }
        }

        private void SpawnFish(RuntimeImportDescriptor descriptor, Sprite sprite, Texture2D texture)
        {
            var profile = config.GetFishProfile(descriptor.TypeId);
            var worldRadius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y) * 0.55f * profile.scale;
            if (!world.TryFindSpawnPosition(worldRadius, out var spawnPosition))
            {
                Debug.LogWarning($"Skipped fish spawn because aquarium is too crowded: {descriptor.FileName}");
                Destroy(sprite);
                Destroy(texture);
                return;
            }

            FishAgent fish;
            if (fishPrefab != null)
            {
                fish = Instantiate(fishPrefab, entityRoot);
                fish.name = $"Fish_{descriptor.TypeId}_{descriptor.Timestamp}";
            }
            else
            {
                var fallback = new GameObject($"Fish_{descriptor.TypeId}_{descriptor.Timestamp}");
                fallback.transform.SetParent(entityRoot, false);
                fallback.AddComponent<CircleCollider2D>().isTrigger = true;
                fallback.AddComponent<SpriteRenderer>();
                fish = fallback.AddComponent<FishAgent>();
            }

            fish.transform.position = spawnPosition;
            fish.PrepareRuntimeVisual(sprite, texture, profile);
            fish.Initialize(world, profile, config.interaction, feedback, worldRadius);

            feedback?.SpawnImportPulse(spawnPosition, false);
        }

        private void SpawnTrash(RuntimeImportDescriptor descriptor, Sprite sprite, Texture2D texture)
        {
            var profile = config.GetTrashProfile(descriptor.TypeId);
            var worldRadius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y) * 0.52f * profile.scale;
            if (!world.TryFindSpawnPosition(worldRadius, out var spawnPosition))
            {
                Debug.LogWarning($"Skipped trash spawn because aquarium is too crowded: {descriptor.FileName}");
                Destroy(sprite);
                Destroy(texture);
                return;
            }

            TrashAgent trash;
            if (trashPrefab != null)
            {
                trash = Instantiate(trashPrefab, entityRoot);
                trash.name = $"Trash_{descriptor.TypeId}_{descriptor.Timestamp}";
            }
            else
            {
                var fallback = new GameObject($"Trash_{descriptor.TypeId}_{descriptor.Timestamp}");
                fallback.transform.SetParent(entityRoot, false);
                fallback.AddComponent<CircleCollider2D>().isTrigger = true;
                fallback.AddComponent<SpriteRenderer>();
                trash = fallback.AddComponent<TrashAgent>();
            }

            trash.transform.position = spawnPosition;
            trash.PrepareRuntimeVisual(sprite, texture, profile);
            trash.Initialize(world, profile, feedback, worldRadius);

            feedback?.SpawnImportPulse(spawnPosition, true);
        }
    }
}
