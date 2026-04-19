using UnityEngine;

namespace Aquascape
{
    public sealed class TransientSpriteEffect : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 startScale;
        private Vector3 endScale;
        private Color startColor;
        private Color endColor;
        private Vector3 driftPerSecond;
        private float lifetime;
        private float age;

        public void Initialize(
            Sprite sprite,
            int sortingOrder,
            Vector3 initialScale,
            Vector3 finalScale,
            Color initialColor,
            Color finalColor,
            float totalLifetime,
            Vector3 driftVelocity)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;

            startScale = initialScale;
            endScale = finalScale;
            startColor = initialColor;
            endColor = finalColor;
            driftPerSecond = driftVelocity;
            lifetime = Mathf.Max(0.05f, totalLifetime);

            transform.localScale = startScale;
            spriteRenderer.color = startColor;
        }

        private void Update()
        {
            age += Time.deltaTime;
            var normalized = Mathf.Clamp01(age / lifetime);
            transform.localScale = Vector3.Lerp(startScale, endScale, normalized);
            transform.position += driftPerSecond * Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, endColor, normalized);

            if (normalized >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
