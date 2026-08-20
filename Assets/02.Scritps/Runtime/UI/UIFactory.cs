using System;
using UnityEngine;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// uGUI 요소를 코드로 만드는 헬퍼.
    /// 프리팹/씬 편집 없이 화면 전체를 런타임에 구성하기 위해 사용한다.
    /// </summary>
    public static class UIFactory
    {
        private static Font _fallbackFont;

        /// <summary>
        /// 폰트를 지정하지 않았을 때 쓰는 기본 폰트.
        ///
        /// 화면 텍스트가 모두 영문이라 빌트인 LegacyRuntime.ttf로 충분하며, 에디터와 WebGL 빌드가
        /// 같은 폰트로 렌더링된다. 나중에 한글 문구로 바꾼다면 이 폰트에는 한글 글리프가 없으므로
        /// GameUI의 uiFont 필드에 한글 TTF를 지정해야 한다.
        /// </summary>
        public static Font FallbackFont
        {
            get
            {
                if (_fallbackFont == null) _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _fallbackFont;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor anchor,
            Font font,
            FontStyle style = FontStyle.Normal)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = font != null ? font : FallbackFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // 부모 카드가 클릭/롱프레스를 받아야 하므로 텍스트는 레이캐스트에서 제외
            text.supportRichText = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            Color backgroundColor,
            Font font,
            Action onClick)
        {
            var image = CreatePanel(name, parent, backgroundColor, SpriteFactory.RoundedSmall);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            var text = CreateText(name + "Label", image.transform, label, fontSize, UITheme.TextOnAccent,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            Stretch(text.rectTransform, 8, 4, 8, 4);

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>부모를 가득 채우되 각 변에서 지정한 만큼 안쪽으로 들어간다.</summary>
        public static RectTransform Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform StretchAll(RectTransform rect) => Stretch(rect, 0, 0, 0, 0);

        /// <summary>부모 위쪽에 가로로 붙인다.</summary>
        public static RectTransform AnchorTop(RectTransform rect, float height, float offsetY, float sideMargin = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(sideMargin, 0f);
            rect.offsetMax = new Vector2(-sideMargin, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(0f, -offsetY);
            return rect;
        }

        /// <summary>부모 아래쪽에 가로로 붙인다.</summary>
        public static RectTransform AnchorBottom(RectTransform rect, float height, float offsetY, float sideMargin = 0f)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(sideMargin, 0f);
            rect.offsetMax = new Vector2(-sideMargin, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(0f, offsetY);
            return rect;
        }

        /// <summary>부모 중앙에 고정 크기로 놓는다.</summary>
        public static RectTransform AnchorCenter(RectTransform rect, float width, float height, Vector2 offset)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = offset;
            return rect;
        }

        /// <summary>부모 왼쪽 위에서 오프셋만큼 떨어진 고정 크기 요소.</summary>
        public static RectTransform AnchorTopLeft(RectTransform rect, float width, float height, Vector2 offset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = offset;
            return rect;
        }

        /// <summary>부모 오른쪽 위에서 오프셋만큼 떨어진 고정 크기 요소.</summary>
        public static RectTransform AnchorTopRight(RectTransform rect, float width, float height, Vector2 offset)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = offset;
            return rect;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing, TextAnchor alignment)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }
    }
}
