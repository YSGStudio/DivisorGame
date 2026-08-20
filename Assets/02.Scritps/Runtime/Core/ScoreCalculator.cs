using System;

namespace DivisorGame.Core
{
    /// <summary>
    /// 목표 클리어 점수 공식 (R8, T2).
    /// 점수 = max(기본점수 - 경과초 × 초당감점, 최소점수)
    /// 기본점수 = baseScorePerDivisor × 약수개수, 최소점수 = minScorePerDivisor × 약수개수
    ///
    /// 세 상수(10, 2, 5)는 밸런싱을 위해 코드 상수가 아니라 Inspector에 노출되는 값으로 둔다.
    /// GameManager의 필드로 직렬화되어 에디터에서 바로 조정할 수 있다.
    /// </summary>
    [Serializable]
    public class ScoreCalculator
    {
        public int baseScorePerDivisor = 10;
        public int penaltyPerSecond = 2;
        public int minScorePerDivisor = 5;

        public ScoreCalculator()
        {
        }

        public ScoreCalculator(int baseScorePerDivisor, int penaltyPerSecond, int minScorePerDivisor)
        {
            this.baseScorePerDivisor = baseScorePerDivisor;
            this.penaltyPerSecond = penaltyPerSecond;
            this.minScorePerDivisor = minScorePerDivisor;
        }

        /// <summary>
        /// 약수 개수와 경과 시간(초)으로 획득 점수를 계산한다.
        /// 경과 시간은 초 단위로 내림 처리한다(화면에 보이는 타이머 값과 일치시키기 위함).
        /// </summary>
        public int Calculate(int divisorCount, float elapsedSeconds)
        {
            if (divisorCount <= 0) return 0;

            int seconds = elapsedSeconds <= 0f ? 0 : (int)Math.Floor(elapsedSeconds);
            int baseScore = baseScorePerDivisor * divisorCount;
            int minScore = minScorePerDivisor * divisorCount;
            int score = baseScore - seconds * penaltyPerSecond;

            return score < minScore ? minScore : score;
        }
    }
}
