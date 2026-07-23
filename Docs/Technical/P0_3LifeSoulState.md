# P0.3 LifeState/SoulState와 파생 능력치 분리

> 문서 상태: `codex/p0-3-life-soul-state` 구현 기준
> 선행 기준선: [P0.1 현재 동작과 저장 형식 기준선](P0_1CharacterizationBaseline.md), [P0.2 안정 ID, legacy alias와 콘텐츠 카탈로그](P0_2StableIdContentCatalog.md)
> 목적: 현재 플레이 결과와 `SaveData.version = 3` JSON을 바꾸지 않고 생·혼백·통계·화면 상태의 소유권을 분리하고, 새 능력치 계산을 shadow mode로 검증한다.

## 1. 구현 범위와 안전 경계

P0.3은 새 상태 모델을 런타임에 병렬 도입한다. 기존 `PlayerData`, `RunData`, `GameManager`, `SaveManager`를 제거하거나 새 모델로 한 번에 교체하지 않는다. 기존 게임 흐름은 호환 계층이 계속 실행하고, 새 상태와 계산은 그 결과를 투영하고 비교한다.

이번 단계의 경계는 다음과 같다.

- `SaveData`의 필드, JSON 순서와 의미, payload `version = 3`, PlayerPrefs 키 `FirstForm.SaveData.v1`을 변경하지 않는다.
- 새 저장 DTO, migrator, backup, revision/checkpoint, 오프라인 진행을 구현하지 않는다.
- 최대 체력·최대 내력·공격·피해 감소·전투 내력 회복·수련 배율은 기존 `PlayerData` 계산을 실제 결과로 유지한다.
- `StatAggregationService`의 결과는 비교만 하고 `PlayerData`에 다시 적용하지 않는다. 보너스와 회복 효과는 정확히 한 번만 적용된다.
- 현재 생의 무공을 혼백의 발견·해금·기억으로 자동 승격하지 않는다.
- 첫 생의 `평범한 육신`, 검 주전투 계열 기본값, 생 번호 1 fallback을 그대로 유지한다.
- `LifeState.lifeId`는 현재 저장되지 않는 호환 식별자다. 재접속을 견디는 영구 ID는 P0.4 저장 계약에서 정한다.
- SampleScene, 프리팹, ScriptableObject, UI와 프로덕션 에셋은 변경하지 않는다.

## 2. 상태 소유권

| 수명 | 단일 원본 또는 호환 소유자 | 현재 포함 상태 | 저장 여부 |
| --- | --- | --- | --- |
| 여러 생 | `SoulState` | 혼백 포인트, 기존 혼백 성장 3종, 누적 사망·승리, 해금, 무공 발견·해금·기억 | 기존 필드만 legacy JSON에 투영 |
| 한 생 | `LifeState` | 생 번호, 출신, 주전투 계열, 현재 자원, 기본 진척, 경지, 현생 무공, 성향, 인벤토리 | P0.3에서는 저장하지 않음 |
| 한 생의 통계 | `LifeStatisticsState` | 생 번호, 격파, 도달 층, 기연, 출행 깊이, 생존 시간 | 기존 `RunData.currentRun`만 저장 |
| 한 접속·화면 | `SessionViewState` | 현재/이전 화면 상태, 전투 승리 요약, 일회성 화면 전환 flag | 저장하지 않음 |
| legacy 호출 호환 | `PlayerData`, `RunData`, `GameManager` | 기존 public field와 manager 호출 계약 | 기존 JSON 계약 유지 |

`LifeState`와 `SoulState`의 핵심 계약은 `Assets/Scripts/Domain/RuntimeStates.cs`에 있다. 이 타입들은 현재 `Assembly-CSharp`에 남아 P0.1 reflection 테스트와 SampleScene의 assembly 경계를 바꾸지 않는다. 별도 asmdef와 물리적 폴더 재배치는 상태·저장 경계가 안정화된 뒤 수행한다.

### 2.1 혼백 상태의 단일 원본

`SaveManager.CurrentSoulState`가 런타임 혼백 상태의 단일 원본이다. `SaveData`는 디스크 wire를 유지하기 위한 legacy DTO이고, `currentSaveData`는 이 원본에서 만든 호환 snapshot이다.

