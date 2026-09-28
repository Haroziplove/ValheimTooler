using System.Collections.Generic;
using UnityEngine;

namespace ValheimAdminTool.UI
{
    public static class SpriteManager
    {
        private static readonly Dictionary<string, Texture2D> s_atlasCache = new Dictionary<string, Texture2D>();

        // One cropped texture per sprite, shared by the item giver and recipe manager. Without this
        // cache every conversion created a new native texture that was never freed.
        private static readonly Dictionary<Sprite, Texture2D> s_cropCache = new Dictionary<Sprite, Texture2D>();
        private static readonly Dictionary<Sprite, Texture2D> s_cropResizedCache = new Dictionary<Sprite, Texture2D>();

        public static Texture2D TextureFromSprite(Sprite sprite, bool resize = true)
        {
            if (sprite == null || sprite.texture == null)
            {
                return null;
            }

            if (sprite.rect.width == sprite.texture.width)
            {
                return sprite.texture;
            }

            Dictionary<Sprite, Texture2D> cache = resize ? s_cropResizedCache : s_cropCache;
            Texture2D cached;
            if (cache.TryGetValue(sprite, out cached) && cached != null)
            {
                return cached;
            }

            Texture2D atlas;
            if (!s_atlasCache.TryGetValue(sprite.texture.name, out atlas) || atlas == null)
            {
                atlas = DuplicateTexture(sprite.texture);
                s_atlasCache[sprite.texture.name] = atlas;
            }

            Texture2D crop = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] colors = atlas.GetPixels(Mathf.CeilToInt(sprite.textureRect.x),
                                             Mathf.CeilToInt(sprite.textureRect.y),
                                             Mathf.CeilToInt(sprite.textureRect.width),
                                             Mathf.CeilToInt(sprite.textureRect.height));
            crop.SetPixels(colors);
            crop.Apply();

            if (resize && (crop.width > 200 || crop.height > 200))
            {
                crop.Reinitialize(60, 60);
            }

            cache[sprite] = crop;
            return crop;
        }

        public static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D readableText = new Texture2D(source.width, source.height)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }
    }
}
