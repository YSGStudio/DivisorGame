using System.Collections.Generic;
using DivisorGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DivisorGame.UI
{
    /// <summary>
    /// 화면 전체를 런타임에 구성하고 GameManager의 상태를 표시한다 (T3, T4, T7, T10, T11, T12).
    /// 프리팹/씬 편집 없이 코드로만 UI를 만들기 때문에, 씬에는 이 컴포넌트가 붙은
    /// GameObject 하나만 있으면 게임이 동작한다.
    ///
    /// 화면 구성: 목표 카드 한 장 오른쪽에 제출한 약수 카드가 순서대로 이어 붙는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameManager))]
    public class GameUI : MonoBehaviour, IHandCardHandler
    {
        [Header("폰트")]
        [Tooltip("한글 TTF를 지정한다. 비워 두면 한글 글리프가 없는 빌트인 LegacyRuntime.ttf로 대체된다")]
        [SerializeField] private Font uiFont;

        [Header("피드백")]
        [Tooltip("정답/오답 메시지가 화면에 남아 있는 시간(초)")]
        [SerializeField] private float feedbackDuration = 2f;

        private GameManager _game;

        private Text _scoreText;
        private Text _handCountText;
        private Text _feedbackText;
        private Button _drawButton;
        private Image _drawButtonImage;

        private RectTransform _targetArea;
        private TargetCardView _targetView;
        private readonly List<SubmittedCardView> _submittedViews = new List<SubmittedCardView>();

        private RectTransform _handRow;
        private readonly List<HandCardView> _handViews = new List<HandCardView>();

        private RectTransform _canvasRect;
        private CardFace _dragGhostFace;
        private RectTransform _dragGhost;

        private GameObject _menuRoot;
        private GameObject _choicePanel;
        private GameObject _pairPanel;
        private Text _menuTitle;
        private Text _menuNotice;
        private Button _decomposeButton;
        private Image _decomposeButtonImage;
        private RectTransform _pairRow;
        private Text _pairTitle;
        private int _menuCardIndex = -1;

        private GameObject _resultRoot;
        private Text _resultScoreText;
        private Text _resultClearedText;

        private float _feedbackTimer;

        private void Awake()
        {
            _game = GetComponent<GameManager>();
            if (uiFont == null) uiFont = UIFactory.FallbackFont;
        }

        private void Start()
        {
            EnsureCamera();
            EnsureEventSystem();
            BuildUI();

            _game.OnHandChanged += RefreshHand;
            _game.OnScoreChanged += RefreshTopBar;
            _game.OnTargetChanged += HandleTargetChanged;
            _game.OnFeedback += ShowFeedback;
            _game.OnGameEnded += ShowResult;
            _game.OnGameStarted += HandleGameStarted;

            _game.StartGame();
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.OnHandChanged -= RefreshHand;
            _game.OnScoreChanged -= RefreshTopBar;
            _game.OnTargetChanged -= HandleTargetChanged;
            _game.OnFeedback -= ShowFeedback;
            _game.OnGameEnded -= ShowResult;
            _game.OnGameStarted -= HandleGameStarted;
        }

        private void Update()
        {
            // 목표 카드의 개별 타이머(R2)는 매 프레임 갱신이 필요하다.
            _targetView.Refresh(_game.Target);

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f) _feedbackText.text = string.Empty;
            }
        }

        // ---------------------------------------------------------------- 화면 구성

        private void EnsureCamera()
        {
            if (Camera.main != null) return;

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UITheme.Background;
            camera.orthographic = true;
        }

        /// <summary>
        /// 이 프로젝트는 Input System 신규 전용(activeInputHandler = 1)이라
        /// 레거시 StandaloneInputModule을 쓸 수 없다. InputSystemUIInputModule을 사용한다.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
        }

        private void BuildUI()
        {
            // R15: 1920x1080 기준, 브라우저 창의 너비와 높이에 맞춰 확대/축소된다.
            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UITheme.ReferenceWidth, UITheme.ReferenceHeight);
            // Expand는 가로/세로 중 더 빡빡한 쪽에 맞춰 축소하므로, 기준 해상도(1920x1080)
            // 영역이 창 비율과 상관없이 항상 화면 안에 전부 들어온다. MatchWidthOrHeight(0.5)는
            // 창이 기준보다 납작할 때 세로가 잘려서 상단바와 손패가 보이지 않는 문제가 있었다.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            canvasGo.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)canvasGo.transform;
            _canvasRect = root;

            var background = UIFactory.CreatePanel("Background", root, UITheme.Background);
            UIFactory.StretchAll(background.rectTransform);
            background.raycastTarget = false;

            BuildTopBar(root);
            BuildGuide(root);
            BuildTargetArea(root);
            BuildFeedback(root);
            BuildHandArea(root);
            BuildCardMenu(root);
            BuildResultPanel(root);
            BuildDragGhost(root);

