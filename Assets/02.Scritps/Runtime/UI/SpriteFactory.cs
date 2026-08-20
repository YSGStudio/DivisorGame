using UnityEngine;

namespace DivisorGame.UI
{
    /// <summary>
    /// 둥근 모서리 스프라이트를 코드로 생성한다.
    /// 프로젝트에 이미지 에셋이 없고 빌트인 UI 스프라이트에 의존하고 싶지 않아,
    /// 9-slice용 텍스처를 런타임에 만들어 쓴다(WebGL에서도 동일하게 동작).
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _rounded24;
        private static Sprite _rounded12;

        /// <summary>모서리 반경이 큰 카드/패널용 스프라이트.</summary>
        public static Sprite RoundedLarge
        {
            get
            {
                // UnityEngine.Object의 == 오버로드를 써야 파괴된 객체를 다시 만들 수 있다(?? 는 안 됨).
                if (_rounded24 == null) _rounded24 = CreateRounded(24);
                return _rounded24;
            }
        }

        /// <summary>버튼 등 작은 요소용 스프라이트.</summary>
        public static Sprite RoundedSmall
        {
            get
            {
                if (_rounded12 == null) _rounded12 = CreateRounded(12);
                return _rounded12;
            }
        }

        private static Sprite CreateRounded(int radius)
        {
            int size = radius * 2 + 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RoundedRect" + radius,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = CoverageAt(x + 0.5f, y + 0.5f, size, radius);
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = "RoundedRect" + radius;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>모서리에서의 픽셀 커버리지(간단한 안티에일리어싱).</summary>
        private static float CoverageAt(float x, float y, int size, int radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - distance + 0.5f);
        }
    }
}