```text
legacy JSON
    ↓ TryLoadGame / ImportLegacy
SaveManager.CurrentSoulState ──────┐
    ↓ 같은 객체 참조              │
PlayerData.soulGrowthData           │
    ↓ 기존 호출 호환               │
SaveGame / legacy DTO snapshot  ←──┘
```

`PlayerData.soulGrowthData`는 별도 복제본을 수동 동기화하지 않고 `SoulState.legacyGrowth`와 같은 객체를 참조한다. 혼백 성장 구매, 혼백석 보상, 사망·승리 누적과 초기화는 이 원본을 먼저 변경한 뒤 기존 DTO snapshot에 투영한다. `SoulState` 객체 자체는 런타임 동안 교체하지 않아 이미 바인딩된 호환 참조가 끊어지지 않는다.

기존 저장에서 복원할 수 있는 혼백 정보는 혼백 포인트, 세 성장 레벨, 누적 사망·승리뿐이다. `SoulUnlockState`와 무공 발견·해금·기억 목록은 P0.3에서 빈 상태로 시작하며, 과거 JSON에 없던 정보를 추측해 생성하지 않는다.

### 2.2 한 생의 상태

`LifeState`는 현재 `PlayerData`와 `RunData`가 실제로 사용한 값을 담는 runtime projection이다.

- `resources`: 현재 체력과 내력
- `baseProgress`: 검법 숙련, 근력, 현행 수련으로 증가한 최대 내력, 누적 수련 시간
- `origin`: 생 번호 보정까지 끝나 `PlayerData`에 실제 적용된 출신 수치 snapshot
- `realm`: 기존 `RealmLevel` ordinal projection
- `martialArtProgress`: 이번 생에 실제 습득한 현생 무공
- `disposition`: 의협·냉혹·신의의 생 한정 상태
- `inventory`: 이번 생의 legacy stack item projection
- `legacyCombat`: 기존 무공 수치와 내력 회복 비대칭을 정확히 비교하기 위한 호환 입력

출신 수치는 `OriginDefinition`과 생 번호로 매번 재계산하지 않고, `ApplyBodyOrigin()`이 실제 적용한 값을 snapshot으로 남긴다. 이는 기존 환생 후보 생성 시점과 재로드 시점의 생 번호 보정 차이까지 characterization 대상으로 보존하기 위한 조치다. P0.3에서 이 차이를 수정하지 않는다.

새 생을 시작하면 새 `LifeState`를 만들고 `DispositionState`와 현생 무공의 숙련 진행 객체를 초기화한다. 같은 `SoulState`는 그대로 유지한다. 다만 기존 프로토타입은 선택한 입문 무공 자체를 육신 교체 뒤에도 유지하므로, P0.3은 이 플레이 결과를 바꾸지 않고 해당 선택을 새 생의 `MartialArtProgressState`로 다시 투영한다. 이는 혼백의 발견·해금·기억으로 승격된 것이 아니며 숙련 경험도 계승하지 않는다. 앱 재시작과 저장 불러오기는 `RunData.currentRun`을 복원할 뿐 생 번호를 자동 증가시키지 않는다.

### 2.3 현생 무공과 혼백 무공 정보

무공 상태는 다음 네 책임으로 나뉜다.

| 상태 | 소유권 | 의미 |
| --- | --- | --- |
| `MartialArtProgressState` | `LifeState` | 현재 생에서 습득한 무공과 현생 숙련 |
| `MartialArtDiscoveryState` | `SoulState` | 과거 생에서 발견한 무공과 습득 경로 정보 |
| `MartialArtUnlockState` | `SoulState` | 다음 생 시작 선택지로 사용할 자격 |
| `MartialArtMemoryState` | `SoulState` | 재습득 보정과 기억된 성취 |

기존 `firstFormSkill`은 호환 계층이 한 개의 `MartialArtProgressState`로 투영한다. 육신 교체 뒤에도 선택 무공이 남는 legacy 동작은 유지하지만, 전생에서 같은 무공을 사용했거나 legacy 저장에 무공 이름이 있다는 이유만으로 발견·해금·기억을 만들지 않는다. 생 종료 때 어떤 현생 진행을 어느 혼백 상태로 변환할지는 향후 생애 결산 규칙의 책임이다.

`SoulUnlockState`는 출신, 주전투 계열, 사건 정보, 자동 행동 규칙, 시작 선택지 ID를 직접 능력치와 분리해 보관한다. `UnlockEligibilityService`는 이 목록과 `MartialArtUnlockState.availableAsStartingChoice`만 질의하며 능력치에 보너스를 적용하지 않는다.

