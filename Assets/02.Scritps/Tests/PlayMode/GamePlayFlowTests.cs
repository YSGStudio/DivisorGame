using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DivisorGame.Core;
using DivisorGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DivisorGame.Tests
{
    /// <summary>
    /// 씬을 실제로 띄워 런타임 UI 생성과 기본 플레이 사이클을 검증한다.
    /// (PRD Verification - Agent: "Play Mode 진입 및 기본 플로우가 예외 없이 동작하는지 확인")
    /// 무작위 요소는 시드를 고정해 재현 가능하게 만든다.
    /// </summary>
    public class GamePlayFlowTests
    {
        private const int Seed = 20260819;

        private GameManager _game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("DivisorCardGame", LoadSceneMode.Single);
            yield return null; // 씬 로드
            yield return null; // Start() 실행(UI 생성 + StartGame)

            _game = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(_game, "씬에 GameManager가 있어야 한다.");

            // 무작위 목표/손패를 재현 가능하게 만든 뒤 다시 시작한다.
            Random.InitState(Seed);
            _game.StartGame();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneBuildsUiWithoutErrors()
        {
            Assert.IsNotNull(Object.FindAnyObjectByType<Canvas>(), "런타임에 Canvas가 생성되어야 한다.");
            Assert.IsNotNull(EventSystem.current, "EventSystem이 생성되어야 한다.");

            var scaler = Object.FindAnyObjectByType<CanvasScaler>();
            Assert.IsNotNull(scaler);
            // R15 / AC11: 기준 해상도 1920x1080으로 스케일된다.
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 0.0001f);

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InitialState_MatchesRequirements()
        {
            // 목표 카드는 항상 한 장이고 값은 1~25.
            var target = _game.Target;
            Assert.IsNotNull(target);
            Assert.GreaterOrEqual(target.Number, 1);
            Assert.LessOrEqual(target.Number, 25);
            Assert.AreEqual("0/" + target.TotalDivisorCount, target.ProgressText);

            var targetViews = Object.FindObjectsByType<TargetCardView>(FindObjectsInactive.Include);
            Assert.AreEqual(1, targetViews.Length, "목표 카드는 화면에 한 장만 있어야 한다.");

            // R3 / AC3: 손패 5장, 값은 1~25.
            Assert.AreEqual(5, _game.Hand.Count);
            foreach (int value in _game.Hand.Cards)
            {
                Assert.GreaterOrEqual(value, 1);
                Assert.LessOrEqual(value, 25);
            }

            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(0, _game.ClearedTargetCount);

            var handViews = Object.FindObjectsByType<HandCardView>(FindObjectsInactive.Include);
            Assert.AreEqual(10, handViews.Length, "손패 뷰는 최대치만큼 미리 만들어 둔다.");
            Assert.AreEqual(5, handViews.Count(v => v.gameObject.activeSelf));

            // 아직 낸 카드가 없으므로 제출 카드는 하나도 보이지 않는다.
            Assert.IsEmpty(GetVisibleSubmittedCards());

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TargetTimer_CountsUpOverTime()
        {
            // R2 / AC2: 목표 카드 타이머가 0부터 시작해 시간에 따라 증가한다.
            // SetUp에서 이미 한 프레임이 지났으므로 "정확히 0"이 아니라 "아직 1초 미만"을 확인한다.
            float before = _game.Target.ElapsedSeconds;
            Assert.Less(before, 1f, "새 목표의 타이머는 0에 가까운 값에서 시작해야 한다.");

            yield return new WaitForSeconds(0.3f);

            float after = _game.Target.ElapsedSeconds;
            Assert.Greater(after, before, "타이머는 시간이 지나면 증가해야 한다.");
            Assert.GreaterOrEqual(after, 0.3f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DrawCard_FillsHandUpToMaxThenStops()
        {
            // R4 / AC3: 10장까지 늘어나고 그 이후로는 더 받을 수 없다.
            while (_game.Hand.CanDraw) Assert.IsTrue(_game.DrawCard());

            Assert.AreEqual(10, _game.Hand.Count);
            Assert.IsFalse(_game.Hand.CanDraw);
            Assert.IsFalse(_game.DrawCard());
            Assert.AreEqual(10, _game.Hand.Count);

            yield return null;

            var handViews = Object.FindObjectsByType<HandCardView>(FindObjectsInactive.Include);
            Assert.AreEqual(10, handViews.Count(v => v.gameObject.activeSelf));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WrongSubmission_KeepsCardAndDoesNotChangeScore()
        {
            // R6 / AC5: 약수가 아닌 값을 내면 손패에 그대로 남고 점수도 변하지 않는다.
            int number = _game.Target.Number;
            int handIndex = FindHandIndexOfNonDivisor(number);
            while (handIndex < 0 && _game.Hand.CanDraw)
            {
                _game.DrawCard();
                handIndex = FindHandIndexOfNonDivisor(number);
            }
            Assert.GreaterOrEqual(handIndex, 0, "약수가 아닌 카드를 확보하지 못했다.");

            int handCountBefore = _game.Hand.Count;
            int progressBefore = _game.Target.CollectedCount;

            _game.SubmitCard(handIndex);

            Assert.AreEqual(handCountBefore, _game.Hand.Count, "오답 카드는 손패에 남아야 한다.");
            Assert.AreEqual(progressBefore, _game.Target.CollectedCount);
            Assert.AreEqual(0, _game.Score, "오답으로 점수가 차감되면 안 된다.");

            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SubmittedCards_LineUpNextToTargetInSubmissionOrder()
        {
            // 제출한 카드가 목표 카드 오른쪽에 낸 순서대로 이어 붙는지 확인한다.
            int number = _game.Target.Number;
            int submitted = 0;
            int guard = 0;

            while (submitted < 2 && !_game.Target.IsCleared && guard++ < 5000)
            {
                if (!SubmitOrDiscardOneDraw(number)) continue;
                submitted++;
                yield return null;

                var visible = GetVisibleSubmittedCards();
                CollectionAssert.AreEqual(
                    _game.Target.CollectedDivisors.ToList(),
                    visible.Select(v => v.Value).ToList(),
                    "화면의 제출 카드는 제출 순서와 같아야 한다.");
            }

            Assert.GreaterOrEqual(submitted, 1, "약수를 한 장도 내지 못했다.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FullCycle_ClearsTargetScoresAndSpawnsNewTarget()
        {
            // R5→R7→R8→R9 / AC4, AC7 한 사이클.
            var target = _game.Target;
            int number = target.Number;

            int guard = 0;
            while (!target.IsCleared && guard++ < 5000) SubmitOrDiscardOneDraw(number);

            Assert.IsTrue(target.IsCleared, "목표 " + number + "의 모든 약수를 냈으면 클리어되어야 한다.");
            Assert.AreEqual(target.TotalDivisorCount + "/" + target.TotalDivisorCount, target.ProgressText);
            Assert.AreEqual(1, _game.ClearedTargetCount);
            Assert.Greater(_game.Score, 0, "클리어하면 점수를 얻어야 한다.");

            yield return null;

            // 클리어 순간에는 모든 약수가 카드로 늘어서 있어야 한다.
            Assert.AreEqual(target.TotalDivisorCount, GetVisibleSubmittedCards().Count);

            // R9 / AC7: 잠시 뒤 새 목표가 등장하고 타이머와 제출 카드가 초기화된다.
            yield return new WaitForSeconds(1.2f);

            Assert.IsFalse(_game.Target.IsCleared, "클리어 후 새 목표가 등장해야 한다.");
            Assert.AreEqual(0, _game.Target.CollectedCount);
            Assert.Less(_game.Target.ElapsedSeconds, 1f, "새 목표의 타이머는 0부터 시작해야 한다.");
            Assert.IsEmpty(GetVisibleSubmittedCards(), "새 목표에서는 제출 카드가 모두 사라져야 한다.");

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EndGame_StopsPlayAndKeepsFinalScore()
        {
            // R14 / AC10.
            Assert.IsTrue(_game.IsPlaying);

            _game.EndGame();
            yield return null;

            Assert.IsFalse(_game.IsPlaying);
            Assert.IsFalse(_game.DrawCard(), "종료 후에는 카드를 가져올 수 없다.");

            _game.RestartGame();
            yield return null;

            Assert.IsTrue(_game.IsPlaying);
            Assert.AreEqual(0, _game.Score);
            Assert.AreEqual(5, _game.Hand.Count);
            LogAssert.NoUnexpectedReceived();
        }

        // ---------------------------------------------------------------- 헬퍼

        /// <summary>카드를 한 장 뽑아, 목표의 약수면 제출하고 아니면 버린다. 제출했으면 true.</summary>
        private bool SubmitOrDiscardOneDraw(int number)
        {
            if (!_game.Hand.CanDraw) _game.DiscardAt(0);

            _game.DrawCard();
            int index = _game.Hand.Count - 1;
            int value = _game.Hand.GetCard(index);

            bool useful = FactorUtil.IsDivisorOf(value, number)
                          && !_game.Target.CollectedDivisors.Contains(value);

            if (!useful)
            {
                _game.DiscardAt(index);
                return false;
            }

            _game.SubmitCard(index);
            return true;
        }

        /// <summary>화면에 보이는 제출 카드를 왼쪽에서 오른쪽 순서(형제 순서)로 모은다.</summary>
        private static List<SubmittedCardView> GetVisibleSubmittedCards()
        {
            var any = Object.FindObjectsByType<SubmittedCardView>(FindObjectsInactive.Include);
            Assert.Greater(any.Length, 0, "제출 카드 자리가 미리 만들어져 있어야 한다.");

            Transform row = any[0].transform.parent;
            var result = new List<SubmittedCardView>();
            for (int i = 0; i < row.childCount; i++)
            {
                var view = row.GetChild(i).GetComponent<SubmittedCardView>();
                if (view != null && view.gameObject.activeSelf) result.Add(view);
            }
            return result;
        }

        private int FindHandIndexOfNonDivisor(int number)
        {
            for (int i = 0; i < _game.Hand.Count; i++)
            {
                if (!FactorUtil.IsDivisorOf(_game.Hand.GetCard(i), number)) return i;
            }
            return -1;
        }
    }
}
