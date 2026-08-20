using System;
using System.Collections.Generic;

namespace DivisorGame.Core
{
    /// <summary>"분해하기"가 막히는 이유 (R12).</summary>
    public enum DecomposeBlockReason
    {
        None,
        InvalidIndex,
        CannotBeSplit,
        HandWouldOverflow
    }

    /// <summary>"합치기"(곱하기)가 막히는 이유.</summary>
    public enum MergeBlockReason
    {
        None,
        InvalidIndex,
        SameCard,
        ResultTooLarge
    }

    /// <summary>
    /// 손패 관리 (T4, T8, T9).
    /// 최대 장수 제한(R3), 카드 가져오기(R4), 버리기(R11), 분해하기(R12)의 규칙을 담는다.
    /// 카드는 값(int)의 목록으로만 다루며, 화면 표시는 인덱스 기준으로 매번 다시 그린다.
    /// </summary>
    public class HandManager
    {
        public const int DefaultMaxCards = 10;

        /// <summary>합쳐서 만들 수 있는 카드의 최댓값. 두 자리 수를 넘지 않게 한다.</summary>
        public const int DefaultMaxCardValue = 99;

        private readonly List<int> _cards = new List<int>();

        public int MaxCards { get; }

        /// <summary>카드에 올 수 있는 최대 숫자. 합치기 결과가 이 값을 넘으면 막는다.</summary>
        public int MaxCardValue { get; }

        public HandManager(int maxCards = DefaultMaxCards, int maxCardValue = DefaultMaxCardValue)
        {
            MaxCards = maxCards < 1 ? 1 : maxCards;
            MaxCardValue = maxCardValue < 1 ? DefaultMaxCardValue : maxCardValue;
        }

        public IReadOnlyList<int> Cards => _cards;
        public int Count => _cards.Count;

        /// <summary>R3: 손패가 상한에 도달했는지.</summary>
        public bool IsFull => _cards.Count >= MaxCards;

        /// <summary>R4: "카드 가져오기" 버튼 활성화 여부.</summary>
        public bool CanDraw => !IsFull;

        public bool IsValidIndex(int index) => index >= 0 && index < _cards.Count;

        /// <summary>유효하지 않은 인덱스면 0을 반환한다.</summary>
        public int GetCard(int index) => IsValidIndex(index) ? _cards[index] : 0;

        public void Clear() => _cards.Clear();

        /// <summary>손패에 카드를 추가한다. 상한을 넘으면 실패한다.</summary>
        public bool TryAdd(int value)
        {
            if (IsFull) return false;
            _cards.Add(value);
            return true;
        }

        /// <summary>손패에서 카드를 제거한다(제출 성공 R5 / 버리기 R11 공용).</summary>
        public bool TryRemoveAt(int index)
        {
            if (!IsValidIndex(index)) return false;
            _cards.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// R12: 분해가 막히는 이유를 반환한다.
        /// 1이거나, 분해 결과(1장 제거 + 2장 추가)가 상한을 넘으면 막힌다.
        /// 2 이상은 최소한 1 × 자기자신으로 나눌 수 있으므로 소수도 분해할 수 있다.
        /// </summary>
        public DecomposeBlockReason GetDecomposeBlockReason(int index)
        {
            if (!IsValidIndex(index)) return DecomposeBlockReason.InvalidIndex;
            if (!FactorUtil.CanBeSplit(_cards[index])) return DecomposeBlockReason.CannotBeSplit;
            if (_cards.Count - 1 + 2 > MaxCards) return DecomposeBlockReason.HandWouldOverflow;
            return DecomposeBlockReason.None;
        }

        public bool CanDecomposeAt(int index) => GetDecomposeBlockReason(index) == DecomposeBlockReason.None;

        /// <summary>해당 손패 카드의 인수쌍 후보 목록.</summary>
        public List<FactorPair> GetFactorPairsAt(int index)
        {
            return IsValidIndex(index)
                ? FactorUtil.GetFactorPairs(_cards[index])
                : new List<FactorPair>();
        }

        /// <summary>
        /// 카드 두 장을 합쳐 곱한 값의 카드 한 장으로 만들 수 있는지 확인한다.
        /// 결과가 두 자리 수(MaxCardValue)를 넘으면 막는다.
        /// 손패는 2장이 빠지고 1장이 들어오므로 상한을 넘길 일은 없다.
        /// </summary>
        public MergeBlockReason GetMergeBlockReason(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex)) return MergeBlockReason.InvalidIndex;
            if (fromIndex == toIndex) return MergeBlockReason.SameCard;
            if (_cards[fromIndex] * _cards[toIndex] > MaxCardValue) return MergeBlockReason.ResultTooLarge;
            return MergeBlockReason.None;
        }

        public bool CanMerge(int fromIndex, int toIndex) =>
            GetMergeBlockReason(fromIndex, toIndex) == MergeBlockReason.None;

        /// <summary>두 카드를 합친 결과 값. 인덱스가 잘못됐으면 0.</summary>
        public int GetMergeResult(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex)) return 0;
            return _cards[fromIndex] * _cards[toIndex];
        }

        /// <summary>
        /// 카드 두 장을 곱한 값의 카드 한 장으로 합친다.
        /// 새 카드는 두 자리 중 앞쪽 자리에 놓아, 끌어다 놓은 자리에 생긴 것처럼 보이게 한다.
        /// </summary>
        public bool TryMerge(int fromIndex, int toIndex)
        {
            if (!CanMerge(fromIndex, toIndex)) return false;

            int product = _cards[fromIndex] * _cards[toIndex];
            int lower = Math.Min(fromIndex, toIndex);
            int higher = Math.Max(fromIndex, toIndex);

            _cards.RemoveAt(higher);
            _cards.RemoveAt(lower);
            _cards.Insert(lower, product);
            return true;
        }

        /// <summary>
        /// R12: 인수쌍을 골라 분해한다. 원래 카드를 제거하고 두 인수 카드를 손패에 추가한다.
        /// </summary>
        public bool TryDecomposeAt(int index, FactorPair pair)
        {
            if (!CanDecomposeAt(index)) return false;
            if (pair.A < 1 || pair.B < 1) return false;
            if (pair.Product != _cards[index]) return false;

            _cards.RemoveAt(index);
            _cards.Add(pair.A);
            _cards.Add(pair.B);
            return true;
        }
    }
}
