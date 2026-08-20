using DivisorGame.Core;
using NUnit.Framework;

namespace DivisorGame.Tests
{
    /// <summary>T4, T8, T9 / AC3, AC9 검증. 손패 상한과 분해 규칙이 핵심이다.</summary>
    public class HandManagerTests
    {
        private HandManager _hand;

        [SetUp]
        public void SetUp()
        {
            _hand = new HandManager(10);
        }

        private void Fill(int count, int value = 7)
        {
            for (int i = 0; i < count; i++) _hand.TryAdd(value);
        }

        [Test]
        public void NewHand_IsEmptyAndCanDraw()
        {
            Assert.AreEqual(0, _hand.Count);
            Assert.IsTrue(_hand.CanDraw);
            Assert.IsFalse(_hand.IsFull);
        }

        [Test]
        public void TryAdd_StopsAtMaxCards()
        {
            for (int i = 0; i < 10; i++) Assert.IsTrue(_hand.TryAdd(i + 1), "i = " + i);

            Assert.AreEqual(10, _hand.Count);
            Assert.IsTrue(_hand.IsFull);
            Assert.IsFalse(_hand.CanDraw);
            Assert.IsFalse(_hand.TryAdd(11));
            Assert.AreEqual(10, _hand.Count);
        }

        [Test]
        public void TryRemoveAt_RemovesOnlyValidIndex()
        {
            _hand.TryAdd(3);
            _hand.TryAdd(8);

            Assert.IsFalse(_hand.TryRemoveAt(-1));
            Assert.IsFalse(_hand.TryRemoveAt(2));
            Assert.IsTrue(_hand.TryRemoveAt(0));
            Assert.AreEqual(1, _hand.Count);
            Assert.AreEqual(8, _hand.GetCard(0));
        }

        [Test]
        public void Decompose_IsBlockedOnlyForOne()
        {
            _hand.TryAdd(1);

            Assert.AreEqual(DecomposeBlockReason.CannotBeSplit, _hand.GetDecomposeBlockReason(0));
            Assert.IsFalse(_hand.CanDecomposeAt(0));
        }

        [Test]
        public void Decompose_IsAllowedForPrimes()
        {
            // 소수는 1 x 자기자신으로 나눌 수 있다.
            _hand.TryAdd(7);

            Assert.AreEqual(DecomposeBlockReason.None, _hand.GetDecomposeBlockReason(0));
            Assert.IsTrue(_hand.TryDecomposeAt(0, new FactorPair(1, 7)));
            CollectionAssert.AreEquivalent(new[] { 1, 7 }, _hand.Cards);
        }

        [Test]
        public void Decompose_IsAllowedForCompositeWithRoom()
        {
            _hand.TryAdd(12);

            Assert.AreEqual(DecomposeBlockReason.None, _hand.GetDecomposeBlockReason(0));
            Assert.IsTrue(_hand.CanDecomposeAt(0));
        }

        [Test]
        public void Decompose_IsAllowedAtNineCardsButNotAtTen()
        {
            // 분해는 1장을 빼고 2장을 넣으므로 손패가 9장 이하일 때만 가능하다.
            _hand.TryAdd(12);
            Fill(8);
            Assert.AreEqual(9, _hand.Count);
            Assert.AreEqual(DecomposeBlockReason.None, _hand.GetDecomposeBlockReason(0));

            Fill(1);
            Assert.AreEqual(10, _hand.Count);
            Assert.AreEqual(DecomposeBlockReason.HandWouldOverflow, _hand.GetDecomposeBlockReason(0));
            Assert.IsFalse(_hand.TryDecomposeAt(0, new FactorPair(3, 4)));
            Assert.AreEqual(10, _hand.Count);
        }

        [Test]
        public void TryDecomposeAt_ReplacesCardWithTwoFactors()
        {
            _hand.TryAdd(12);

            Assert.IsTrue(_hand.TryDecomposeAt(0, new FactorPair(3, 4)));
            Assert.AreEqual(2, _hand.Count);
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, _hand.Cards);
        }

        [Test]
        public void TryDecomposeAt_AtExactlyNineCards_ResultsInTenCards()
        {
            _hand.TryAdd(12);
            Fill(8);

            Assert.IsTrue(_hand.TryDecomposeAt(0, new FactorPair(2, 6)));
            Assert.AreEqual(10, _hand.Count);
            Assert.IsTrue(_hand.IsFull);
        }

        [Test]
        public void TryDecomposeAt_RejectsPairThatDoesNotMultiplyBack()
        {
            _hand.TryAdd(12);

            Assert.IsFalse(_hand.TryDecomposeAt(0, new FactorPair(3, 5)));
            Assert.AreEqual(1, _hand.Count);
            Assert.AreEqual(12, _hand.GetCard(0));
        }

        [Test]
        public void TryDecomposeAt_AcceptsOneTimesN()
        {
            _hand.TryAdd(12);

            Assert.IsTrue(_hand.TryDecomposeAt(0, new FactorPair(1, 12)));
            CollectionAssert.AreEquivalent(new[] { 1, 12 }, _hand.Cards);
        }