## 3. legacy 호환 계층

`Assets/Scripts/Domain/LegacyPlayerFacade.cs`가 기존 `PlayerData` public API와 새 상태 모델을 연결한다.

- `PlayerData`는 제거하거나 직렬화 필드를 바꾸지 않는다.
- 육신 적용, 경지 복원, 아이템 복원, 무공 선택과 직접 진척 변경 뒤 실제 legacy 값을 `LifeState`에 포착한다.
- `PlayerData.LifeState`, `PlayerData.SoulState`는 manager와 테스트가 새 소유권을 관찰하는 진입점이다.
- 혼백 성장 참조는 `SaveManager`의 단일 `SoulState`에 바인딩한다.
- 새 생 번호가 실제로 바뀔 때만 새 `LifeState`를 만든다.
- 호환 facade는 저장 DTO를 직렬화하거나 새로운 저장 key를 쓰지 않는다.

`RunData`의 기존 public field는 유지하고, `LifeStatisticsState`에 같은 값을 투영한다. 생존 시간처럼 기존 manager가 직접 가감하던 경로는 호환 메서드를 통해 두 표현을 함께 갱신한다. `GameManager`의 현재 화면, 직전 승리 요약과 일회성 flag는 `SessionViewState`가 소유하지만 기존 화면 전환 순서와 public 조회 결과는 유지한다.

## 4. 파생 능력치 shadow mode

`Assets/Scripts/Domain/StatAggregationService.cs`는 `LifeState`와 `SoulState`만 읽어 다음 결과를 순수 계산한다.

- 최대 체력
- 최대 내력
- 일반 또는 입문 무공 활성 공격 미리보기
- 지정 피해에 대한 피해 감소 결과
- 전투 내력 회복량
- 전체 검법 수련 배율

`LegacyPlayerFacade`는 같은 입력 시점에 기존 `PlayerData` 메서드 결과와 새 결과를 `StatShadowComparison`으로 비교한다. 정수 결과는 정확히 같아야 하고 배율은 작은 부동소수점 허용 오차 안에서 같아야 한다. 비교 결과는 진단과 테스트에만 사용하며 새 결과를 플레이어에게 적용하지 않는다.

```text
현재 manager 변경
    ↓
PlayerData 기존 필드와 공식 ───────→ 실제 게임 결과
    ↓ capture                         (권위 유지)
LifeState + SoulState
    ↓ StatAggregationService
shadow 결과 ───── compare ────────→ 진단 결과만 보관
```

다음 legacy 비대칭도 이 단계에서는 결함 수정 대상이 아니다.

- 출신 적용 때의 내력 회복 배율과 현생 중 `맑은 내력` 구매 때의 `+0.08` 직접 가산 방식
- 생 번호 보정이 끝난 출신 정수 보너스
- `rusty_sword`, `worn_training_robe`, `cracked_jade_token`의 stack 효과
- 현재 경지 ordinal과 기존 입문 무공의 공격·방어·수련 수치

호환 offset과 실제 적용 snapshot은 이 결과를 새 공식의 승인으로 굳히기 위한 것이 아니라, P0.3에서 무의식적으로 밸런스를 바꾸지 않기 위한 임시 비교 입력이다.

## 5. 저장 호환성

P0.3 이후에도 저장 wire field는 P0.1 기준선과 같다.

```text
version
selectedFirstFormSkillName
selectedFirstFormSkillType
currentRun
currentBodyName
currentRealmLevel
runItems[{itemId, stackCount}]
soulGrowthPoints
soulGrowth{soulToughnessLevel, residualSwordWillLevel, clearInternalEnergyLevel}
totalDeaths
totalBattleWins
savedAtUnixTime
```

`SoulState`, `LifeState`, `LifeStatisticsState`, `SessionViewState`, 무공 발견·해금·기억과 성향은 새 JSON 필드가 아니다. 기존 저장을 읽으면 복원 가능한 legacy 값만 런타임 상태로 import하고, 저장할 때 같은 legacy DTO로 되돌린다. P0.1 fixture는 수정하지 않는다.

따라서 다음 상태는 여전히 재접속 후 보존되지 않는다.

- 현재 체력·내력, 검법 숙련, 근력, 수련 시간과 경지 사이 진척
- `DispositionState`
- 현생 무공의 상세 숙련 경험
- 무공 발견·해금·기억과 새 해금 목록
- 생 통계 중 `currentRun` 이외 항목
- `SessionViewState`

