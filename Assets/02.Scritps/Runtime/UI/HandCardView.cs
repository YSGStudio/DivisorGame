using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>손패 카드가 화면에 알리는 입력들. GameUI가 구현한다.</summary>
    public interface IHandCardHandler
    {
        void OnCardClicked(int index);
        /// <summary>버리기/분해하기 팝업 요청(더블클릭 또는 오른쪽 클릭).</summary>
        void OnCardMenuRequested(int index);
        void OnCardBeginDrag(int index, PointerEventData eventData);
        void OnCardDrag(PointerEventData eventData);
        void OnCardEndDrag();

        /// <summary>fromIndex 카드를 toIndex 카드 위에 놓았다(합치기).</summary>
        void OnCardDropped(int fromIndex, int toIndex);
    }

    /// <summary>
    /// 손패 카드 한 장의 표시와 입력 (T4, T7).
    ///
    /// - 왼쪽 클릭 한 번: 목표 카드에 바로 제출
    /// - 더블클릭 / 오른쪽 클릭: 버리기/분해하기 팝업
    /// - 다른 손패 카드 위로 드래그: 두 수를 곱해 한 장으로 합치기
    ///
    /// 클릭이 곧 제출이라, 약수인 카드는 첫 클릭에 손패에서 사라져 더블클릭이 성립하지 않는다.
    /// (예: 목표가 12인데 12를 3 x 4로 쪼개고 싶은 경우) 그래서 언제나 통하는 경로로
    /// 오른쪽 클릭을 함께 받는다. Unity WebGL은 캔버스의 브라우저 우클릭 메뉴를 막아 두므로
    /// 웹에서도 그대로 동작한다.
    ///
    /// 드래그가 시작되면 EventSystem이 클릭을 무효화하므로(eligibleForClick = false),
    /// 끌었을 때 제출이 함께 일어나지는 않는다.
    /// </summary>
    public class HandCardView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public const float CardHeight = 172f;

        private CardFace _face;

        private int _index;
        private IHandCardHandler _handler;

        private float _lastClickTime = float.NegativeInfinity;

        /// <summary>두 번의 클릭을 더블클릭으로 볼 최대 간격(초). GameManager의 Inspector 값이 주입된다.</summary>
        public float DoubleClickSeconds { get; set; } = 0.3f;

        public int Value { get; private set; }

        public static HandCardView Create(Transform parent, Font font, IHandCardHandler handler)
        {
            var face = CardFace.Create("HandCard", parent, CardHeight, font, 72,
                UITheme.HandBorder, UITheme.HandFill);

            Vector2 size = face.Root.sizeDelta;
            var layoutElement = face.Root.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;

            var view = face.Root.gameObject.AddComponent<HandCardView>();
            view._face = face;
            view._handler = handler;
            return view;
        }

        public void Bind(int index, int value)
        {
            _index = index;
            Value = value;
            _face.SetNumber(value);
            SetDragging(false);

            // 손패가 바뀌면 이 자리에 다른 카드가 올 수 있으므로 더블클릭 연쇄를 끊는다.
            // (첫 클릭으로 카드가 제출돼 사라진 뒤, 두 번째 클릭이 엉뚱한 카드의 메뉴를 여는 것을 막는다.)
            _lastClickTime = float.NegativeInfinity;
        }

        /// <summary>끌고 있는 동안 원래 자리의 카드를 흐리게 보여 준다.</summary>
        public void SetDragging(bool dragging)
        {
            _face.SetAlpha(dragging ? 0.35f : 1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _lastClickTime = float.NegativeInfinity;
                _handler.OnCardMenuRequested(_index);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;

            float now = Time.unscaledTime;

            if (now - _lastClickTime <= DoubleClickSeconds)
            {
                _lastClickTime = float.NegativeInfinity; // 세 번째 클릭이 또 더블클릭이 되지 않도록 초기화
                _handler.OnCardMenuRequested(_index);
                return;
            }

            _lastClickTime = now;
            _handler.OnCardClicked(_index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            SetDragging(true);
            _handler.OnCardBeginDrag(_index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _handler.OnCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SetDragging(false);
            _handler.OnCardEndDrag();
        }

        /// <summary>다른 카드가 이 카드 위에 놓였을 때 EventSystem이 호출한다.</summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            var source = eventData.pointerDrag.GetComponent<HandCardView>();
            if (source == null || source == this) return;

            _handler.OnCardDropped(source._index, _index);
        }
    }
}
