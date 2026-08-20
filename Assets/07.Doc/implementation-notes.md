# 구현 노트: 약수 카드게임 (Unity)

PRD(`prd.md`) T1~T12 구현 결과와, 이어서 작업할 사람이 알아야 할 사항을 정리한다.
WebGL 빌드(T13)는 뒤로 미뤘고, 현재는 **에디터에서 플레이해 확인하는 것**이 기본 검증 경로다.

## 에디터에서 실행하기

메뉴 **Factor Card Game → Open Scene and Play** 를 누르면 씬을 열고 바로 Play까지 들어간다.
(씬만 열려면 **Open Game Scene**. 직접 열려면 `Assets/01.Scene/DivisorCardGame.unity`.)

씬에는 `GameRoot` GameObject 하나뿐이고, 여기에 `GameManager`와 `GameUI`가 붙어 있다.
UI(캔버스·카드·버튼·팝업)는 전부 `GameUI`가 런타임에 코드로 생성하므로 프리팹이나
씬 편집이 필요 없다. 밸런싱 값은 `GameRoot`의 Inspector에서 조정한다.

### 조작

| 동작 | 결과 |
|---|---|
| 손패 카드 클릭 | **곧바로 제출** (선택 단계 없음) |
| 손패 카드 더블클릭 **또는 오른쪽 클릭** | 버리기 / 분해하기 팝업 |
| 손패 카드를 다른 손패 카드로 드래그 | 두 수를 곱해 한 장으로 합치기 (99 이하만) |

목표 카드는 표시 전용이라 클릭해도 아무 일도 일어나지 않는다.

**왜 오른쪽 클릭도 받는가**: 클릭이 곧 제출이라, 목표의 약수인 카드는 첫 클릭에 손패에서
사라져 더블클릭이 성립하지 않는다(예: 목표가 12인데 12를 3 × 4로 쪼개고 싶은 경우).
그래서 언제나 통하는 경로로 오른쪽 클릭을 함께 받는다. Unity WebGL 로더는
`disabledCanvasEvents: ["contextmenu", "dragstart"]`로 브라우저 우클릭 메뉴를 막으므로
웹에서도 그대로 동작한다.
또한 카드가 제출돼 사라지면 `HandCardView.Bind`가 더블클릭 연쇄를 끊어,
두 번째 클릭이 그 자리에 새로 온 다른 카드의 메뉴를 여는 일이 없다.

### 테스트 도구 (F1)

Play 중에 **F1** 을 누르면 오른쪽에 테스트 패널이 열린다. 목표 숫자와 손패가 무작위라
특정 상황을 우연에 기대지 않고 바로 만들어 보기 위한 것이다.

| 기능 | 용도 |
|---|---|
| **손패에 추가** + 1~25 격자 | 원하는 숫자 카드를 손패에 넣는다 |
| **목표 숫자 바꾸기** + 1~25 격자 | 목표 카드 숫자를 원하는 값으로 바꾼다 (타이머도 0으로) |
| **손패 가득 채우기** | 손패를 10장까지 채운다 |
| **손패 비우기** | 손패를 비운다 |

이 패널과 `GameManager`의 `Debug*` 메서드는 전부 `#if UNITY_EDITOR`로 감싸여 있어
플레이어 빌드에는 컴파일되지 않는다.

남은 사람 확인 항목(AC8, AC9)을 이 도구로 빠르게 확인하는 방법:

- **AC8 버리기**: 아무 카드나 더블클릭(또는 오른쪽 클릭)해 팝업 → 버리기.
- **AC9 분해 가능**: 손패에 추가로 `12`를 넣고 오른쪽 클릭 → 분해하기가 활성화되고
  인수쌍 `1 × 12`, `2 × 6`, `3 × 4`가 뜬다.
- **AC9 소수 분해**: `7`을 넣고 오른쪽 클릭 → 분해하기 활성, 인수쌍 `1 × 7`이 뜬다.
- **AC9 1은 분해 불가**: `1`을 넣고 오른쪽 클릭 → 분해하기가 회색으로 비활성화되고
  "1은 더 나눌 수 없어요." 문구가 뜬다.
- **AC9 손패 초과 시 분해 불가**: `12`를 넣고 **손패 가득 채우기**로 10장을 만든 뒤 `12`를 오른쪽 클릭
  → 분해하기 비활성화 + "손패가 가득 차 분해할 수 없어요." 문구.
- **AC4 빠른 클리어**: 목표 숫자 바꾸기로 목표를 `7`(약수 1, 7 두 개)로 바꾸고
  손패에 추가로 `1`, `7`을 넣어 제출하면 한 사이클이 몇 초 만에 끝난다.

## 한글 폰트

`Assets/04.Images/GothicA1-Regular.ttf`(Dynamic, `includeFontData: 1`)를 씬의
`GameRoot` → `GameUI` → **UI Font** 에 지정해 두었다. 레거시 uGUI `Text`가 쓰는
동적 폰트라 **실제 화면에 나온 글자만 런타임에 래스터화**되므로 WebGL 빌드도 가볍다.