#if UNITY_EDITOR
            // 에디터에서 특정 상황을 손으로 만들어 확인하기 위한 도구. 플레이어 빌드에는 없다.
            DebugPanel.Create(root, _game, uiFont);
#endif
        }

        private void BuildTopBar(RectTransform root)
        {
            var bar = UIFactory.CreatePanel("TopBar", root, UITheme.Panel);
            UIFactory.AnchorTop(bar.rectTransform, 120f, 0f);
            bar.raycastTarget = false;

            _scoreText = UIFactory.CreateText("Score", bar.transform, "점수 0", 46, UITheme.TextDark,
                TextAnchor.MiddleLeft, uiFont, FontStyle.Bold);
            UIFactory.AnchorTopLeft(_scoreText.rectTransform, 420f, 70f, new Vector2(40f, -25f));

            _handCountText = UIFactory.CreateText("HandCount", bar.transform, "손패 0/10", 40, UITheme.TextMuted,
                TextAnchor.MiddleLeft, uiFont);
            UIFactory.AnchorTopLeft(_handCountText.rectTransform, 340f, 70f, new Vector2(470f, -25f));

            _drawButton = UIFactory.CreateButton("DrawButton", bar.transform, "카드 가져오기", 34,
                UITheme.ButtonPrimary, uiFont, () => _game.DrawCard());
            UIFactory.AnchorTopRight(_drawButton.GetComponent<RectTransform>(), 300f, 76f, new Vector2(-280f, -22f));
            _drawButtonImage = _drawButton.GetComponent<Image>();

            var endButton = UIFactory.CreateButton("EndButton", bar.transform, "게임 종료", 34,
                UITheme.ButtonDanger, uiFont, () => _game.EndGame());
            UIFactory.AnchorTopRight(endButton.GetComponent<RectTransform>(), 220f, 76f, new Vector2(-40f, -22f));
        }

        private void BuildGuide(RectTransform root)
        {
            string message = "손패 카드를 클릭하면 바로 제출!   ·   카드를 다른 카드 위로 끌면 두 수를 곱해서 합치기\n"
                             + "더블클릭 또는 오른쪽 클릭 = 버리기 / 분해하기";
#if UNITY_EDITOR
            message += "     [F1] 테스트 도구";
#endif
            var guide = UIFactory.CreateText("Guide", root, message,
                30, UITheme.TextMuted, TextAnchor.MiddleCenter, uiFont);
            UIFactory.AnchorTop(guide.rectTransform, 82f, 122f, 40f);
        }

        /// <summary>
        /// 목표 카드 한 장 + 구분선 + 제출한 약수 카드들을 왼쪽 정렬로 한 줄에 놓는다.
        /// 제출할 때마다 오른쪽으로 카드가 하나씩 이어 붙고, 목표 카드 위치는 움직이지 않는다.
        /// </summary>
        private void BuildTargetArea(RectTransform root)
        {
            _targetArea = UIFactory.CreateRect("TargetArea", root);
            UIFactory.Stretch(_targetArea, 100f, 350f, 60f, 212f);
            UIFactory.AddHorizontalLayout(_targetArea.gameObject, 22f, TextAnchor.MiddleLeft);

            // 0번: 목표 카드
            _targetView = TargetCardView.Create(_targetArea, uiFont);

            // 1번: 구분선
            var divider = UIFactory.CreatePanel("Divider", _targetArea, new Color(0.72f, 0.78f, 0.9f, 1f));
            divider.raycastTarget = false;
            divider.rectTransform.sizeDelta = new Vector2(5f, 250f);
            var dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.preferredWidth = 5f;
            dividerLayout.preferredHeight = 250f;

            // 2번 이후: 제출한 약수 카드 (최대 개수만큼 미리 만들어 두고 켜고 끈다)
            for (int i = 0; i < _game.MaxPossibleDivisorCount; i++)
            {
                _submittedViews.Add(SubmittedCardView.Create(_targetArea, uiFont));
            }
        }

        private void BuildFeedback(RectTransform root)
        {
            _feedbackText = UIFactory.CreateText("Feedback", root, string.Empty, 40, UITheme.Positive,
                TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorBottom(_feedbackText.rectTransform, 64f, 276f, 40f);
        }

        private void BuildHandArea(RectTransform root)
        {
            var panel = UIFactory.CreatePanel("HandPanel", root, UITheme.PanelSoft);
            UIFactory.AnchorBottom(panel.rectTransform, 262f, 0f);
            panel.raycastTarget = false;

            var title = UIFactory.CreateText("HandTitle", panel.transform, "내 손패", 32, UITheme.TextMuted,
                TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorTop(title.rectTransform, 44f, 10f, 30f);

            _handRow = UIFactory.CreateRect("HandRow", panel.transform);
            UIFactory.Stretch(_handRow, 30f, 20f, 30f, 58f);
            UIFactory.AddHorizontalLayout(_handRow.gameObject, 16f, TextAnchor.MiddleCenter);

            for (int i = 0; i < _game.MaxHandCards; i++)
            {
                var view = HandCardView.Create(_handRow, uiFont, this);
                view.DoubleClickSeconds = _game.DoubleClickSeconds;
                view.gameObject.SetActive(false);
                _handViews.Add(view);
            }
        }

        private void BuildCardMenu(RectTransform root)
        {
            var dim = UIFactory.CreatePanel("CardMenu", root, UITheme.Dim);
            UIFactory.StretchAll(dim.rectTransform);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(CloseCardMenu);
            _menuRoot = dim.gameObject;

            // --- 버리기 / 분해하기 선택
            var choice = UIFactory.CreatePanel("ChoicePanel", dim.transform, UITheme.Panel, SpriteFactory.RoundedLarge);
            UIFactory.AnchorCenter(choice.rectTransform, 680f, 440f, Vector2.zero);
            _choicePanel = choice.gameObject;

            _menuTitle = UIFactory.CreateText("Title", choice.transform, "카드", 44, UITheme.TextDark,
                TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorTop(_menuTitle.rectTransform, 70f, 28f, 20f);

            var discardButton = UIFactory.CreateButton("DiscardButton", choice.transform, "버리기", 38,
                UITheme.ButtonDanger, uiFont, OnDiscardPressed);
            UIFactory.AnchorCenter(discardButton.GetComponent<RectTransform>(), 260f, 96f, new Vector2(-150f, 32f));

            _decomposeButton = UIFactory.CreateButton("DecomposeButton", choice.transform, "분해하기", 38,
                UITheme.ButtonPrimary, uiFont, OnDecomposePressed);
            UIFactory.AnchorCenter(_decomposeButton.GetComponent<RectTransform>(), 260f, 96f, new Vector2(150f, 32f));
            _decomposeButtonImage = _decomposeButton.GetComponent<Image>();

            _menuNotice = UIFactory.CreateText("Notice", choice.transform, string.Empty, 28, UITheme.Negative,
                TextAnchor.MiddleCenter, uiFont);
            UIFactory.AnchorCenter(_menuNotice.rectTransform, 620f, 60f, new Vector2(0f, -50f));

            var closeButton = UIFactory.CreateButton("CloseButton", choice.transform, "닫기", 32,
                UITheme.ButtonNeutral, uiFont, CloseCardMenu);
            UIFactory.AnchorBottom(closeButton.GetComponent<RectTransform>(), 72f, 24f, 220f);

            // --- 인수쌍 선택
            var pair = UIFactory.CreatePanel("PairPanel", dim.transform, UITheme.Panel, SpriteFactory.RoundedLarge);
            UIFactory.AnchorCenter(pair.rectTransform, 900f, 440f, Vector2.zero);
            _pairPanel = pair.gameObject;

            _pairTitle = UIFactory.CreateText("PairTitle", pair.transform, "어떻게 나눌까요?", 42, UITheme.TextDark,
                TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorTop(_pairTitle.rectTransform, 70f, 28f, 20f);

            _pairRow = UIFactory.CreateRect("PairRow", pair.transform);
            UIFactory.Stretch(_pairRow, 30f, 110f, 30f, 110f);
            UIFactory.AddHorizontalLayout(_pairRow.gameObject, 20f, TextAnchor.MiddleCenter);

            var backButton = UIFactory.CreateButton("BackButton", pair.transform, "뒤로", 32,
                UITheme.ButtonNeutral, uiFont, ShowChoicePanel);
            UIFactory.AnchorBottom(backButton.GetComponent<RectTransform>(), 72f, 24f, 330f);

            _menuRoot.SetActive(false);
        }

        private void BuildResultPanel(RectTransform root)
        {
            var dim = UIFactory.CreatePanel("ResultRoot", root, UITheme.Dim);
            UIFactory.StretchAll(dim.rectTransform);
            _resultRoot = dim.gameObject;

            var panel = UIFactory.CreatePanel("ResultPanel", dim.transform, UITheme.Panel, SpriteFactory.RoundedLarge);
            UIFactory.AnchorCenter(panel.rectTransform, 760f, 520f, Vector2.zero);

            var title = UIFactory.CreateText("ResultTitle", panel.transform, "게임 끝!", 62, UITheme.TextDark,
                TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorTop(title.rectTransform, 90f, 40f, 20f);

            _resultScoreText = UIFactory.CreateText("ResultScore", panel.transform, "최종 점수 0점", 48,
                UITheme.ButtonPrimary, TextAnchor.MiddleCenter, uiFont, FontStyle.Bold);
            UIFactory.AnchorCenter(_resultScoreText.rectTransform, 700f, 70f, new Vector2(0f, 30f));

            _resultClearedText = UIFactory.CreateText("ResultCleared", panel.transform, "클리어한 목표 0개", 38,
                UITheme.TextMuted, TextAnchor.MiddleCenter, uiFont);
            UIFactory.AnchorCenter(_resultClearedText.rectTransform, 700f, 60f, new Vector2(0f, -40f));

            var restartButton = UIFactory.CreateButton("RestartButton", panel.transform, "다시 시작", 38,
                UITheme.ButtonPrimary, uiFont, OnRestartPressed);
            UIFactory.AnchorBottom(restartButton.GetComponent<RectTransform>(), 90f, 36f, 240f);

            _resultRoot.SetActive(false);
        }

        /// <summary>
        /// 드래그하는 동안 포인터를 따라다니는 카드 그림.
        /// 실제 카드는 레이아웃 안에 있어 움직이면 줄이 흐트러지므로, 원본은 흐리게 두고
        /// 이 사본만 움직인다. 드롭 대상 판정을 가리지 않도록 레이캐스트는 꺼 둔다.
        /// </summary>
        private void BuildDragGhost(RectTransform root)
        {
            _dragGhostFace = CardFace.Create("DragGhost", root, HandCardView.CardHeight, uiFont, 72,
                UITheme.DragGhostBorder, UITheme.DragGhostFill);
            _dragGhostFace.SetRaycastTarget(false);

            _dragGhost = _dragGhostFace.Root;
            _dragGhost.anchorMin = new Vector2(0.5f, 0.5f);
            _dragGhost.anchorMax = new Vector2(0.5f, 0.5f);
            _dragGhost.pivot = new Vector2(0.5f, 0.5f);

            _dragGhost.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- 갱신

        private void HandleGameStarted()
        {
            _resultRoot.SetActive(false);
            CloseCardMenu();
            _feedbackText.text = string.Empty;
            _feedbackTimer = 0f;
        }

        private void HandleTargetChanged()
        {
            _targetView.Refresh(_game.Target);
            RefreshSubmittedCards();
        }

        /// <summary>제출한 약수 카드를 제출 순서대로 목표 카드 오른쪽에 늘어놓는다.</summary>
        private void RefreshSubmittedCards()
        {
            var collected = _game.Target.CollectedDivisors;
            for (int i = 0; i < _submittedViews.Count; i++)
            {
                bool active = i < collected.Count;
                _submittedViews[i].gameObject.SetActive(active);
                if (active) _submittedViews[i].Bind(collected[i]);
            }
        }

        private void RefreshTopBar()
        {
            _scoreText.text = "점수 " + _game.Score;
            _handCountText.text = "손패 " + _game.Hand.Count + "/" + _game.MaxHandCards;

            bool canDraw = _game.IsPlaying && _game.Hand.CanDraw;
            _drawButton.interactable = canDraw;
            _drawButtonImage.color = canDraw ? UITheme.ButtonPrimary : UITheme.ButtonDisabled;
        }

        private void RefreshHand()
        {
            var hand = _game.Hand;
            for (int i = 0; i < _handViews.Count; i++)
            {
                bool active = i < hand.Count;
                _handViews[i].gameObject.SetActive(active);
                if (active) _handViews[i].Bind(i, hand.GetCard(i));
            }

            RefreshTopBar();

            // 손패가 바뀌면 열려 있던 팝업의 대상 인덱스가 어긋날 수 있으므로 닫는다.
            if (_menuRoot.activeSelf && !hand.IsValidIndex(_menuCardIndex)) CloseCardMenu();
        }

        private void ShowFeedback(string message, bool positive)
        {
            _feedbackText.text = message;
            _feedbackText.color = positive ? UITheme.Positive : UITheme.Negative;
            _feedbackTimer = feedbackDuration;
        }

        private void ShowResult()
        {
            CloseCardMenu();
            _resultScoreText.text = "최종 점수 " + _game.Score + "점";
            _resultClearedText.text = "클리어한 목표 " + _game.ClearedTargetCount + "개";
            _resultRoot.SetActive(true);
        }

        // ---------------------------------------------------------------- 입력 처리

        public void OnCardClicked(int index)
        {
            if (_menuRoot.activeSelf || _resultRoot.activeSelf) return;
            _game.SubmitCard(index);
        }

        public void OnCardMenuRequested(int index)
        {
            if (_resultRoot.activeSelf) return;
            OpenCardMenu(index);
        }

        public void OnCardBeginDrag(int index, PointerEventData eventData)
        {
            if (_menuRoot.activeSelf || _resultRoot.activeSelf) return;

            _dragGhostFace.SetNumber(_game.Hand.GetCard(index));
            _dragGhost.gameObject.SetActive(true);
            MoveDragGhost(eventData);
        }

        public void OnCardDrag(PointerEventData eventData)
        {
            if (!_dragGhost.gameObject.activeSelf) return;
            MoveDragGhost(eventData);
        }

        public void OnCardEndDrag()
        {
            _dragGhost.gameObject.SetActive(false);
        }

        /// <summary>카드를 다른 카드 위에 놓으면 두 수를 곱해 한 장으로 합친다.</summary>
        public void OnCardDropped(int fromIndex, int toIndex)
        {
            if (_menuRoot.activeSelf || _resultRoot.activeSelf) return;
            _game.MergeCards(fromIndex, toIndex);
        }

        private void MoveDragGhost(PointerEventData eventData)
        {
            // Screen Space - Overlay 캔버스이므로 카메라는 null을 넘긴다.
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, eventData.position, null, out Vector2 localPoint))
            {
                _dragGhost.anchoredPosition = localPoint;
            }
        }

        private void OpenCardMenu(int index)
        {
            if (!_game.Hand.IsValidIndex(index)) return;

            _menuCardIndex = index;
            _menuTitle.text = _game.Hand.GetCard(index) + " 카드를 어떻게 할까요?";

            bool canDecompose = _game.Hand.CanDecomposeAt(index);
            _decomposeButton.interactable = canDecompose;
            _decomposeButtonImage.color = canDecompose ? UITheme.ButtonPrimary : UITheme.ButtonDisabled;
            _menuNotice.text = canDecompose ? string.Empty : _game.GetDecomposeBlockMessage(index);

            ShowChoicePanel();
            _menuRoot.SetActive(true);
        }

        private void ShowChoicePanel()
        {
            _choicePanel.SetActive(true);
            _pairPanel.SetActive(false);
        }

        private void CloseCardMenu()
        {
            _menuCardIndex = -1;
            _menuRoot.SetActive(false);
        }

        private void OnDiscardPressed()
        {
            int index = _menuCardIndex;
            CloseCardMenu();
            _game.DiscardAt(index);
        }

        private void OnDecomposePressed()
        {
            int index = _menuCardIndex;
            if (!_game.Hand.CanDecomposeAt(index)) return;

            var pairs = _game.Hand.GetFactorPairsAt(index);
            _pairTitle.text = _game.Hand.GetCard(index) + "을(를) 어떻게 나눌까요?";

            for (int i = _pairRow.childCount - 1; i >= 0; i--) Destroy(_pairRow.GetChild(i).gameObject);

            foreach (var pair in pairs)
            {
                var chosen = pair;
                var button = UIFactory.CreateButton("Pair", _pairRow, chosen.A + " × " + chosen.B, 40,
                    UITheme.ButtonPrimary, uiFont, () => OnPairChosen(chosen));
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, 120f);
                var layoutElement = button.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 200f;
                layoutElement.preferredHeight = 120f;
            }

            _choicePanel.SetActive(false);
            _pairPanel.SetActive(true);
        }

        private void OnPairChosen(FactorPair pair)
        {
            int index = _menuCardIndex;
            CloseCardMenu();
            _game.DecomposeAt(index, pair);
        }

        private void OnRestartPressed()
        {
            _resultRoot.SetActive(false);
            _game.RestartGame();
        }
    }
}
