using UnityEngine;

namespace Aquascape
{
    public sealed class FoodItem : MonoBehaviour
    {
        [Header("Template References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int sortingOrder = 30;

        private AquariumWorld world;
        private AquariumFeedback feedback;
        private FoodConfig config;
        private Vector3 anchorPosition;
        private Vector3 baseScale;
        private float age;
        private float phaseOffset;
        private bool consumed;

        public Vector2 Position => anchorPosition;
        public float Radius { get; private set; }

        private void Reset()
        {
            CaptureAuthoringReferences();
        }

        private void Awake()
        {
            CaptureAuthoringReferences();
        }

        public void CaptureAuthoringReferences()
        {
            visualRoot = transform.Find("Visual") ?? transform;
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(AquariumWorld aquariumWorld, FoodConfig foodConfig, AquariumFeedback aquariumFeedback, ProceduralSpriteLibrary spriteLibrary, Vector2 position)
        {
            world = aquariumWorld;
            config = foodConfig;
            feedback = aquariumFeedback;
            anchorPosition = new Vector3(position.x, position.y, 0f);
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);

            CaptureAuthoringReferences();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = spriteLibrary.GetCircleSprite("FoodPellet", Color.white);
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = new Color(0.99215686f, 0.9137255f, 0.57254905f, 1f);

            baseScale = Vector3.one * config.scale;
            transform.position = anchorPosition;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale;
            }
            Radius = 0.28f * config.scale;

            world.RegisterFood(this);
        }

        public void Consume()
        {
            if (consumed)
            {
                return;
            }

            consumed = true;
            feedback?.SpawnFoodBurst(anchorPosition);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            age += Time.deltaTime;
            var bob = Mathf.Sin((age * config.bobFrequency) + phaseOffset) * config.bobAmplitude;
            transform.position = anchorPosition + new Vector3(0f, bob, 0f);

            var pulse = 1f + (Mathf.Sin((age * config.bobFrequency * 1.5f) + phaseOffset) * 0.06f);
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale * pulse;
            }

            var fadeStart = config.lifetimeSeconds * 0.72f;
            var fadeProgress = Mathf.InverseLerp(config.lifetimeSeconds, fadeStart, age);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.99215686f, 0.9137255f, 0.57254905f, fadeProgress);
            }

            if (age >= config.lifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (world != null)
            {
                world.UnregisterFood(this);
            }
        }
    }
}
