using DivisorGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// 화면에 하나뿐인 목표 숫자 카드의 표시 (T3).
    /// 목표 숫자, 진행 표시 "제출한 개수/전체 약수 개수"(R2-1), 개별 타이머(R2)를 보여준다.
    /// 제출은 손패 카드를 한 번 클릭하면 바로 이루어지므로 이 카드는 표시 전용이다.
    ///
    /// 확보한 약수는 이 카드 안이 아니라 카드 오른쪽에 카드 모양으로 이어 붙는다
    /// (`SubmittedCardView`). 그래서 여기에는 상태 문구만 남겼다.
    /// </summary>
    public class TargetCardView : MonoBehaviour
    {
        public const float CardWidth = 340f;
        public const float CardHeight = 440f;

        private Image _border;
        private Image _fill;
        private Text _numberText;
        private Text _progressText;
        private Text _timerText;
        private Text _statusText;

        public static TargetCardView Create(Transform parent, Font font)
        {
            var border = UIFactory.CreatePanel("TargetCard", parent, UITheme.TargetBorder, SpriteFactory.RoundedLarge);
            border.rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);

            var layoutElement = border.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = CardWidth;
            layoutElement.preferredHeight = CardHeight;

            var fill = UIFactory.CreatePanel("Fill", border.transform, UITheme.TargetFill, SpriteFactory.RoundedLarge);
            UIFactory.Stretch(fill.rectTransform, 8, 8, 8, 8);
            fill.raycastTarget = false;

            var numberText = UIFactory.CreateText("Number", fill.transform, "0", 150, UITheme.TextDark,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.AnchorTop(numberText.rectTransform, 190f, 24f, 10f);

            var progressText = UIFactory.CreateText("Progress", fill.transform, "0/0", 60, UITheme.TargetBorder,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.AnchorTop(progressText.rectTransform, 70f, 222f, 10f);

            var timerText = UIFactory.CreateText("Timer", fill.transform, "0초", 36, UITheme.TextMuted,
                TextAnchor.MiddleCenter, font);
            UIFactory.AnchorTop(timerText.rectTransform, 48f, 300f, 10f);

            var statusText = UIFactory.CreateText("Status", fill.transform, "약수를 모두 찾아 보세요", 26,
                UITheme.TextMuted, TextAnchor.MiddleCenter, font);
            UIFactory.AnchorBottom(statusText.rectTransform, 44f, 18f, 12f);

            var view = border.gameObject.AddComponent<TargetCardView>();
            view._border = border;
            view._fill = fill;
            view._numberText = numberText;
            view._progressText = progressText;
            view._timerText = timerText;
            view._statusText = statusText;
            return view;
        }

        /// <summary>매 프레임 호출해도 되도록 텍스트 갱신만 한다.</summary>
        public void Refresh(TargetCardController target)
        {
            if (target == null) return;

            _numberText.text = target.Number.ToString();
            _timerText.text = target.ElapsedSecondsFloored + "초";
            _progressText.text = target.ProgressText;

            if (target.IsCleared)
            {
                _border.color = UITheme.TargetClearedBorder;
                _fill.color = UITheme.TargetClearedFill;
                _progressText.color = UITheme.TargetClearedBorder;
                _statusText.color = UITheme.Positive;
                _statusText.text = "클리어!";
            }
            else
            {
                _border.color = UITheme.TargetBorder;
                _fill.color = UITheme.TargetFill;
                _progressText.color = UITheme.TargetBorder;
                _statusText.color = UITheme.TextMuted;
                _statusText.text = "약수를 모두 찾아 보세요";
            }
        }
    }
}
