using System.Collections.Generic;
using UnityEngine;

namespace Aquascape
{
    public sealed class ProceduralSpriteLibrary : MonoBehaviour
    {
        private readonly Dictionary<string, Sprite> cachedSprites = new();
        private readonly Dictionary<string, Texture2D> cachedTextures = new();

        public Sprite GetSolidSprite(string key, Color color)
        {
            return GetOrCreate(key, () =>
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"{key}_Texture"
                };

                texture.SetPixels(new[] { color, color, color, color });
                texture.Apply();
                return texture;
            });
        }

        public Sprite GetCircleSprite(string key, Color color, int size = 64)
        {
            return GetOrCreate(key, () =>
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"{key}_Texture"
                };

                var pixels = new Color[size * size];
                var center = (size - 1) * 0.5f;
                var maxRadius = center;
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                        var normalized = Mathf.InverseLerp(maxRadius, maxRadius * 0.55f, distance);
                        var alpha = Mathf.Clamp01(normalized * normalized);
                        pixels[(y * size) + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            });
        }

        public Sprite GetVerticalGradientSprite(string key, Color top, Color bottom, int width = 8, int height = 128)
        {
            return GetOrCreate(key, () =>
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"{key}_Texture"
                };

                var pixels = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    var color = Color.Lerp(bottom, top, y / (height - 1f));
                    for (var x = 0; x < width; x++)
                    {
                        pixels[(y * width) + x] = color;
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            });
        }

        public Sprite GetLightBeamSprite(string key, Color color, int width = 128, int height = 256)
        {
            return GetOrCreate(key, () =>
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = $"{key}_Texture"
                };

                var pixels = new Color[width * height];
                var center = (width - 1) * 0.5f;
                for (var y = 0; y < height; y++)
                {
                    var verticalAlpha = Mathf.Lerp(0.1f, 1f, y / (height - 1f));
                    for (var x = 0; x < width; x++)
                    {
                        var horizontal = Mathf.Abs(x - center) / center;
                        var alpha = Mathf.Clamp01(1f - horizontal * horizontal);
                        pixels[(y * width) + x] = new Color(color.r, color.g, color.b, color.a * alpha * verticalAlpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            });
        }

        private Sprite GetOrCreate(string key, System.Func<Texture2D> textureFactory)
        {
            if (cachedSprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var texture = textureFactory();
            cachedTextures[key] = texture;
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{key}_Sprite";
            cachedSprites[key] = sprite;
            return sprite;
        }

        private void OnDestroy()
        {
            foreach (var pair in cachedSprites)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            foreach (var pair in cachedTextures)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            cachedSprites.Clear();
            cachedTextures.Clear();
        }
    }
}
