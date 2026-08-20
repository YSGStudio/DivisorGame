# Checklist: 초등학교 5학년 대상 약수 카드게임 (유니티 웹/WebGL)

상태 표기: `[x]` 완료 · `[~]` 구현 완료, 사람 확인 필요 · `[ ]` 미완료

참고: `Assets/04.Images/GothicA1-Regular.ttf`를 UI 폰트로 지정해 화면 문구는 **한글**이다.

## Tasks

- [x] T1 `FactorUtil`(약수 목록, 소수 판정, 인수쌍 생성) 구현 및 단위 테스트 작성 (req: R1) (ac: AC4)
- [x] T2 `ScoreCalculator`(점수 공식) 구현 및 단위 테스트 작성 (req: R8) (ac: AC6)
- [x] T3 `TargetCardController` 구현: 목표 숫자, 필요/확보 약수 목록, 개별 타이머, `제출수/전체 약수수` 진행 표시 (req: R2, R2-1) (ac: AC1, AC2-1) (after: T1)
- [x] T4 `HandManager` 구현: 초기 5장 지급, 최대 10장 제한, 카드 가져오기 (req: R3) (ac: AC3)
- [x] T5 카드 제출 플로우(**클릭 한 번에 제출** → 정답/오답 판정, 피드백) (req: R5 변경) (ac: AC4) (after: T1, T3, T4)
      → 사용자 요청으로 "선택 후 목표 클릭" 2단계를 없앴다.
- [x] T6 목표 클리어 시 점수 반영 및 신규 목표 자동 생성 (req: R7) (ac: AC7) (after: T2, T3, T5)
- [~] T7 손패 카드 **더블클릭 / 오른쪽 클릭** → 버리기/분해하기 팝업 UI (req: R10 변경) (ac: AC8) (after: T4)
      → 사용자 요청으로 롱프레스 → 더블클릭. 클릭이 즉시 제출이라 우클릭 경로를 함께 둠.
        포인터 입력 시뮬레이션 테스트가 없어 사람 확인 필요.
- [x] T8 "버리기" 동작 구현 (req: R11) (ac: AC8) (after: T7)
- [x] T9 "분해하기" 동작 구현(1만 불가, 인수쌍 선택, 손패 초과 방지) (req: R12 변경) (ac: AC9) (after: T1, T7)
      → 사용자 요청으로 소수도 1×n 분해 가능. 규칙은 단위 테스트로 검증.
        인수쌍 선택 UI 조작은 사람 확인 필요.
- [x] T10 상단 UI(점수/손패 수/종료 버튼) 구현 및 실시간 갱신 (req: R13) (ac: AC10) (after: T4, T6)
- [x] T11 종료 버튼 → 결과 화면(최종 점수, 클리어 수) 구현 (req: R14) (ac: AC10) (after: T10)
- [~] T12 Canvas Scaler 기준 해상도(1920×1080) 설정 및 다양한 창 크기 검증 (req: R15) (ac: AC11) (after: T3, T4, T10)
      → 설정은 코드로 적용했고 PlayMode 테스트로 값까지 검증. 실제 창 크기별 레이아웃은 사람 확인 필요.
- [~] T13 WebGL 빌드 및 브라우저 실행 확인 (req: R15) (ac: AC11) (after: T12)
      → 빌드는 CLI로 성공 확인(61.6MB). 브라우저에서 직접 플레이해 보는 것은 사람 확인 필요.
        메뉴: 약수 카드게임 → WebGL 빌드. `implementation-notes.md` 참고.

## 추가 기능 (PRD 범위 밖, 사용자 요청)

- [x] 카드 합치기: 손패 카드를 다른 손패 카드로 드래그하면 두 수를 곱한 카드가 된다
      (결과가 두 자리 수 99를 넘으면 막고 안내). 규칙은 EditMode 테스트로 검증.
- [ ] 드래그 조작이 실제로 매끄럽게 되는지 Play Mode에서 사람 확인 필요
- [ ] 오른쪽 클릭으로 메뉴가 열리는지 Play Mode / WebGL에서 사람 확인 필요

## Acceptance Criteria

- [x] AC1 게임 시작 시 목표 카드 **1장**(사용자 요청으로 4장에서 변경), 값 1~25 — PlayMode `InitialState_MatchesRequirements`
- [x] AC2 목표 카드 타이머가 0부터 초 단위로 증가하는지 확인 — PlayMode `TargetTimers_CountUpOverTime`
- [x] AC2-1 목표 카드에 `제출한 개수/전체 약수 개수`가 표시되고 정답마다 갱신, 분자=분모면 클리어 — EditMode `TargetCardControllerTests`
- [x] AC3 손패 초기 5장, 10장까지 증가, 10장에서 버튼 비활성화 — PlayMode `DrawCard_FillsHandUpToMaxThenStops`
- [x] AC4 목표 숫자의 모든 약수를 제출하면 클리어 — EditMode + PlayMode `FullCycle_ClearsTargetScoresAndSpawnsNewTarget`
- [x] AC5 약수가 아닌 값 제출 시 손패 유지, 점수 미차감 — PlayMode `WrongSubmission_KeepsCardAndDoesNotChangeScore`
- [x] AC6 점수 공식(기본점수-경과초×2, 최소점수 하한) 단위 테스트 검증 — EditMode `ScoreCalculatorTests`
- [x] AC7 목표 클리어 직후 새 목표 카드 자동 생성 및 새 타이머 시작 — PlayMode `FullCycle_...`
- [~] AC8 더블클릭 팝업에서 "버리기" 선택 시 카드 삭제 — 버리기 로직은 검증됨. 팝업 조작은 사람 확인 필요.
- [~] AC9 "분해하기": 1만 비활성화(소수 포함 분해 가능), 인수쌍 선택 UI, 손패 9장 초과 시 비활성화
      — 규칙 전체 EditMode 검증 완료(`HandManagerTests`). UI 조작은 사람 확인 필요.
- [~] AC10 점수/손패 수 UI 실시간 갱신, 종료 버튼 → 결과 화면 전환
      — 종료/재시작 상태 전환은 PlayMode `EndGame_StopsPlayAndKeepsFinalScore`로 검증. 화면 전환은 사람 확인 필요.
- [ ] AC11 다양한 해상도/창 크기에서 레이아웃 정상 확인 — 사람 확인 필요

## Human Checks

에디터에서 Play 중 **F1**을 누르면 테스트 도구가 열린다(원하는 카드/목표 숫자를 바로 만들 수 있음).
항목별 확인 절차는 `implementation-notes.md`의 "테스트 도구" 절에 정리해 두었다.

- [ ] 에디터 Play Mode에서 전체 플로우를 직접 플레이해 확인
      (메뉴: Factor Card Game → Open Scene and Play)
- [ ] 손패 카드 더블클릭 → 버리기/분해하기 팝업이 실제로 뜨고 눌리는지 Play Mode에서 확인
- [ ] 나중에: WebGL 빌드를 브라우저에서 열어 확인 (선행: WebGL 모듈 설치)
- [ ] 초등학교 5학년 또는 유사 연령대에게 설명 없이 플레이시켜 규칙 이해도 확인
- [ ] 여러 모니터 해상도/브라우저 창 크기에서 레이아웃 점검
- [ ] 점수 공식 상수(10, 2, 5) 체감 난이도 확인 및 필요 시 밸런싱 조정 요청
      (씬의 `GameRoot` → `GameManager` → Score Calculator 에서 코드 수정 없이 조정 가능)