같은 폴더의 `GothicA1-Regular SDF.asset`(33MB)은 TextMeshPro용 폰트 에셋이다.
현재 UI는 TMP가 아니라 레거시 `Text`로 만들어져 있어 **사용하지 않는다.**
참조되지 않는 에셋은 빌드에 포함되지 않으므로 그냥 두어도 무해하지만,
정리하고 싶다면 지워도 된다. (TMP로 전환하려면 `UIFactory`와 각 View의
`Text`를 `TextMeshProUGUI`로 바꿔야 하는데, 지금 구조에서 얻을 이점은 크지 않다.)

`uiFont`를 비우면 빌트인 `LegacyRuntime.ttf`로 대체되는데 여기엔 한글 글리프가 없어
글자가 깨진다. 폰트를 옮기거나 이름을 바꿀 때 이 참조가 끊기지 않았는지 확인할 것.

## 파일 구조

```
Assets/01.Scene/
  DivisorCardGame.unity          게임 씬 (빌드 설정 index 0)

Assets/04.Images/
  GothicA1-Regular.ttf           UI에 실제로 쓰는 한글 폰트
  GothicA1-Regular SDF.asset     TextMeshPro용 (현재 미사용)

Assets/02.Scritps/
  Runtime/
    DivisorGame.asmdef           런타임 어셈블리 (UnityEngine.UI, Unity.InputSystem 참조)
    Core/                        UnityEngine에 의존하지 않는 순수 로직 (단위 테스트 대상)
      FactorUtil.cs              약수 목록 · 소수 판정 · 인수쌍 생성            (T1)
      ScoreCalculator.cs         점수 공식                                      (T2)
      TargetCardController.cs    목표 카드 상태 · 타이머 · 제출 판정            (T3)
      HandManager.cs             손패 상한 · 버리기 · 분해 규칙                 (T4, T8, T9)
    Game/
      GameManager.cs             전체 진행 · 점수 · 신규 목표 생성              (T5, T6, T11)
    UI/
      GameUI.cs                  화면 전체 구성 및 갱신              (T3, T4, T7, T10, T11, T12)
      TargetCardView.cs          목표 카드 표시 (표시 전용)                     (T3)
      SubmittedCardView.cs       목표 카드 오른쪽에 이어 붙는 제출 약수 카드
      HandCardView.cs            손패 카드 표시 · 클릭 제출/메뉴/드래그 합치기   (T4, T7)
      DebugPanel.cs              에디터 전용 테스트 도구 (F1)
      UIFactory.cs               uGUI 요소 생성 헬퍼
      SpriteFactory.cs           둥근 모서리 스프라이트를 코드로 생성
      UITheme.cs                 색상 · 기준 해상도 상수
  Editor/
    GameSceneMenu.cs             씬 열기 / 열고 바로 플레이 메뉴
    WebGLBuilder.cs              WebGL 빌드 (메뉴 + CLI)
  Tests/
    EditMode/                    순수 로직 단위 테스트 (46개)
    PlayMode/                    씬을 띄워 확인하는 통합 테스트 (8개)
```

## 설계 메모

- **목표 카드는 항상 1장** (PRD R1에서 벗어남): PRD는 4장 동시 노출이었으나
  "너무 복잡하다"는 사용자 판단에 따라 1장으로 바꿨다. 슬롯 인덱스 관리가 전부
  사라져 `GameManager`·`GameUI`가 함께 단순해졌다.
- **분해 규칙: 2 이상이면 언제나 1을 떼어낼 수 있다** (PRD R12에서 벗어남):
  PRD는 합성수만 분해 가능했으나, 소수도 `1 × n`으로 나눌 수 있어야 한다는 요청에 맞춰
  규칙을 통일했다. 합성수도 `1 × n`이 후보에 함께 나온다(12 → 1×12, 2×6, 3×4).
  분해할 수 없는 값은 이제 **1뿐**이다.
  참고: 1은 모든 수의 약수이므로 `n → 1, n` 분해를 반복하면 1 카드를 계속 얻을 수 있다.
  손패 상한(10장) 때문에 무한정은 아니지만 난이도가 다소 낮아진다. 문제가 되면
  `FactorUtil.GetFactorPairs`에서 `a = 1`을 소수일 때만 포함하도록 좁히면 된다.
- **조작: 한 번 클릭하면 바로 제출** (PRD R5/R10에서 벗어남):
  PRD는 "손패 선택 → 목표 클릭" 2단계에 롱프레스 메뉴였으나, 사용자 요청에 따라
  클릭 한 번으로 제출하고 메뉴는 더블클릭/오른쪽 클릭으로 연다.
  "선택" 개념 자체가 사라져 `SelectedHandIndex`와 인덱스 보정 코드가 전부 제거됐다.
  더블클릭 간격은 `GameRoot` → `GameManager` → Double Click Seconds 로 조정한다.
