using UnityEngine;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// 목표 카드 오른쪽에 이어 붙는 "제출한 약수 카드" 한 장.
    /// 손패에서 낸 카드가 그대로 옆으로 옮겨간 것처럼 보이게 해서,
    /// 지금까지 찾은 약수가 무엇인지 한눈에 보이도록 한다.
    ///
    /// 개수가 자주 바뀌므로 매번 만들고 지우지 않고 최대 개수만큼 미리 만들어 두고
    /// 켜고 끈다(Destroy가 프레임 끝까지 지연되어 레이아웃이 튀는 것을 피하기 위함).
    /// </summary>
    public class SubmittedCardView : MonoBehaviour
    {
        public const float CardWidth = 116f;
        public const float CardHeight = 158f;

        private Text _numberText;

        /// <summary>현재 표시 중인 약수 값.</summary>
        public int Value { get; private set; }

        public static SubmittedCardView Create(Transform parent, Font font)
        {
            var border = UIFactory.CreatePanel("SubmittedCard", parent, UITheme.TargetClearedBorder,
                SpriteFactory.RoundedLarge);
            border.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            border.raycastTarget = false;

            var layoutElement = border.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = CardWidth;
            layoutElement.preferredHeight = CardHeight;

            var fill = UIFactory.CreatePanel("Fill", border.transform, UITheme.TargetClearedFill,
                SpriteFactory.RoundedLarge);
            UIFactory.Stretch(fill.rectTransform, 6, 6, 6, 6);
            fill.raycastTarget = false;

            var numberText = UIFactory.CreateText("Number", fill.transform, "0", 64, UITheme.TextDark,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.StretchAll(numberText.rectTransform);

            var view = border.gameObject.AddComponent<SubmittedCardView>();
            view._numberText = numberText;
            view.gameObject.SetActive(false);
            return view;
        }

        public void Bind(int value)
        {
            Value = value;
            _numberText.text = value.ToString();
        }
    }
}
