using System;
using DivisorGame.Core;
using UnityEngine;

namespace DivisorGame
{
    /// <summary>
    /// 게임 전체 진행을 총괄한다 (T5, T6, T11).
    /// 목표 카드 · 손패 · 점수 · 제출 판정 · 클리어 후 신규 목표 생성을 담당하고,
    /// 화면 갱신은 이벤트로 GameUI에 알린다(로직과 표현 분리).
    ///
    /// PRD R1은 목표 카드 4장 동시 노출이었으나, "너무 복잡하다"는 사용자 판단에 따라
    /// 목표 카드를 항상 1장만 두도록 바꿨다. 슬롯 인덱스 관리가 전부 사라져 규칙이 단순해졌다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        [Header("목표 카드 (R2, R9)")]
        [Tooltip("목표 숫자 최솟값")]
        [SerializeField] private int minTargetNumber = 1;
        [Tooltip("목표 숫자 최댓값")]
        [SerializeField] private int maxTargetNumber = 25;
        [Tooltip("클리어 후 새 목표가 등장하기까지의 연출 지연(초). R9에 따라 1초 이내로 유지할 것")]
        [SerializeField] private float newTargetDelaySeconds = 0.8f;

        [Header("손패 (R3, R4)")]
        [Tooltip("게임 시작 시 자동 지급되는 손패 장수")]
        [SerializeField] private int initialHandCards = 5;
        [Tooltip("손패 최대 장수")]
        [SerializeField] private int maxHandCards = 10;
        [Tooltip("손패 카드 숫자 최솟값")]
        [SerializeField] private int minCardNumber = 1;
        [Tooltip("손패 카드 숫자 최댓값")]
        [SerializeField] private int maxCardNumber = 25;
        [Tooltip("카드를 합쳐(곱해) 만들 수 있는 최대 숫자. 두 자리를 넘지 않게 99로 둔다")]
        [SerializeField] private int maxCardValue = 99;

        [Header("점수 공식 (R8) - 밸런싱용, 코드 수정 없이 조정 가능")]
        [SerializeField] private ScoreCalculator scoreCalculator = new ScoreCalculator();

        [Header("조작")]
        [Tooltip("손패 카드를 더블클릭으로 판단할 두 클릭 사이의 최대 간격(초)")]
        [SerializeField] private float doubleClickSeconds = 0.3f;

        private HandManager _hand;
        private TargetCardController _target;
        private float _respawnTimer;

        /// <summary>손패가 바뀌었을 때(장수·구성·선택 상태).</summary>
        public event Action OnHandChanged;

        /// <summary>점수 또는 클리어 수가 바뀌었을 때.</summary>
        public event Action OnScoreChanged;

        /// <summary>목표 카드의 구성이 바뀌었을 때(정답 제출 · 클리어 · 신규 목표 등장).</summary>
        public event Action OnTargetChanged;

        /// <summary>정답/오답/안내 피드백. (메시지, 긍정 여부)</summary>
        public event Action<string, bool> OnFeedback;

        /// <summary>종료 버튼으로 게임이 끝났을 때(R14).</summary>
        public event Action OnGameEnded;

        /// <summary>새 게임이 시작되었을 때.</summary>
        public event Action OnGameStarted;

        public bool IsPlaying { get; private set; }
        public int Score { get; private set; }
        public int ClearedTargetCount { get; private set; }

        /// <summary>화면에 떠 있는 단 하나의 목표 카드.</summary>
        public TargetCardController Target => _target;

        public HandManager Hand => _hand;
        public int MaxHandCards => maxHandCards;
        public float DoubleClickSeconds => doubleClickSeconds;

        /// <summary>
        /// 목표 숫자 범위에서 나올 수 있는 최대 약수 개수.
        /// 제출한 카드를 보여줄 자리를 미리 만들어 두기 위해 UI가 사용한다.
        /// </summary>
        public int MaxPossibleDivisorCount
        {
            get
            {
                int max = 1;
                for (int number = minTargetNumber; number <= maxTargetNumber; number++)
                {
                    max = Mathf.Max(max, FactorUtil.GetDivisorCount(number));
                }
                return max;
            }
        }

        private void Awake()
        {
            // GameUI가 Start에서 구독한 뒤 StartGame()을 호출하므로, 여기서는 그릇만 만든다.
            _hand = new HandManager(maxHandCards, maxCardValue);
            _target = new TargetCardController(RandomTargetNumber());
        }

