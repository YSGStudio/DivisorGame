#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// 에디터에서 손으로 확인하기 위한 테스트 도구 패널. F1으로 열고 닫는다.
    ///
    /// 목표 숫자와 손패가 무작위라 "합성수 카드 분해", "소수 카드는 분해 불가",
    /// "손패가 가득 차면 분해 불가" 같은 상황을 우연에 기대지 않고 바로 만들어 보기 위해 있다.
    /// 파일 전체가 #if UNITY_EDITOR로 감싸여 있어 플레이어 빌드에는 포함되지 않는다.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private enum Mode
        {
            AddCard,
            SetTarget
        }

        private GameManager _game;
        private GameObject _root;
        private Text _modeText;
        private Button _addModeButton;
        private Button _targetModeButton;

        private Mode _mode = Mode.AddCard;

        public static DebugPanel Create(Transform canvasRoot, GameManager game, Font font)
        {
            var panel = canvasRoot.gameObject.AddComponent<DebugPanel>();
            panel.Build(canvasRoot, game, font);
            return panel;
        }

        private void Build(Transform canvasRoot, GameManager game, Font font)
        {
            _game = game;

            var panel = UIFactory.CreatePanel("DebugPanel", canvasRoot, new Color(0.11f, 0.14f, 0.22f, 0.94f),
                SpriteFactory.RoundedLarge);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 560f);
            rect.anchoredPosition = new Vector2(-20f, 40f);
            _root = panel.gameObject;

            var title = UIFactory.CreateText("DebugTitle", panel.transform, "테스트 도구   (F1로 닫기)", 26,
                Color.white, TextAnchor.MiddleCenter, font, FontStyle.Bold);
            UIFactory.AnchorTop(title.rectTransform, 40f, 14f, 12f);

            _addModeButton = UIFactory.CreateButton("AddMode", panel.transform, "손패에 추가", 24,
                UITheme.ButtonPrimary, font, () => SetMode(Mode.AddCard));
            PlaceHalfWidth(_addModeButton.GetComponent<RectTransform>(), true, 56f, -60f);

            _targetModeButton = UIFactory.CreateButton("TargetMode", panel.transform, "목표 숫자 바꾸기", 24,
                UITheme.ButtonNeutral, font, () => SetMode(Mode.SetTarget));
            PlaceHalfWidth(_targetModeButton.GetComponent<RectTransform>(), false, 56f, -60f);

            _modeText = UIFactory.CreateText("ModeHint", panel.transform, string.Empty, 22,
                new Color(0.78f, 0.85f, 1f), TextAnchor.MiddleCenter, font);
            UIFactory.AnchorTop(_modeText.rectTransform, 34f, 126f, 12f);

            // 1~25 숫자 격자
            var grid = UIFactory.CreateRect("NumberGrid", panel.transform);
            UIFactory.AnchorTop(grid, 290f, 170f, 12f);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(68f, 50f);
            gridLayout.spacing = new Vector2(6f, 6f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            for (int number = 1; number <= 25; number++)
            {
                int value = number;
                UIFactory.CreateButton("Num" + number, grid, number.ToString(), 24,
                    UITheme.ButtonPrimary, font, () => OnNumberPressed(value));
            }

            var clearButton = UIFactory.CreateButton("ClearHand", panel.transform, "손패 비우기", 22,
                UITheme.ButtonDanger, font, () => _game.DebugClearHand());
            PlaceHalfWidthFromBottom(clearButton.GetComponent<RectTransform>(), true, 52f, 16f);

            var fillButton = UIFactory.CreateButton("FillHand", panel.transform, "손패 가득 채우기", 22,
                UITheme.ButtonNeutral, font, () => _game.DebugFillHand());
            PlaceHalfWidthFromBottom(fillButton.GetComponent<RectTransform>(), false, 52f, 16f);

            SetMode(Mode.AddCard);
            _root.SetActive(false);
        }

        /// <summary>패널 위쪽에서 좌/우 절반 폭으로 배치한다.</summary>
        private static void PlaceHalfWidth(RectTransform rect, bool leftHalf, float height, float offsetY)
        {
            rect.anchorMin = new Vector2(leftHalf ? 0f : 0.5f, 1f);
            rect.anchorMax = new Vector2(leftHalf ? 0.5f : 1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(leftHalf ? 12f : 6f, 0f);
            rect.offsetMax = new Vector2(leftHalf ? -6f : -12f, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, offsetY);
        }

        /// <summary>패널 아래쪽에서 좌/우 절반 폭으로 배치한다.</summary>
        private static void PlaceHalfWidthFromBottom(RectTransform rect, bool leftHalf, float height, float offsetY)
        {
            rect.anchorMin = new Vector2(leftHalf ? 0f : 0.5f, 0f);
            rect.anchorMax = new Vector2(leftHalf ? 0.5f : 1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(leftHalf ? 12f : 6f, 0f);
            rect.offsetMax = new Vector2(leftHalf ? -6f : -12f, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, offsetY);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) _root.SetActive(!_root.activeSelf);
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;

            _addModeButton.GetComponent<Image>().color =
                mode == Mode.AddCard ? UITheme.ButtonPrimary : UITheme.ButtonNeutral;
            _targetModeButton.GetComponent<Image>().color =
                mode == Mode.SetTarget ? UITheme.ButtonPrimary : UITheme.ButtonNeutral;

            _modeText.text = mode == Mode.AddCard
                ? "숫자를 누르면 그 카드가 손패에 들어갑니다"
                : "숫자를 누르면 목표 숫자가 그 수로 바뀝니다";
        }

        private void OnNumberPressed(int value)
        {
            if (_mode == Mode.AddCard) _game.DebugAddCard(value);
            else _game.DebugSetTargetNumber(value);
        }
    }
}
#endif
