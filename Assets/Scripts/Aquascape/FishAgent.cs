using UnityEngine;

namespace Aquascape
{
    public sealed class FishAgent : MonoBehaviour, IAquariumOccupant
    {
        [Header("Template References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CircleCollider2D hitCollider;
        [SerializeField] private RuntimeSpriteHandle runtimeSpriteHandle;
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private float colliderRadiusMultiplier = 0.55f;

        private enum FishState
        {
            Wander,
            SeekFood,
            Flee
        }

        private AquariumWorld world;
        private FishTypeConfig profile;
        private InteractionConfig interaction;
        private AquariumFeedback feedback;
        private Vector3 baseScale;
        private Vector2 velocity;
        private Vector2 wanderTarget;
        private Vector2 fleeDirection;
        private FoodItem targetFood;
        private float hunger;
        private float cooldownTimer;
        private float wanderTimer;
        private float currentCruiseSpeed;
        private float fleeTimer;
        private float fearVisualTimer;
        private float animationSeed;
        private bool registered;

        public Vector2 Position => transform.position;
        public float Radius { get; private set; }
        public bool BlocksMovement => true;

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

        public void PrepareRuntimeVisual(Sprite sprite, Texture2D texture, FishTypeConfig fishProfile)
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
                visualRoot.localScale = Vector3.one * fishProfile.scale;
                visualRoot.localRotation = Quaternion.identity;
                baseScale = visualRoot.localScale;
            }

            var spriteRadius = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y) * colliderRadiusMultiplier * fishProfile.scale;
            Radius = spriteRadius;
            if (hitCollider != null)
            {
                hitCollider.radius = Radius;
                hitCollider.isTrigger = true;
                hitCollider.enabled = true;
            }
        }

        public void Initialize(
            AquariumWorld aquariumWorld,
            FishTypeConfig fishProfile,
            InteractionConfig interactionConfig,
            AquariumFeedback aquariumFeedback,
            float worldRadius)
        {
            world = aquariumWorld;
            profile = fishProfile;
            interaction = interactionConfig;
            feedback = aquariumFeedback;
            Radius = worldRadius;
            CaptureAuthoringReferences();
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            hunger = Random.Range(35f, 95f);
            animationSeed = Random.Range(0f, Mathf.PI * 2f);

            PickNewWanderTarget(true);
            world.RegisterOccupant(this);
            registered = true;
        }

        public void TriggerFlee(Vector2 sourcePosition)
        {
            var direction = Position - sourcePosition;
            fleeDirection = direction.sqrMagnitude < 0.001f ? Random.insideUnitCircle.normalized : direction.normalized;
            fleeTimer = interaction.fleeDurationSeconds;
            fearVisualTimer = 0.24f;
            feedback?.SpawnFearPulse(Position);
        }

        private void Update()
        {
            if (world == null)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            UpdateNeeds(deltaTime);
            UpdateState(deltaTime);
            Move(deltaTime);
            UpdateVisuals(deltaTime);
        }

        private void UpdateNeeds(float deltaTime)
        {
            if (fleeTimer > 0f)
            {
                fleeTimer = Mathf.Max(0f, fleeTimer - deltaTime);
            }

            if (fearVisualTimer > 0f)
            {
                fearVisualTimer = Mathf.Max(0f, fearVisualTimer - deltaTime);
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
                hunger = 100f;
                return;
            }

            hunger = Mathf.Max(0f, hunger - (profile.hungerDrainPerSecond * deltaTime));
        }

        private FishState CurrentState { get; set; }

        private void UpdateState(float deltaTime)
        {
            if (fleeTimer > 0f)
            {
                CurrentState = FishState.Flee;
                targetFood = null;
                return;
            }

            if (hunger <= 0f)
            {
                targetFood = world.FindNearestFood(Position, profile.detectionRadius);
                CurrentState = targetFood != null ? FishState.SeekFood : FishState.Wander;
                return;
            }

            targetFood = null;
            CurrentState = FishState.Wander;

            wanderTimer -= deltaTime;
            if (wanderTimer <= 0f || Vector2.Distance(Position, wanderTarget) < 0.4f)
            {
                PickNewWanderTarget(true);
            }
        }

        private void Move(float deltaTime)
        {
            var position = Position;
            var desiredDirection = Vector2.zero;
            var targetSpeed = currentCruiseSpeed;

            switch (CurrentState)
            {
                case FishState.Flee:
                    desiredDirection = fleeDirection;
                    targetSpeed = profile.maxSpeed * interaction.fleeSpeedMultiplier;
                    break;
                case FishState.SeekFood:
                    if (targetFood == null)
                    {
                        PickNewWanderTarget(true);
                        desiredDirection = wanderTarget - position;
                        targetSpeed = currentCruiseSpeed;
                        break;
                    }

                    if ((targetFood.Position - position).sqrMagnitude <= Mathf.Pow(Radius + targetFood.Radius + 0.18f, 2f))
                    {
                        targetFood.Consume();
                        targetFood = null;
                        hunger = 100f;
                        cooldownTimer = profile.hungerCooldown;
                        PickNewWanderTarget(true);
                        desiredDirection = wanderTarget - position;
                        targetSpeed = currentCruiseSpeed;
                        break;
                    }

                    desiredDirection = targetFood.Position - position;
                    targetSpeed = profile.maxSpeed;
                    break;
                default:
                    desiredDirection = wanderTarget - position;
                    targetSpeed = currentCruiseSpeed;
                    break;
            }

            desiredDirection += world.CalculateSeparation(position, Radius, this, Radius * 2.4f) * profile.avoidanceStrength;
            desiredDirection += world.CalculateBoundaryForce(position, Radius) * 2.2f;

            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                desiredDirection = Random.insideUnitCircle.normalized;
            }

            var smoothing = 1f - Mathf.Exp(-profile.turnResponsiveness * deltaTime);
            velocity = Vector2.Lerp(velocity, desiredDirection.normalized * targetSpeed, smoothing);

            position += velocity * deltaTime;
            position = world.ClampInside(position, Radius);
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        private void UpdateVisuals(float deltaTime)
        {
            var visualStrength = fearVisualTimer > 0f ? Mathf.InverseLerp(0f, 0.24f, fearVisualTimer) : 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(Color.white, new Color(0.8392157f, 0.972549f, 1f, 1f), visualStrength);
            }

            var pulse = 1f + (visualStrength * 0.14f);
            var idleSway = Mathf.Sin((Time.time * 4.5f) + animationSeed) * 2.8f;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale * pulse;
            }

            if (spriteRenderer != null && Mathf.Abs(velocity.x) > 0.05f)
            {
                spriteRenderer.flipX = velocity.x < 0f;
            }

            var targetAngle = Mathf.Clamp((velocity.y * 9f) + idleSway, -16f, 16f);
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.Euler(0f, 0f, targetAngle), 1f - Mathf.Exp(-6f * deltaTime));
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }
        }

        private void PickNewWanderTarget(bool refreshSpeed)
        {
            wanderTarget = world.GetRandomPoint(Radius);
            wanderTimer = profile.wanderRetargetSeconds + Random.Range(-0.35f, 0.45f);
            if (refreshSpeed)
            {
                currentCruiseSpeed = Random.Range(profile.minSpeed, profile.maxSpeed);
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