        private void Update()
        {
            if (!IsPlaying) return;

            float dt = Time.deltaTime;

            if (_target.IsCleared)
            {
                // R9: 클리어된 목표는 짧은 연출 뒤 새 목표로 교체된다.
                _respawnTimer -= dt;
                if (_respawnTimer <= 0f)
                {
                    _target.Reset(RandomTargetNumber());
                    OnTargetChanged?.Invoke();
                }
            }
            else
            {
                _target.Tick(dt);
            }
        }

        /// <summary>새 게임을 시작한다. 목표 카드와 손패를 초기화한다(R3).</summary>
        public void StartGame()
        {
            Score = 0;
            ClearedTargetCount = 0;
            IsPlaying = true;

            _target.Reset(RandomTargetNumber());
            _respawnTimer = 0f;

            _hand.Clear();
            for (int i = 0; i < initialHandCards; i++) _hand.TryAdd(RandomCardNumber());

            OnGameStarted?.Invoke();
            OnTargetChanged?.Invoke();
            OnHandChanged?.Invoke();
            OnScoreChanged?.Invoke();
        }

        public void RestartGame() => StartGame();

        /// <summary>R14: 종료 버튼. 결과 화면으로 넘어간다.</summary>
        public void EndGame()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            OnHandChanged?.Invoke();
            OnGameEnded?.Invoke();
        }

        /// <summary>R4: 카드 가져오기. 손패가 가득 차 있으면 실패한다.</summary>
        public bool DrawCard()
        {
            if (!IsPlaying) return false;
            if (!_hand.CanDraw)
            {
                Feedback("손패가 가득 찼어요.  (" + _hand.Count + "/" + maxHandCards + ")", false);
                return false;
            }

            int value = RandomCardNumber();
            if (!_hand.TryAdd(value)) return false;

            Feedback("" + value + " 카드를 가져왔어요.", true);
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// R5/R6: 손패 카드를 목표 카드에 제출한다. 카드를 한 번 클릭하면 바로 호출된다.
        /// 정답이면 손패에서 제거되고, 오답이면 손패에 그대로 남으며 점수 차감도 없다.
        /// </summary>
        public void SubmitCard(int handIndex)
        {
            if (!IsPlaying || !_hand.IsValidIndex(handIndex)) return;

            if (_target.IsCleared)
            {
                Feedback("이미 클리어한 목표예요.", false);
                return;
            }

            int value = _hand.GetCard(handIndex);
            SubmitResult result = _target.Submit(value);

            switch (result)
            {
                case SubmitResult.Correct:
                    _hand.TryRemoveAt(handIndex);
                    OnHandChanged?.Invoke();
                    OnTargetChanged?.Invoke();

                    if (_target.IsCleared)
                    {
                        // R8: 클리어 시점의 경과 시간으로 점수를 확정한다.
                        int gained = scoreCalculator.Calculate(_target.TotalDivisorCount, _target.ElapsedSeconds);
                        Score += gained;
                        ClearedTargetCount++;
                        _respawnTimer = newTargetDelaySeconds;

                        OnScoreChanged?.Invoke();
                        Feedback(_target.Number + " 클리어!   +" + gained + "점", true);
                    }
                    else
                    {
                        Feedback("정답!   " + value + "은(는) " + _target.Number + "의 약수예요.   (" + _target.ProgressText + ")", true);
                    }
                    break;

                case SubmitResult.NotADivisor:
                    Feedback(value + "은(는) " + _target.Number + "의 약수가 아니에요.", false);
                    break;

                case SubmitResult.AlreadySubmitted:
                    Feedback(value + "은(는) 이미 냈어요.", false);
                    break;

                case SubmitResult.AlreadyCleared:
                    Feedback("이미 클리어한 목표예요.", false);
                    break;
            }
        }

        /// <summary>R11: 버리기.</summary>
        public void DiscardAt(int index)
        {
            if (!IsPlaying || !_hand.IsValidIndex(index)) return;

            int value = _hand.GetCard(index);
            if (!_hand.TryRemoveAt(index)) return;

            Feedback(value + " 카드를 버렸어요.", true);
            OnHandChanged?.Invoke();
        }

        /// <summary>R12: 분해하기.</summary>
        public bool DecomposeAt(int index, FactorPair pair)
        {
            if (!IsPlaying) return false;

            int value = _hand.GetCard(index);
            if (!_hand.TryDecomposeAt(index, pair))
            {
                Feedback(GetDecomposeBlockMessage(index), false);
                return false;
            }

            Feedback(value + " 카드를 " + pair.A + "와 " + pair.B + "로 나눴어요.", true);
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 손패 카드를 다른 손패 카드 위로 끌어다 놓아 두 수를 곱한 카드 한 장으로 합친다.
        /// 결과가 두 자리 수를 넘으면 막고 이유를 알려 준다.
        /// </summary>
        public bool MergeCards(int fromIndex, int toIndex)
        {
            if (!IsPlaying) return false;

            int a = _hand.GetCard(fromIndex);
            int b = _hand.GetCard(toIndex);

            if (!_hand.TryMerge(fromIndex, toIndex))
            {
                string message = GetMergeBlockMessage(fromIndex, toIndex);
                if (!string.IsNullOrEmpty(message)) Feedback(message, false);
                return false;
            }

            Feedback(a + "과 " + b + "을(를) 곱해 " + (a * b) + " 카드를 만들었어요.", true);
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>합치기가 막혔을 때 학생에게 보여줄 안내 문구.</summary>
        public string GetMergeBlockMessage(int fromIndex, int toIndex)
        {
            switch (_hand.GetMergeBlockReason(fromIndex, toIndex))
            {
                case MergeBlockReason.ResultTooLarge:
                    return _hand.GetCard(fromIndex) + " × " + _hand.GetCard(toIndex) + " = "
                           + _hand.GetMergeResult(fromIndex, toIndex)
                           + " 은(는) 두 자리 수를 넘어서 만들 수 없어요.";
                default:
                    return string.Empty;
            }
        }

        public DecomposeBlockReason GetDecomposeBlockReason(int index) => _hand.GetDecomposeBlockReason(index);

        /// <summary>R12: 분해 비활성화 시 학생에게 보여줄 안내 문구.</summary>
        public string GetDecomposeBlockMessage(int index)
        {
            switch (_hand.GetDecomposeBlockReason(index))
            {
                case DecomposeBlockReason.CannotBeSplit:
                    return "1은 더 나눌 수 없어요.";
                case DecomposeBlockReason.HandWouldOverflow:
                    return "손패가 가득 차 분해할 수 없어요.  (손패 " + (maxHandCards - 1) + "장 이하일 때만 가능)";
                default:
                    return string.Empty;
            }
        }

#if UNITY_EDITOR
        // ------------------------------------------------------------------
        // 에디터 전용 테스트 훅.
        // 목표 숫자와 손패가 무작위라 특정 상황(합성수 분해, 손패 가득 참 등)을 손으로 확인하기
        // 어려워서 추가했다. #if UNITY_EDITOR로 감싸 두어 플레이어 빌드에는 포함되지 않는다.
        // ------------------------------------------------------------------

        /// <summary>손패에 원하는 숫자 카드를 넣는다. 손패가 가득 차 있으면 실패한다.</summary>
        public bool DebugAddCard(int value)
        {
            if (!_hand.TryAdd(value))
            {
                Feedback("[테스트] 손패가 가득 찼어요.", false);
                return false;
            }

            Feedback("[테스트] " + value + " 카드를 손패에 넣었어요.", true);
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>목표 숫자를 원하는 값으로 바꾸고 타이머를 0으로 되돌린다.</summary>
        public void DebugSetTargetNumber(int number)
        {
            _target.Reset(number);
            _respawnTimer = 0f;

            Feedback("[테스트] 목표를 " + number + "로 바꿨어요.", true);
            OnTargetChanged?.Invoke();
        }

        /// <summary>손패를 상한까지 무작위 카드로 채운다(분해 제한 확인용).</summary>
        public void DebugFillHand()
        {
            while (_hand.CanDraw) _hand.TryAdd(RandomCardNumber());

            Feedback("[테스트] 손패를 " + _hand.Count + "장으로 채웠어요.", true);
            OnHandChanged?.Invoke();
        }

        /// <summary>손패를 모두 비운다.</summary>
        public void DebugClearHand()
        {
            _hand.Clear();

            Feedback("[테스트] 손패를 비웠어요.", true);
            OnHandChanged?.Invoke();
        }
#endif

        private void Feedback(string message, bool positive) => OnFeedback?.Invoke(message, positive);

        private int RandomTargetNumber() => UnityEngine.Random.Range(minTargetNumber, maxTargetNumber + 1);

        private int RandomCardNumber() => UnityEngine.Random.Range(minCardNumber, maxCardNumber + 1);
    }
}
