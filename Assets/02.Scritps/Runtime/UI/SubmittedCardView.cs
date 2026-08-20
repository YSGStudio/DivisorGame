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
        public const float CardHeight = 158f;

        /// <summary>이미 확보한 카드임을 알리는 옅은 초록빛. 손패 카드와 한눈에 구분된다.</summary>
        private static readonly Color CollectedTint = new Color(0.80f, 0.98f, 0.82f, 1f);

        private CardFace _face;

        /// <summary>현재 표시 중인 약수 값.</summary>
        public int Value { get; private set; }

        public static SubmittedCardView Create(Transform parent, Font font)
        {
            var face = CardFace.Create("SubmittedCard", parent, CardHeight, font, 64,
                UITheme.TargetClearedBorder, UITheme.TargetClearedFill);
            face.SetRaycastTarget(false);
            face.SetTint(CollectedTint);

            Vector2 size = face.Root.sizeDelta;
            var layoutElement = face.Root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;

            var view = face.Root.gameObject.AddComponent<SubmittedCardView>();
            view._face = face;
            view.gameObject.SetActive(false);
            return view;
        }

        public void Bind(int value)
        {
            Value = value;
            _face.SetNumber(value);
        }
    }
}
