using UnityEngine;

namespace Aquascape
{
    public sealed class AquariumFeedback : MonoBehaviour
    {
        private ProceduralSpriteLibrary spriteLibrary;
        private Transform effectRoot;

        public void Initialize(ProceduralSpriteLibrary library, Transform root = null)
        {
            spriteLibrary = library;
            effectRoot = root != null ? root : transform;
        }

        public void SpawnFoodRipple(Vector2 position)
        {
            SpawnPulse("FoodRippleInner", position, new Vector3(0.12f, 0.12f, 1f), new Vector3(0.65f, 0.65f, 1f), new Color(0.99607843f, 0.9019608f, 0.5411765f, 0.55f), new Color(0.99607843f, 0.9019608f, 0.5411765f, 0f), 0.28f, 40);
            SpawnPulse("FoodRippleOuter", position, new Vector3(0.08f, 0.08f, 1f), new Vector3(0.95f, 0.95f, 1f), new Color(1f, 1f, 1f, 0.24f), new Color(1f, 1f, 1f, 0f), 0.42f, 39);
        }

        public void SpawnFoodBurst(Vector2 position)
        {
            SpawnPulse("FoodBurst", position, new Vector3(0.18f, 0.18f, 1f), new Vector3(0.48f, 0.48f, 1f), new Color(1f, 0.94509804f, 0.63529414f, 0.65f), new Color(1f, 0.94509804f, 0.63529414f, 0f), 0.24f, 42);
        }

        public void SpawnFearPulse(Vector2 position)
        {
            SpawnPulse("FearPulse", position, new Vector3(0.15f, 0.15f, 1f), new Vector3(0.72f, 0.72f, 1f), new Color(0.8745098f, 0.99215686f, 1f, 0.38f), new Color(0.8745098f, 0.99215686f, 1f, 0f), 0.32f, 41);
        }

        public void SpawnImportPulse(Vector2 position, bool isTrash)
        {
            var tint = isTrash
                ? new Color(0.9098039f, 0.96862745f, 0.9372549f, 0.32f)
                : new Color(0.8666667f, 0.95686275f, 1f, 0.3f);
            SpawnPulse("ImportPulse", position, new Vector3(0.2f, 0.2f, 1f), new Vector3(0.84f, 0.84f, 1f), tint, new Color(tint.r, tint.g, tint.b, 0f), 0.26f, 32);
        }

        public void SpawnCleanupPulse(Vector2 position)
        {
            SpawnPulse("CleanupPulse", position, new Vector3(0.18f, 0.18f, 1f), new Vector3(0.82f, 0.82f, 1f), new Color(0.85882354f, 0.9764706f, 0.9372549f, 0.36f), new Color(0.85882354f, 0.9764706f, 0.9372549f, 0f), 0.28f, 35);
        }

        private void SpawnPulse(string key, Vector2 position, Vector3 startScale, Vector3 endScale, Color startColor, Color endColor, float lifetime, int sortingOrder)
        {
            if (spriteLibrary == null)
            {
                return;
            }

            var pulseObject = new GameObject(key);
            pulseObject.transform.SetParent(effectRoot != null ? effectRoot : transform, false);
            pulseObject.transform.position = position;
            var effect = pulseObject.AddComponent<TransientSpriteEffect>();
            effect.Initialize(
                spriteLibrary.GetCircleSprite($"{key}_Sprite", Color.white),
                sortingOrder,
                startScale,
                endScale,
                startColor,
                endColor,
                lifetime,
                Vector3.zero);
        }
    }
}
