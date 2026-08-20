using System.Collections.Generic;
using DivisorGame.Core;
using NUnit.Framework;

namespace DivisorGame.Tests
{
    /// <summary>T1 / AC4, AC9 검증. 1, 소수, 완전제곱수, 약수가 많은 수 등 경계값을 포함한다.</summary>
    public class FactorUtilTests
    {
        [Test]
        public void GetDivisors_Of1_ReturnsOnly1()
        {
            CollectionAssert.AreEqual(new[] { 1 }, FactorUtil.GetDivisors(1));
        }

        [Test]
        public void GetDivisors_OfPrime_ReturnsOneAndItself()
        {
            CollectionAssert.AreEqual(new[] { 1, 7 }, FactorUtil.GetDivisors(7));
        }

        [Test]
        public void GetDivisors_OfPerfectSquare_DoesNotDuplicateRoot()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 8, 16 }, FactorUtil.GetDivisors(16));
        }

        [Test]
        public void GetDivisors_OfHighlyCompositeNumber_IsAscendingAndComplete()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 6, 8, 12, 24 }, FactorUtil.GetDivisors(24));
        }

        [Test]
        public void GetDivisors_Of12_MatchesAcceptanceCriteriaExample()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 6, 12 }, FactorUtil.GetDivisors(12));
            Assert.AreEqual(6, FactorUtil.GetDivisorCount(12));
        }

        [Test]
        public void GetDivisors_OfNonPositive_ReturnsEmpty()
        {
            Assert.IsEmpty(FactorUtil.GetDivisors(0));
            Assert.IsEmpty(FactorUtil.GetDivisors(-5));
        }

        [Test]
        public void GetDivisorCount_CoversWholeCardRange()
        {
            // 카드 숫자 범위 1~25 전체에서 약수 개수가 실제 나누어떨어지는 수의 개수와 일치해야 한다.
            for (int number = 1; number <= 25; number++)
            {
                int expected = 0;
                for (int candidate = 1; candidate <= number; candidate++)
                {
                    if (number % candidate == 0) expected++;
                }
                Assert.AreEqual(expected, FactorUtil.GetDivisorCount(number), "number = " + number);
            }
        }

        [Test]
        public void IsDivisorOf_JudgesCorrectly()
        {
            Assert.IsTrue(FactorUtil.IsDivisorOf(3, 12));
            Assert.IsTrue(FactorUtil.IsDivisorOf(12, 12));
            Assert.IsTrue(FactorUtil.IsDivisorOf(1, 12));
            Assert.IsFalse(FactorUtil.IsDivisorOf(5, 12));
            Assert.IsFalse(FactorUtil.IsDivisorOf(24, 12));
            Assert.IsFalse(FactorUtil.IsDivisorOf(0, 12));
        }

        [Test]
        public void IsPrime_HandlesBoundaries()
        {
            Assert.IsFalse(FactorUtil.IsPrime(0));
            Assert.IsFalse(FactorUtil.IsPrime(1));
            Assert.IsTrue(FactorUtil.IsPrime(2));
            Assert.IsTrue(FactorUtil.IsPrime(3));
            Assert.IsFalse(FactorUtil.IsPrime(4));
            Assert.IsTrue(FactorUtil.IsPrime(23));
            Assert.IsFalse(FactorUtil.IsPrime(25));
        }

        [Test]
        public void IsComposite_ExcludesOneAndPrimes()
        {
            Assert.IsFalse(FactorUtil.IsComposite(1));
            Assert.IsFalse(FactorUtil.IsComposite(2));
            Assert.IsFalse(FactorUtil.IsComposite(7));
            Assert.IsTrue(FactorUtil.IsComposite(4));
            Assert.IsTrue(FactorUtil.IsComposite(12));
            Assert.IsTrue(FactorUtil.IsComposite(25));
        }

        [Test]
        public void GetFactorPairs_Of12_IncludesOneTimesNAndHasNoDuplicates()
        {
            var pairs = FactorUtil.GetFactorPairs(12);
            CollectionAssert.AreEquivalent(
                new List<FactorPair> { new FactorPair(1, 12), new FactorPair(2, 6), new FactorPair(3, 4) },
                pairs);
        }

        [Test]
        public void GetFactorPairs_OfPerfectSquare_IncludesEqualPairOnce()
        {
            var pairs = FactorUtil.GetFactorPairs(16);
            CollectionAssert.AreEquivalent(
                new List<FactorPair> { new FactorPair(1, 16), new FactorPair(2, 8), new FactorPair(4, 4) },
                pairs);
        }

        [Test]
        public void GetFactorPairs_OfPrime_IsOneTimesItself()
        {
            // 소수도 1 x 자기자신으로 나눌 수 있어야 한다.
            CollectionAssert.AreEqual(new[] { new FactorPair(1, 2) }, FactorUtil.GetFactorPairs(2));
            CollectionAssert.AreEqual(new[] { new FactorPair(1, 7) }, FactorUtil.GetFactorPairs(7));
            CollectionAssert.AreEqual(new[] { new FactorPair(1, 23) }, FactorUtil.GetFactorPairs(23));
        }

        [Test]
        public void GetFactorPairs_OfOne_IsEmpty()
        {
            // 1은 더 쪼갤 수 없다(1 x 1로 나누면 카드가 무한히 늘어난다).
            Assert.IsEmpty(FactorUtil.GetFactorPairs(1));
            Assert.IsEmpty(FactorUtil.GetFactorPairs(0));
        }

        [Test]
        public void CanBeSplit_IsTrueForEverythingAboveOne()
        {
            Assert.IsFalse(FactorUtil.CanBeSplit(1));
            Assert.IsTrue(FactorUtil.CanBeSplit(2));
            Assert.IsTrue(FactorUtil.CanBeSplit(7));
            Assert.IsTrue(FactorUtil.CanBeSplit(25));
        }

        [Test]
        public void GetFactorPairs_AlwaysMultiplyBackToOriginal()
        {
            for (int number = 1; number <= 25; number++)
            {
                foreach (var pair in FactorUtil.GetFactorPairs(number))
                {
                    Assert.AreEqual(number, pair.Product, "number = " + number + ", pair = " + pair);
                    Assert.LessOrEqual(pair.A, pair.B);
                    Assert.GreaterOrEqual(pair.A, 1);
                }
            }
        }

        [Test]
        public void FactorPair_NormalizesOrder()
        {
            Assert.AreEqual(new FactorPair(2, 6), new FactorPair(6, 2));
        }
    }
}
