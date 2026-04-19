using UnityEngine;

namespace Aquascape
{
    public sealed class AmbientBubbleSpawner : MonoBehaviour
    {
        private AquariumWorld world;
        private ProceduralSpriteLibrary spriteLibrary;
        private float density;
        private float spawnAccumulator;
        private Transform bubbleRoot;

        public void Initialize(AquariumWorld aquariumWorld, ProceduralSpriteLibrary library, float bubbleDensity, Transform root = null)
        {
            world = aquariumWorld;
            spriteLibrary = library;
            density = Mathf.Max(0.2f, bubbleDensity);
            bubbleRoot = root != null ? root : transform;
        }

        private void Update()
        {
            if (world == null || spriteLibrary == null)
            {
                return;
            }

            spawnAccumulator += Time.deltaTime * Mathf.Lerp(0.6f, 2.2f, Mathf.InverseLerp(0.2f, 2f, density));
            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                SpawnBubble();
            }
        }

        private void SpawnBubble()
        {
            var bubbleObject = new GameObject("AmbientBubble");
            bubbleObject.transform.SetParent(bubbleRoot != null ? bubbleRoot : transform, false);

            var renderer = bubbleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = spriteLibrary.GetCircleSprite("AmbientBubble", Color.white);
            renderer.sortingOrder = -30;
            renderer.color = new Color(0.9019608f, 0.98039216f, 1f, Random.Range(0.09f, 0.22f));

            var bubble = bubbleObject.AddComponent<AmbientBubble>();
            bubble.Initialize(
                world,
                renderer,
                new Vector2(Random.Range(world.BoundsRect.xMin, world.BoundsRect.xMax), world.BoundsRect.yMin - 0.55f),
                Random.Range(0.4f, 0.9f),
                Random.Range(0.08f, 0.18f),
                Random.Range(0.22f, 0.6f));
        }
    }

    internal sealed class AmbientBubble : MonoBehaviour
    {
        private AquariumWorld world;
        private SpriteRenderer spriteRenderer;
        private Vector2 position;
        private float riseSpeed;
        private float swayAmplitude;
        private float swayFrequency;
        private float phase;
        private Vector3 baseScale;

        public void Initialize(AquariumWorld aquariumWorld, SpriteRenderer renderer, Vector2 startPosition, float speed, float scale, float amplitude)
        {
            world = aquariumWorld;
            spriteRenderer = renderer;
            position = startPosition;
            riseSpeed = speed;
            swayAmplitude = amplitude;
            swayFrequency = Random.Range(0.8f, 1.5f);
            phase = Random.Range(0f, Mathf.PI * 2f);
            baseScale = Vector3.one * scale;

            transform.position = new Vector3(position.x, position.y, 0f);
            transform.localScale = baseScale;
        }

        private void Update()
        {
            position.y += riseSpeed * Time.deltaTime;
            position.x += Mathf.Sin((Time.time * swayFrequency) + phase) * swayAmplitude * Time.deltaTime;
            transform.position = new Vector3(position.x, position.y, 0f);

            var alpha = Mathf.InverseLerp(world.BoundsRect.yMax + 0.6f, world.BoundsRect.center.y, position.y);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, Mathf.Clamp01(alpha) * 0.22f);

            var scalePulse = 1f + (Mathf.Sin((Time.time * 2.2f) + phase) * 0.08f);
            transform.localScale = baseScale * scalePulse;

            if (position.y > world.BoundsRect.yMax + 0.8f)
            {
                Destroy(gameObject);
            }
        }
    }
}
