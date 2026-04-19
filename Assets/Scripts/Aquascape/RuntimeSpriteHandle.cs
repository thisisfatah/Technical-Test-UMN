using UnityEngine;

namespace Aquascape
{
    public sealed class RuntimeSpriteHandle : MonoBehaviour
    {
        private Sprite sprite;
        private Texture2D texture;

        public void Initialize(Sprite spriteAsset, Texture2D textureAsset)
        {
            sprite = spriteAsset;
            texture = textureAsset;
        }

        private void OnDestroy()
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }

            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
