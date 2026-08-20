using System;
using System.Collections.Generic;

namespace DivisorGame.Core
{
    /// <summary>
    /// 어떤 수를 두 인수의 곱으로 나타낸 쌍. 항상 A &lt;= B 로 정규화된다.
    /// (R12 "분해하기"의 인수쌍 후보로 사용)
    /// </summary>
    public readonly struct FactorPair : IEquatable<FactorPair>
    {
        public readonly int A;
        public readonly int B;

        public FactorPair(int a, int b)
        {
            if (a <= b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public int Product => A * B;

        public bool Equals(FactorPair other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is FactorPair other && Equals(other);
        public override int GetHashCode() => (A * 397) ^ B;
        public override string ToString() => A + " × " + B;
    }

    /// <summary>
    /// 약수 관련 순수 수학 유틸리티. UnityEngine에 의존하지 않아 EditMode 테스트로 직접 검증한다. (T1)
    /// </summary>
    public static class FactorUtil
    {
        /// <summary>number의 모든 약수를 오름차순으로 반환한다. number가 1 미만이면 빈 목록.</summary>
        public static List<int> GetDivisors(int number)
        {
            var result = new List<int>();
            if (number < 1) return result;

            var larger = new List<int>();
            for (int i = 1; (long)i * i <= number; i++)
            {
                if (number % i != 0) continue;
                result.Add(i);
                int paired = number / i;
                if (paired != i) larger.Add(paired);
            }

            for (int i = larger.Count - 1; i >= 0; i--) result.Add(larger[i]);
            return result;
        }

        /// <summary>number의 약수 개수. (목표 카드의 "n/전체" 분모, 점수 공식의 약수개수)</summary>
        public static int GetDivisorCount(int number) => GetDivisors(number).Count;

        /// <summary>value가 number의 약수인지 판정한다. (R5/R6 정답 판정)</summary>
        public static bool IsDivisorOf(int value, int number)
        {
            if (value < 1 || number < 1) return false;
            return number % value == 0;
        }

        /// <summary>소수 판정. 1 이하는 소수가 아니다.</summary>
        public static bool IsPrime(int number)
        {
            if (number < 2) return false;
            if (number % 2 == 0) return number == 2;
            for (int i = 3; (long)i * i <= number; i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        /// <summary>합성수(1과 자기 자신 외의 약수를 가지는 수) 판정. 1과 소수는 false.</summary>
        public static bool IsComposite(int number) => number >= 4 && !IsPrime(number);

        /// <summary>
        /// 분해 가능한 인수쌍 목록. a &lt;= b 형태로 중복 없이 오름차순으로 반환한다.
        ///
        /// 2 이상인 수는 언제나 1 × 자기자신으로 나눌 수 있으므로 그 쌍도 포함한다.
        /// 소수도 분해할 수 있어야 한다는 요구에 맞춘 것이고, 합성수에도 같은 규칙을 적용해
        /// "2 이상이면 1을 떼어낼 수 있다"로 규칙을 통일했다.
        /// 1은 더 쪼갤 수 없으므로 빈 목록을 반환한다.
        ///
        /// 예: 2 → 1×2 / 7 → 1×7 / 12 → 1×12, 2×6, 3×4 / 16 → 1×16, 2×8, 4×4
        /// </summary>
        public static List<FactorPair> GetFactorPairs(int number)
        {
            var pairs = new List<FactorPair>();
            if (number < 2) return pairs;

            for (int a = 1; (long)a * a <= number; a++)
            {
                if (number % a != 0) continue;
                pairs.Add(new FactorPair(a, number / a));
            }
            return pairs;
        }

        /// <summary>분해할 수 있는 수인지. 2 이상이면 최소한 1 × 자기자신으로 나눌 수 있다.</summary>
        public static bool CanBeSplit(int number) => number >= 2;
    }
}
