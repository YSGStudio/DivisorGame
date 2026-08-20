using System.Collections.Generic;

namespace DivisorGame.Core
{
    /// <summary>카드 제출 결과 (R5 정답 / R6 오답).</summary>
    public enum SubmitResult
    {
        Correct,
        NotADivisor,
        AlreadySubmitted,
        AlreadyCleared
    }

    /// <summary>
    /// 목표 숫자 카드 한 장의 상태 (T3).
    /// 목표 숫자, 필요한 약수 목록, 확보한 약수 목록, 개별 타이머(R2), 진행 표시(R2-1)를 가진다.
    /// MonoBehaviour가 아니라 순수 C# 클래스이므로 EditMode 테스트로 직접 검증한다.
    /// </summary>
    public class TargetCardController
    {
        private readonly List<int> _required = new List<int>();
        private readonly List<int> _collected = new List<int>();

        public int Number { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool IsCleared { get; private set; }

        public IReadOnlyList<int> RequiredDivisors => _required;
        /// <summary>확보한 약수. 제출한 순서대로 들어 있다.</summary>
        public IReadOnlyList<int> CollectedDivisors => _collected;

        public int TotalDivisorCount => _required.Count;
        public int CollectedCount => _collected.Count;

        /// <summary>R2-1: "제출한 개수/전체 약수 개수" 표시 문자열 (예: "1/2").</summary>
        public string ProgressText => CollectedCount + "/" + TotalDivisorCount;

        /// <summary>화면에 표시할 경과 초(내림).</summary>
        public int ElapsedSecondsFloored => ElapsedSeconds <= 0f ? 0 : (int)ElapsedSeconds;

        public TargetCardController(int number)
        {
            Reset(number);
        }

        /// <summary>새 목표 숫자로 초기화한다. 슬롯 재사용(R9)에도 사용한다.</summary>
        public void Reset(int number)
        {
            Number = number;
            _required.Clear();
            _required.AddRange(FactorUtil.GetDivisors(number));
            _collected.Clear();
            ElapsedSeconds = 0f;
            IsCleared = false;
        }

        /// <summary>R2: 클리어되지 않은 동안 타이머를 증가시킨다.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsCleared || deltaSeconds <= 0f) return;
            ElapsedSeconds += deltaSeconds;
        }

        /// <summary>
        /// 값을 제출한다. 약수이면서 아직 제출되지 않았다면 확보 목록에 추가하고,
        /// 모든 약수가 모이면 클리어 상태가 된다(R7).
        /// </summary>
        public SubmitResult Submit(int value)
        {
            if (IsCleared) return SubmitResult.AlreadyCleared;
            if (!FactorUtil.IsDivisorOf(value, Number)) return SubmitResult.NotADivisor;
            if (_collected.Contains(value)) return SubmitResult.AlreadySubmitted;

            // 제출한 순서를 그대로 유지한다. 화면에서 목표 카드 오른쪽에 낸 순서대로 이어 붙는다.
            _collected.Add(value);

            if (_collected.Count >= _required.Count) IsCleared = true;
            return SubmitResult.Correct;
        }
    }
}
