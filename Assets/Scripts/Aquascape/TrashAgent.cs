using System.Collections;
using UnityEngine;

namespace Aquascape
{
    public sealed class TrashAgent : MonoBehaviour, IAquariumOccupant
    {
        [Header("Template References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CircleCollider2D hitCollider;
        [SerializeField] private RuntimeSpriteHandle runtimeSpriteHandle;
        [SerializeField] private int sortingOrder = 14;
        [SerializeField] private float colliderRadiusMultiplier = 0.52f;

        private AquariumWorld world;
        private TrashTypeConfig profile;
        private AquariumFeedback feedback;
        private Vector3 baseScale;
        private Vector2 velocity;
        private Vector2 driftTarget;
        private float driftTimer;
        private float currentSpeed;
        private float swaySeed;
        private bool registered;
        private bool cleaningUp;

        public Vector2 Position => transform.position;
        public float Radius { get; private set; }
        public bool BlocksMovement => !cleaningUp;

        private void Reset()
        {
            CaptureAuthoringReferences();
            if (hitCollider != null)
            {
                hitCollider.isTrigger = true;
            }
        }

        private void Awake()
        {
            CaptureAuthoringReferences();
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        }

        public void CaptureAuthoringReferences()
        {
            visualRoot = transform.Find("Visual") ?? transform;
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            hitCollider ??= GetComponent<CircleCollider2D>();
            runtimeSpriteHandle ??= GetComponent<RuntimeSpriteHandle>();
        }

        public void PrepareRuntimeVisual(Sprite sprite, Texture2D texture, TrashTypeConfig trashProfile)
        {
            CaptureAuthoringReferences();

            if (runtimeSpriteHandle == null)
            {
                runtimeSpriteHandle = gameObject.AddComponent<RuntimeSpriteHandle>();
            }

            runtimeSpriteHandle.Initialize(sprite, texture);

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = sortingOrder;
                spriteRenderer.color = Color.white;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = Vector3.one * trashProfile.scale;
                visualRoot.localRotation = Quaternion.identity;
                baseScale = visualRoot.localScale;
            }

            Radius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y) * colliderRadiusMultiplier * trashProfile.scale;
            if (hitCollider != null)
            {
                hitCollider.radius = Radius;
                hitCollider.isTrigger = true;
                hitCollider.enabled = true;
            }
        }

        public void Initialize(
            AquariumWorld aquariumWorld,
            TrashTypeConfig trashProfile,
            AquariumFeedback aquariumFeedback,
            float worldRadius)
        {
            world = aquariumWorld;
            profile = trashProfile;
            feedback = aquariumFeedback;
            Radius = worldRadius;
            CaptureAuthoringReferences();
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            swaySeed = Random.Range(0f, Mathf.PI * 2f);

            PickNewDriftTarget(true);
            world.RegisterOccupant(this);
            registered = true;
        }

        public void BeginCleanup()
        {
            if (cleaningUp)
            {
                return;
            }

            StartCoroutine(CleanupRoutine());
        }

        private void Update()
        {
            if (cleaningUp || world == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            driftTimer -= deltaTime;
            if (driftTimer <= 0f || Vector2.Distance(Position, driftTarget) < 0.45f)
            {
                PickNewDriftTarget(true);
            }

            var position = Position;
            var desiredDirection = driftTarget - position;
            desiredDirection.y += Mathf.Sin((Time.time * profile.swayFrequency) + swaySeed) * profile.swayAmplitude;
            desiredDirection += world.CalculateSeparation(position, Radius, this, Radius * 2.1f) * profile.avoidanceStrength;
            desiredDirection += world.CalculateBoundaryForce(position, Radius) * 1.8f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                desiredDirection = Random.insideUnitCircle.normalized;
            }

            var smoothing = 1f - Mathf.Exp(-2.8f * deltaTime);
            velocity = Vector2.Lerp(velocity, desiredDirection.normalized * currentSpeed, smoothing);
            position += velocity * deltaTime;
            position = world.ClampInside(position, Radius);
            transform.position = new Vector3(position.x, position.y, 0f);

            var roll = Mathf.Clamp((velocity.x * 18f) + (Mathf.Sin((Time.time * profile.swayFrequency) + swaySeed) * 6f), -18f, 18f);
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.Euler(0f, 0f, roll), 1f - Mathf.Exp(-5f * deltaTime));
            }
        }

        private IEnumerator CleanupRoutine()
        {
            cleaningUp = true;
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            if (registered && world != null)
            {
                world.UnregisterOccupant(this);
                registered = false;
            }

            feedback?.SpawnCleanupPulse(Position);

            var duration = 0.28f;
            var elapsed = 0f;
            var startColor = spriteRenderer.color;
            var startScale = visualRoot != null ? visualRoot.localScale : baseScale;
            var endScale = startScale * 0.7f;
            var startPosition = transform.position;
            var endPosition = startPosition + new Vector3(0f, 0.22f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                if (visualRoot != null)
                {
                    visualRoot.localScale = Vector3.Lerp(startScale, endScale, normalized);
                }
                transform.position = Vector3.Lerp(startPosition, endPosition, normalized);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), normalized);
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void PickNewDriftTarget(bool refreshSpeed)
        {
            driftTarget = world.GetRandomPoint(Radius);
            driftTimer = profile.driftChangeInterval + Random.Range(-0.35f, 0.5f);
            if (refreshSpeed)
            {
                currentSpeed = Random.Range(profile.minSpeed, profile.maxSpeed);
            }
        }

        private void OnDestroy()
        {
            if (registered && world != null)
            {
                world.UnregisterOccupant(this);
            }
        }
    }
}