- **합치기(곱하기)** (PRD에 없던 추가 기능): 손패 카드를 다른 손패 카드 위로 끌어다 놓으면
  두 수를 곱한 카드 한 장이 된다. 결과가 **두 자리 수(99)를 넘으면** 막고 이유를 알려 준다.
  분해하기의 반대 동작이라, 필요한 약수를 만들어 내는 두 가지 길이 생긴다
  (예: 목표 12에 6이 필요하면 2와 3을 합치거나, 큰 수를 분해해서 얻는다).
  손패는 2장이 빠지고 1장이 들어오므로 상한을 넘길 일이 없다.
  상한값은 `GameRoot` → `GameManager` → Max Card Value 에서 조정한다.
  드래그 중에는 원래 카드가 흐려지고 포인터를 따라다니는 사본이 보인다.
- **제출한 카드는 목표 카드 오른쪽에 이어 붙는다**: 손패에서 낸 카드가 그대로 옆으로
  옮겨간 것처럼 보이게 해서 지금까지 찾은 약수가 한눈에 들어온다. 낸 순서를 그대로
  유지하려고 `TargetCardController`는 확보 약수를 정렬하지 않는다.
  목표 카드 위치가 흔들리지 않도록 이 줄은 왼쪽 정렬이다.

- **로직과 표현 분리**: 규칙은 전부 `Core/`의 순수 C# 클래스에 있고 `GameManager`가 조율한다.
  `GameUI`는 이벤트를 구독해 표시만 한다. 덕분에 규칙 전체를 EditMode 테스트로 검증할 수 있다.
- **UI를 코드로 생성하는 이유**: 프리팹/씬 파일을 손으로 편집하면 병합·검증이 어렵고
  변경 이력이 남지 않는다. 화면 구조가 코드에 있으면 리뷰와 수정이 쉽다.
- **입력 시스템**: 이 프로젝트는 Input System 신규 전용(`activeInputHandler: 1`)이라
  레거시 `StandaloneInputModule`을 쓸 수 없다. `GameUI.EnsureEventSystem()`이
  `InputSystemUIInputModule`을 붙인다.
- **제출 조작**: PRD의 미확정 항목이었던 "드래그 앤 드롭 vs 클릭-클릭" 중
  **클릭-클릭**(손패 카드 선택 → 목표 카드 클릭)을 채택했다.
  변경하려면 `TargetCardView`/`HandCardView`의 포인터 처리를 교체하면 되고,
  `GameManager`의 규칙 코드는 그대로 쓸 수 있다.
- **손패 인덱스**: 손패는 값(int)의 리스트이고 화면은 인덱스 기준으로 매번 다시 바인딩한다.
  카드가 제거되면 `GameManager`가 선택 인덱스를 함께 보정한다.

## 자동 테스트 실행

Unity 에디터의 **Window → General → Test Runner** 에서 EditMode / PlayMode 탭으로 실행한다.
CLI로 돌리려면 (에디터를 닫은 상태에서):

```
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode -testResults results.xml -logFile unity.log
```

## 남은 작업

### WebGL 빌드 (T13)

메뉴 **약수 카드게임 → WebGL 빌드** 를 누르면 `Builds/WebGL`에 빌드된다.
CLI로는 (에디터를 닫은 상태에서):

```
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . -buildTarget WebGL \
  -executeMethod DivisorGame.EditorTools.WebGLBuilder.BuildFromCommandLine \
  -logFile webgl-build.log
```

빌드 결과는 `file://`로 직접 열면 브라우저 보안 정책 때문에 동작하지 않는다.
정적 서버로 열어야 한다:

```
cd Builds/WebGL && python3 -m http.server 8000
# 브라우저에서 http://localhost:8000
```

압축(gzip/Brotli)은 꺼 두었다. 웹서버가 `Content-Encoding` 헤더를 정확히 내려줘야 해서
로컬에서 간단히 열어 볼 때 실패하기 쉽기 때문이다. 실제 배포 시 서버 설정을 맞출 수 있다면
Player Settings → Publishing Settings에서 Brotli로 바꾸면 용량이 크게 준다.

### 자동 검증이 닿지 않은 부분

더블클릭 팝업(AC8)과 인수쌍 선택 UI(AC9)는 **포인터 입력 시뮬레이션 테스트가 없다.**
버리기/분해하기의 *규칙*은 단위 테스트로 검증했지만, *팝업이 실제로 뜨고 눌리는지*는
사람이 Play Mode에서 확인해야 한다(위의 F1 테스트 도구 활용). 레이아웃(AC11)도 마찬가지다.

### PRD에서 범위 밖으로 둔 것

- 효과음: 현재 무음이다. 정답/오답/클리어 효과음은 `GameManager.OnFeedback`에
  `AudioSource` 재생을 붙이면 된다.
- 최고 점수 영구 저장: 세션 내 점수만 다룬다(Non-Goals).