        [Test]
        public void TryDecomposeAt_RejectsPairWithZero()
        {
            _hand.TryAdd(12);

            Assert.IsFalse(_hand.TryDecomposeAt(0, new FactorPair(0, 12)));
            Assert.AreEqual(1, _hand.Count);
        }

        [Test]
        public void GetFactorPairsAt_ReturnsCandidatesForTheCard()
        {
            _hand.TryAdd(12);

            CollectionAssert.AreEquivalent(
                new[] { new FactorPair(1, 12), new FactorPair(2, 6), new FactorPair(3, 4) },
                _hand.GetFactorPairsAt(0));
        }

        // ---------------------------------------------------------------- 합치기(곱하기)

        [Test]
        public void TryMerge_MultipliesTwoCardsIntoOne()
        {
            _hand.TryAdd(3);
            _hand.TryAdd(4);

            Assert.IsTrue(_hand.TryMerge(0, 1));
            Assert.AreEqual(1, _hand.Count, "두 장이 빠지고 한 장이 들어와야 한다.");
            Assert.AreEqual(12, _hand.GetCard(0));
        }

        [Test]
        public void TryMerge_PlacesResultAtTheEarlierSlot()
        {
            _hand.TryAdd(9);
            _hand.TryAdd(2);
            _hand.TryAdd(5);

            // 뒤쪽(2번) 카드를 앞쪽(1번) 카드에 놓아도 결과는 앞자리에 온다.
            Assert.IsTrue(_hand.TryMerge(2, 1));
            CollectionAssert.AreEqual(new[] { 9, 10 }, _hand.Cards);
        }

        [Test]
        public void TryMerge_IsBlockedWhenResultExceedsTwoDigits()
        {
            _hand.TryAdd(12);
            _hand.TryAdd(9);

            // 12 x 9 = 108 은 세 자리라 막힌다.
            Assert.AreEqual(MergeBlockReason.ResultTooLarge, _hand.GetMergeBlockReason(0, 1));
            Assert.IsFalse(_hand.CanMerge(0, 1));
            Assert.IsFalse(_hand.TryMerge(0, 1));
            Assert.AreEqual(2, _hand.Count);
        }

        [Test]
        public void TryMerge_AllowsExactlyNinetyNine()
        {
            _hand.TryAdd(11);
            _hand.TryAdd(9);

            Assert.AreEqual(MergeBlockReason.None, _hand.GetMergeBlockReason(0, 1));
            Assert.IsTrue(_hand.TryMerge(0, 1));
            Assert.AreEqual(99, _hand.GetCard(0));
        }

        [Test]
        public void TryMerge_RejectsSameCardAndInvalidIndex()
        {
            _hand.TryAdd(3);
            _hand.TryAdd(4);

            Assert.AreEqual(MergeBlockReason.SameCard, _hand.GetMergeBlockReason(0, 0));
            Assert.IsFalse(_hand.TryMerge(0, 0));

            Assert.AreEqual(MergeBlockReason.InvalidIndex, _hand.GetMergeBlockReason(0, 5));
            Assert.IsFalse(_hand.TryMerge(0, 5));

            Assert.AreEqual(2, _hand.Count);
        }

        [Test]
        public void TryMerge_NeverOverflowsTheHand()
        {
            // 손패가 가득 차 있어도 합치기는 장수를 줄이므로 항상 가능하다.
            Fill(10, 3);
            Assert.IsTrue(_hand.IsFull);

            Assert.IsTrue(_hand.TryMerge(0, 1));
            Assert.AreEqual(9, _hand.Count);
            Assert.AreEqual(9, _hand.GetCard(0));
        }

        [Test]
        public void GetMergeResult_ReturnsProduct()
        {
            _hand.TryAdd(6);
            _hand.TryAdd(7);

            Assert.AreEqual(42, _hand.GetMergeResult(0, 1));
            Assert.AreEqual(0, _hand.GetMergeResult(0, 9));
        }

        [Test]
        public void MergedCard_CanBeSplitAgain()
        {
            // 합쳐서 만든 카드도 다시 분해할 수 있어야 한다.
            _hand.TryAdd(3);
            _hand.TryAdd(4);
            Assert.IsTrue(_hand.TryMerge(0, 1));

            Assert.IsTrue(_hand.CanDecomposeAt(0));
            Assert.IsTrue(_hand.TryDecomposeAt(0, new FactorPair(2, 6)));
            CollectionAssert.AreEquivalent(new[] { 2, 6 }, _hand.Cards);
        }

        [Test]
        public void GetDecomposeBlockReason_ForInvalidIndex()
        {
            Assert.AreEqual(DecomposeBlockReason.InvalidIndex, _hand.GetDecomposeBlockReason(0));
            Assert.AreEqual(DecomposeBlockReason.InvalidIndex, _hand.GetDecomposeBlockReason(-1));
        }

        [Test]
        public void GetCard_ForInvalidIndex_ReturnsZero()
        {
            Assert.AreEqual(0, _hand.GetCard(0));
        }
    }
}
