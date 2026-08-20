using DivisorGame.Core;
using NUnit.Framework;

namespace DivisorGame.Tests
{
    /// <summary>T2 / AC6 검증. 기본 상수는 기본점수 10, 초당감점 2, 최소점수 5이다.</summary>
    public class ScoreCalculatorTests
    {
        private ScoreCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new ScoreCalculator();
        }

        [Test]
        public void Calculate_AcceptanceCriteriaExample_Returns34()
        {
            // AC6: 약수 4개인 숫자를 3초 만에 클리어 → max(40 - 6, 20) = 34
            Assert.AreEqual(34, _calculator.Calculate(4, 3f));
        }

        [Test]
        public void Calculate_AtZeroSeconds_ReturnsFullBaseScore()
        {
            Assert.AreEqual(60, _calculator.Calculate(6, 0f));
        }

        [Test]
        public void Calculate_ClampsToMinimumScore()
        {
            // 약수 4개 → 기본 40, 최소 20. 100초가 지나도 20 아래로 내려가지 않는다.
            Assert.AreEqual(20, _calculator.Calculate(4, 100f));
        }

        [Test]
        public void Calculate_ExactlyAtMinimumBoundary()
        {
            // 약수 4개 → 기본 40, 최소 20. (40 - 20) / 2 = 10초에 최소점수에 도달한다.
            Assert.AreEqual(20, _calculator.Calculate(4, 10f));
            Assert.AreEqual(22, _calculator.Calculate(4, 9f));
        }

        [Test]
        public void Calculate_FloorsFractionalSeconds()
        {
            // 3.9초는 3초로 내림되어 화면 타이머 표시와 일치한다.
            Assert.AreEqual(34, _calculator.Calculate(4, 3.9f));
        }

        [Test]
        public void Calculate_NegativeElapsedIsTreatedAsZero()
        {
            Assert.AreEqual(40, _calculator.Calculate(4, -1f));
        }

        [Test]
        public void Calculate_WithNoDivisors_ReturnsZero()
        {
            Assert.AreEqual(0, _calculator.Calculate(0, 5f));
        }

        [Test]
        public void Calculate_HonoursCustomBalancingConstants()
        {
            var custom = new ScoreCalculator(20, 5, 2);
            // 기본 20*2 = 40, 감점 5*3 = 15, 최소 2*2 = 4 → 25
            Assert.AreEqual(25, custom.Calculate(2, 3f));
        }
    }
}
