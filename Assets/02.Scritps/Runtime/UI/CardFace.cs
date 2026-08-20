using UnityEngine;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// 숫자 카드 한 장의 겉모습. 목표 카드 · 손패 카드 · 제출한 카드 · 드래그 그림자가 모두 이걸 쓴다.
    ///
    /// 평소에는 04.Images의 카드 그림을 그대로 띄우고, 그림이 없는 숫자는 예전처럼 코드로 그린
    /// 둥근 카드에 숫자를 적는다. 지금 규칙에서는 카드 값이 99를 넘지 않아 늘 그림이 있지만,
    /// 밸런싱으로 상한이 올라가도 카드가 빈 사각형으로 보이지 않도록 예비 표시를 남겨 두었다.
    ///
    /// MonoBehaviour가 아니라 일반 클래스다. 카드마다 붙는 컴포넌트를 늘리지 않으려는 의도이고,
    /// 소유자(HandCardView 등)가 필드로 들고 있으면 된다.
    /// </summary>
    public sealed class CardFace
    {
        private readonly Image _image;
        private readonly Image _fallbackFill;
        private readonly Text _fallbackNumber;
        private readonly Color _fallbackBorderColor;

        private Color _tint = Color.white;
        private float _alpha = 1f;
        private bool _usingArtwork;

        private CardFace(Image image, Image fallbackFill, Text fallbackNumber, Color fallbackBorderColor)
        {
            _image = image;
            _fallbackFill = fallbackFill;
            _fallbackNumber = fallbackNumber;
            _fallbackBorderColor = fallbackBorderColor;
        }

        /// <summary>카드의 RectTransform. 레이아웃과 입력 처리는 이 오브젝트에 붙인다.</summary>
        public RectTransform Root => _image.rectTransform;

        public int Value { get; private set; }

        /// <summary>
        /// 카드 한 장을 만든다. 크기는 세로 길이만 정하면 그림 비율에 맞춰 가로가 정해진다.
        /// fallbackBorder / fallbackFill / numberFontSize는 그림이 없는 숫자일 때만 쓰인다.
        /// </summary>
        public static CardFace Create(
            string name,
            Transform parent,
            float height,
            Font font,
            int numberFontSize,
            Color fallbackBorder,
            Color fallbackFill)
        {
            var image = UIFactory.CreatePanel(name, parent, Color.white, SpriteFactory.RoundedLarge);
            image.rectTransform.sizeDelta = CardSpriteLibrary.SizeForHeight(height);
            image.preserveAspect = true;

            var fill = UIFactory.CreatePanel("Fill", image.transform, fallbackFill, SpriteFactory.RoundedLarge);
            UIFactory.Stretch(fill.rectTransform, 6, 6, 6, 6);
            fill.raycastTarget = false;

            var number = UIFactory.CreateText("Number", fill.transform, "0", numberFontSize, UITheme.TextDark,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.StretchAll(number.rectTransform);

            fill.gameObject.SetActive(false);
            return new CardFace(image, fill, number, fallbackBorder);
        }

        /// <summary>이 카드가 나타낼 숫자를 정한다.</summary>
        public void SetNumber(int value)
        {
            Value = value;

            Sprite artwork = CardSpriteLibrary.Get(value);
            _usingArtwork = artwork != null;

            if (_usingArtwork)
            {
                _image.sprite = artwork;
                _image.type = Image.Type.Simple;
            }
            else
            {
                // 그림이 없는 숫자: 둥근 테두리 + 숫자 텍스트로 대체한다.
                _image.sprite = SpriteFactory.RoundedLarge;
                _image.type = Image.Type.Sliced;
                _fallbackNumber.text = value.ToString();
            }

            _fallbackFill.gameObject.SetActive(!_usingArtwork);
            ApplyColor();
        }

        /// <summary>카드 전체에 곱해지는 색. 상태 표시(예: 클리어한 목표를 초록빛으로)에 쓴다.</summary>
        public void SetTint(Color tint)
        {
            _tint = tint;
            ApplyColor();
        }

        /// <summary>끌고 있는 동안 원래 자리의 카드를 흐리게 하는 등 투명도만 바꾼다.</summary>
        public void SetAlpha(float alpha)
        {
            _alpha = alpha;
            ApplyColor();
        }

        public void SetRaycastTarget(bool value)
        {
            _image.raycastTarget = value;
        }

        private void ApplyColor()
        {
            // 그림을 쓸 때는 원본 색을 살려야 하므로 흰색에 tint만 곱하고,
            // 예비 표시일 때는 테두리 색에 곱한다.
            Color color = _usingArtwork ? _tint : _fallbackBorderColor * _tint;
            color.a = _alpha;
            _image.color = color;

            Color numberColor = _fallbackNumber.color;
            numberColor.a = _alpha;
            _fallbackNumber.color = numberColor;

            Color fillColor = _fallbackFill.color;
            fillColor.a = _alpha;
            _fallbackFill.color = fillColor;
        }
    }
}
