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
    /// 숫자는 04.Images의 카드 그림이 그린다. 그림의 숫자는 카드 한가운데(위아래로 대략 35~65%)에
    /// 있으므로, 진행 표시와 타이머는 그 위쪽 여백에, 상태 문구는 아래쪽 여백에 얹는다.
    ///
    /// 확보한 약수는 이 카드 안이 아니라 카드 오른쪽에 카드 모양으로 이어 붙는다
    /// (`SubmittedCardView`). 그래서 여기에는 상태 문구만 남겼다.
    /// </summary>
    public class TargetCardView : MonoBehaviour
    {
        public const float CardHeight = 440f;

        /// <summary>클리어한 목표를 초록빛으로 물들여 한눈에 알아보게 한다.</summary>
        private static readonly Color ClearedTint = new Color(0.78f, 0.98f, 0.78f, 1f);

        private CardFace _face;
        private Text _progressText;
        private Text _timerText;
        private Text _statusText;

        public static TargetCardView Create(Transform parent, Font font)
        {
            var face = CardFace.Create("TargetCard", parent, CardHeight, font, 150,
                UITheme.TargetBorder, UITheme.TargetFill);
            face.SetRaycastTarget(false);

            RectTransform root = face.Root;
            Vector2 size = root.sizeDelta;
            var layoutElement = root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;

            // 그림 속 숫자를 가리지 않도록 위/아래 여백 안에서만 자리를 잡는다.
            var progressText = UIFactory.CreateText("Progress", root, "0/0", 58, UITheme.TargetBorder,
                TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.AnchorTop(progressText.rectTransform, 64f, 34f, 24f);

            var timerText = UIFactory.CreateText("Timer", root, "0초", 32, UITheme.TextMuted,
                TextAnchor.MiddleCenter, font);
            UIFactory.AnchorTop(timerText.rectTransform, 42f, 98f, 24f);

            var statusText = UIFactory.CreateText("Status", root, "약수를 모두 찾아 보세요", 24,
                UITheme.TextMuted, TextAnchor.MiddleCenter, font);
            UIFactory.AnchorBottom(statusText.rectTransform, 40f, 34f, 20f);

            var view = root.gameObject.AddComponent<TargetCardView>();
            view._face = face;
            view._progressText = progressText;
            view._timerText = timerText;
            view._statusText = statusText;
            return view;
        }

        /// <summary>매 프레임 호출해도 되도록 텍스트 갱신만 한다.</summary>
        public void Refresh(TargetCardController target)
        {
            if (target == null) return;

            if (_face.Value != target.Number) _face.SetNumber(target.Number);
            _timerText.text = target.ElapsedSecondsFloored + "초";
            _progressText.text = target.ProgressText;

            if (target.IsCleared)
            {
                _face.SetTint(ClearedTint);
                _progressText.color = UITheme.TargetClearedBorder;
                _statusText.color = UITheme.Positive;
                _statusText.text = "클리어!";
            }
            else
            {
                _face.SetTint(Color.white);
                _progressText.color = UITheme.TargetBorder;
                _statusText.color = UITheme.TextMuted;
                _statusText.text = "약수를 모두 찾아 보세요";
            }
        }
    }
}