이 누락은 P0.3 실패가 아니라 P0.4 저장 경계의 명시적 입력이다.

## 6. 테스트와 회귀 검증

P0.3 테스트는 기존 P0.1·P0.2 기대값을 수정하지 않고 다음을 추가로 고정한다.

- `SaveManager`와 `PlayerData`가 같은 `SoulState`와 `legacyGrowth` 객체를 참조하며 혼백 성장 변경이 복제본 없이 보이는지
- 새 생에서 `LifeState`, 생 한정 성향과 현생 숙련 객체는 초기화되고, legacy 입문 무공 선택만 새 현생 진행으로 다시 투영되는 동안 `SoulState` 객체와 혼백 목록은 유지되는지
- 현생 무공을 습득해도 혼백의 발견·해금·기억이 자동 생성되지 않는지
- 첫 생 fallback이 생 번호 1, `평범한 육신`, 검 주전투 계열로 유지되는지
- 저장 불러오기만으로 생 번호가 증가하지 않는지
- 출신·경지·입문 무공·아이템·혼백 성장 조합에서 legacy와 shadow 파생 능력치가 같은지
- shadow 비교가 체력, 내력, 아이템 또는 성장 보너스를 다시 적용하지 않는지
- `LifeStatisticsState`와 `SessionViewState`가 각자의 runtime 책임만 갖는지
- JSON에 새 상태 필드가 추가되지 않고 기존 fixture와 SampleScene이 바뀌지 않았는지

테스트 실행은 [P0.1 문서의 Unity 명령](P0_1CharacterizationBaseline.md#2-자동-테스트-구성과-실행)을 사용한다. P0.3의 구현 테스트 근거는 `Assets/Tests/EditMode/LifeSoulStateTests.cs`와 `Assets/Tests/PlayMode/LifeSoulStatePlayModeTests.cs`다.

## 7. 파일별 책임

| 파일 | P0.3 책임 |
| --- | --- |
| `Assets/Scripts/Domain/RuntimeStates.cs` | `SoulState`, `LifeState`, 무공 상태 4종, 성향, 생 통계, session/view 상태 |
| `Assets/Scripts/Domain/LegacyPlayerFacade.cs` | 기존 `PlayerData`와 새 상태의 호환 투영, shadow 비교 진입점 |
| `Assets/Scripts/Domain/StatAggregationService.cs` | 기존 결과와 동등해야 하는 순수 파생 능력치 계산 |
| `Assets/Scripts/Domain/UnlockEligibilityService.cs` | 해금 목록과 시작 선택 자격의 순수 질의 |
| `Assets/Scripts/Data/PlayerData.cs` | 기존 플레이 권위 유지, facade 접근점과 호환 변경 메서드 |
| `Assets/Scripts/Data/RunData.cs` | 기존 생 번호·통계와 `LifeStatisticsState` projection |
| `Assets/Scripts/Managers/SaveManager.cs` | 단일 `SoulState` 소유, legacy JSON import/export |
| `Assets/Scripts/Managers/GameManager.cs` | `SessionViewState`와 기존 상태 흐름 연결 |

## 8. P0.4로 넘기는 경계

P0.4는 P0.3 런타임 모델을 그대로 직렬화하는 작업이 아니다. 다음 저장 책임을 별도 DTO, mapper와 repository 경계에서 설계해야 한다.

- 지속 가능한 `lifeId` 생성 규칙과 생 번호 의미
- 현재 자원, 출신 stable ID, 경지 진척, 현생 무공 상세 진행, 성향과 stack inventory
- 혼백의 해금·무공 발견·해금·기억 저장과 생 종료 변환 provenance
- legacy `SaveData.version = 3` 원문 backup과 최소 version 판별
- 원문을 덮어쓰지 않는 shadow-write, 의미 검증, 실제 재로드 검증과 승격
- save revision, checkpoint sequence와 중복 정산 방지
- 알 수 없는 stable ID의 `UnresolvedReference` 격리

P0.4가 활성화되기 전에는 P0.3 상태를 새 저장이 이미 지원하는 것처럼 간주하거나 legacy JSON에서 복원 불가능한 값을 추정하지 않는다. 오프라인 수련과 범용 simulation tick은 P0.5 이후 책임이다.
