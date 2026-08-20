using DivisorGame.Core;
using NUnit.Framework;

namespace DivisorGame.Tests
{
    /// <summary>T3 / AC2, AC2-1, AC4, AC5 검증.</summary>
    public class TargetCardControllerTests
    {
        [Test]
        public void NewTarget_StartsWithZeroProgressAndZeroTimer()
        {
            var target = new TargetCardController(12);

            Assert.AreEqual(12, target.Number);
            Assert.AreEqual(6, target.TotalDivisorCount);
            Assert.AreEqual(0, target.CollectedCount);
            Assert.AreEqual("0/6", target.ProgressText);
            Assert.AreEqual(0f, target.ElapsedSeconds);
            Assert.IsFalse(target.IsCleared);
        }

        [Test]
        public void Submit_Divisor_IsCorrectAndAdvancesProgress()
        {
            var target = new TargetCardController(12);

            Assert.AreEqual(SubmitResult.Correct, target.Submit(3));
            Assert.AreEqual("1/6", target.ProgressText);
            CollectionAssert.Contains(target.CollectedDivisors, 3);
        }

        [Test]
        public void Submit_NonDivisor_IsRejectedAndProgressUnchanged()
        {
            var target = new TargetCardController(12);

            Assert.AreEqual(SubmitResult.NotADivisor, target.Submit(5));
            Assert.AreEqual("0/6", target.ProgressText);
        }

        [Test]
        public void Submit_SameDivisorTwice_IsRejectedTheSecondTime()
        {
            var target = new TargetCardController(12);

            Assert.AreEqual(SubmitResult.Correct, target.Submit(2));
            Assert.AreEqual(SubmitResult.AlreadySubmitted, target.Submit(2));
            Assert.AreEqual("1/6", target.ProgressText);
        }

        [Test]
        public void Submit_AllDivisorsInAnyOrder_ClearsTarget()
        {
            // AC4: 12의 약수 1, 2, 3, 4, 6, 12를 순서 상관없이 모두 제출하면 클리어된다.
            var target = new TargetCardController(12);
            int[] shuffled = { 6, 1, 12, 3, 2, 4 };

            for (int i = 0; i < shuffled.Length; i++)
            {
                Assert.IsFalse(target.IsCleared, "제출 " + i + "회차에서 미리 클리어되면 안 된다.");
                Assert.AreEqual(SubmitResult.Correct, target.Submit(shuffled[i]));
            }

            Assert.IsTrue(target.IsCleared);
            Assert.AreEqual("6/6", target.ProgressText);
        }

        [Test]
        public void Submit_AfterCleared_ReturnsAlreadyCleared()
        {
            var target = new TargetCardController(5);
            target.Submit(1);
            target.Submit(5);

            Assert.IsTrue(target.IsCleared);
            Assert.AreEqual(SubmitResult.AlreadyCleared, target.Submit(1));
        }

        [Test]
        public void TargetOfOne_IsClearedBySingleSubmission()
        {
            var target = new TargetCardController(1);

            Assert.AreEqual("0/1", target.ProgressText);
            Assert.AreEqual(SubmitResult.Correct, target.Submit(1));
            Assert.IsTrue(target.IsCleared);
        }

        [Test]
        public void Tick_AccumulatesElapsedTimeUntilCleared()
        {
            var target = new TargetCardController(5);

            target.Tick(1.5f);
            target.Tick(1.5f);
            Assert.AreEqual(3f, target.ElapsedSeconds, 0.0001f);
            Assert.AreEqual(3, target.ElapsedSecondsFloored);

            target.Submit(1);
            target.Submit(5);
            target.Tick(10f);

            // 클리어 후에는 타이머가 멈춘다(점수 확정 시점 보존).
            Assert.AreEqual(3f, target.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void Reset_ReusesSlotWithFreshState()
        {
            var target = new TargetCardController(5);
            target.Tick(4f);
            target.Submit(1);
            target.Submit(5);
            Assert.IsTrue(target.IsCleared);

            target.Reset(9);

            Assert.AreEqual(9, target.Number);
            Assert.AreEqual(3, target.TotalDivisorCount);
            Assert.AreEqual("0/3", target.ProgressText);
            Assert.AreEqual(0f, target.ElapsedSeconds);
            Assert.IsFalse(target.IsCleared);
        }

        [Test]
        public void CollectedDivisors_KeepSubmissionOrder()
        {
            // 화면에서 목표 카드 오른쪽에 "낸 순서대로" 이어 붙어야 하므로 정렬하지 않는다.
            var target = new TargetCardController(12);
            target.Submit(6);
            target.Submit(1);
            target.Submit(3);

            CollectionAssert.AreEqual(new[] { 6, 1, 3 }, target.CollectedDivisors);
        }
    }
}
