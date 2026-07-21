# P0.1 현재 동작과 저장 형식 기준선

> 문서 상태: `main` 커밋 `2e623be4e8fd393471b0c7b4319598d952319c18` 기준 characterization baseline
> 목적: P0.2 이후 리팩터링이 현재 프로토타입의 동작·밸런스·저장 호환성을 의도치 않게 바꾸면 자동 테스트로 감지한다.
> 해석 원칙: 이 문서와 테스트의 기대값은 현재 사실이지 목표 설계나 바람직한 동작을 뜻하지 않는다.

## 1. 범위와 변경 경계

이 기준선은 현재 `SaveData`/`SaveManager`, 핵심 콘텐츠 수치, `GameManager` 상태 흐름, `SampleScene.unity`의 부트스트랩 계약을 보존한다. 테스트는 런타임 타입을 `Assembly-CSharp`에서 reflection으로 찾아 호출하므로 게임 C# 코드와 assembly 경계를 바꾸지 않는다.

이번 단계에서 하지 않은 일은 다음과 같다.

- 새 저장 형식, migrator, stable ID, `LifeState`/`SoulState`, 오프라인 진행 구현
- 현재 결함과 밸런스 수정
- 런타임 C# 코드, 씬, 프리팹, ScriptableObject, 아트 에셋 변경
- Git 이력에 존재하는 스키마를 실제 배포·실사용 저장으로 간주하는 일

## 2. 자동 테스트 구성과 실행

| 구분 | assembly | 주요 검증 |
| --- | --- | --- |
| EditMode | `FirstForm.EditModeTests` | fixture 역직렬화, `Sanitize`, 실제 `SaveManager` 저장·불러오기, 콘텐츠·경지·육신·아이템·런 데이터, SampleScene 직렬화 계약 |
| PlayMode | `FirstForm.PlayModeTests` | SampleScene 런타임 초기화, 무공 선택 → 수련 → 출행 → 전투 → 승리/사망 → 육신 선택, 돌파 진입 기준 |

Unity Editor Test Runner에서 두 assembly를 각각 실행할 수 있다. 명령줄에서는 Unity `6000.5.0f1`을 사용한다. Unity 6의 `-runTests` 실행에는 `-quit`을 붙이지 않는다.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe'
$repo = 'D:\projects\FirstForm'

& $unity -runTests -batchmode -nographics `
  -projectPath $repo `
  -testPlatform EditMode `
  -assemblyNames 'FirstForm.EditModeTests' `
  -testResults "$repo\Logs\EditModeResults.xml" `
  -logFile "$repo\Logs\EditModeTests.log"

& $unity -runTests -batchmode -nographics `
  -projectPath $repo `
  -testPlatform PlayMode `
  -assemblyNames 'FirstForm.PlayModeTests' `
  -testResults "$repo\Logs\PlayModeResults.xml" `
  -logFile "$repo\Logs\PlayModeTests.log"
```

컴파일만 별도로 확인할 때는 다음 명령을 사용한다.

```powershell
& $unity -batchmode -nographics -projectPath $repo -quit -logFile "$repo\Logs\Compile.log"
```

테스트가 사용하는 PlayerPrefs 키 `FirstForm.SaveData.v1`은 각 테스트가 기존 값을 백업한 뒤 복원한다. 다만 Unity 프로세스를 강제 종료하거나 크래시가 발생하면 TearDown 복원이 실행되지 않을 수 있으므로, 실제 플레이 저장이 중요한 개발 환경에서는 테스트 전에 PlayerPrefs 원문을 별도로 보관한다.

## 3. 저장 fixture와 출처

fixture 경로는 [`Assets/Tests/Fixtures/SaveData`](../../Assets/Tests/Fixtures/SaveData)이다. 저장소와 Git 이력에는 실제 사용자 기기에서 수집한 원본 저장 JSON이나 배포 버전 표식이 없다. 따라서 네 버전 fixture는 각 커밋의 `SaveData` 공개 필드 순서와 당시 `SaveManager.BuildSaveData()`를 바탕으로 같은 sentinel 값을 넣어 재구성한 조사 표본이다. 존재가 확인된 wire shape일 뿐, 자동 지원이 확정된 배포 세대가 아니다.

| fixture | Git 근거 | 구분 기준 |
| --- | --- | --- |
| [`v1-initial.json`](../../Assets/Tests/Fixtures/SaveData/v1-initial.json) | `9fa3e3cbed25de2be9974f9ee631468787af6be9` | version 1, `soulGrowth` 없음 |
| [`v1-with-soul.json`](../../Assets/Tests/Fixtures/SaveData/v1-with-soul.json) | `43684870f8327a7cfbd45e07bc2cb814b743d2f8` | version 1, `soulGrowth` 있음 |
| [`v2-realm.json`](../../Assets/Tests/Fixtures/SaveData/v2-realm.json) | `0b08e49794ced4f7a4039ad71aa42e6f664fc0a5` | version 2, `currentRealmLevel` 추가 |
| [`v3-loot.json`](../../Assets/Tests/Fixtures/SaveData/v3-loot.json) | `a1a2884a8943d4034bc3fa95915a9fec12e16b98` 및 현재 main | version 3, `runItems` 추가 |

네 스키마의 PlayerPrefs 키는 모두 `FirstForm.SaveData.v1`이다. payload version과 저장 키 suffix는 독립적으로 움직인다. 같은 version 1인 두 shape는 `soulGrowth` 필드 존재 여부로 구분해야 한다.

오류 경계 fixture도 함께 둔다.

| fixture | 고정하는 현재 동작 |
| --- | --- |
| [`empty-object.json`](../../Assets/Tests/Fixtures/SaveData/empty-object.json) | `{}`를 유효 저장으로 받아들이고 현재 필드 initializer 기본값인 version 3·무공 없음으로 로드한다. |
| [`damaged-json.json`](../../Assets/Tests/Fixtures/SaveData/damaged-json.json) | 문법 손상은 로드 실패하며 원문 PlayerPrefs와 플레이어를 바꾸지 않는다. |
| [`sanitize-boundaries.json`](../../Assets/Tests/Fixtures/SaveData/sanitize-boundaries.json) | 음수·null·범위 밖 필드의 현재 `Sanitize` 결과를 고정한다. |
| [`unknown-and-invalid-items.json`](../../Assets/Tests/Fixtures/SaveData/unknown-and-invalid-items.json) | 미등록·즉시형·0 중첩 아이템 삭제, 유효 중첩 clamp, 중복 유지, 미래 version 허용을 고정한다. |
| [`death-after-save-v3.json`](../../Assets/Tests/Fixtures/SaveData/death-after-save-v3.json) | 사망 저장에 체력과 상태가 없어 같은 생이 정상 체력으로 로드되는 결과를 고정한다. |

현재 version 3의 wire field는 다음 순서다.

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

실제 첫 `SaveGame()`은 `currentBodyName`을 기본 표시명 `평범한 육신`으로, `savedAtUnixTime`을 현재 UTC Unix 시간으로 기록한다.

## 4. 저장·불러오기·Sanitize 기준선

근거 코드는 `Assets/Scripts/Data/SaveData.cs`, `Assets/Scripts/Managers/SaveManager.cs`, `Assets/Scripts/Data/PlayerData.cs`다.

### 4.1 저장과 로드

- `SaveGame()`은 `PlayerData`에서 무공 이름/enum ordinal, 육신 이름, 경지 ordinal, 런 아이템을 읽고 `RunData.currentRun`을 기록한다.
- 혼백 포인트·혼백 성장·누적 사망·승리는 기존 `SaveManager.currentSaveData`에서 이어받는다.
- `TryLoadGame()`은 version별 분기나 migrator 없이 현재 DTO로 곧바로 `JsonUtility.FromJson`을 호출한 뒤 `Sanitize()`한다.
- 로드만으로 원문 JSON을 다시 쓰지 않는다. 다음 명시적 저장에서 새 version 3 DTO로 재작성한다.
- 문법 손상, 빈 문자열, 키 없음은 `false`다. 플레이어는 바꾸지 않고 메모리의 `CurrentSaveData`만 현재 런타임 snapshot으로 다시 만든다.
- `{}`는 `true`다. Unity `6000.5.0f1`의 실제 실행에서는 현재 필드 initializer가 적용되어 `version == 3`, `selectedFirstFormSkillType == -1`, 무공 없음으로 로드된다.
- version 999도 거부하지 않는다. 이후 저장하면 version 3으로 조용히 낮아진다.

### 4.2 `Sanitize()`

- version은 최소 1만 보장하며 상한은 없다.
- 무공 ordinal은 `-1..2`, 생 번호는 최소 1, 경지는 `0..2`로 제한한다.
- 혼백 포인트·누적 사망·누적 승리는 최소 0, 혼백 성장 3종은 각각 `0..5`다.
- null 문자열은 빈 문자열, null 아이템 목록과 혼백 성장은 빈 객체로 바꾼다.
- null/미등록/즉시 사용/0 이하 중첩 아이템은 제거하고 유효 중첩은 현재 최대 3으로 제한한다.
- 같은 아이템 ID의 중복 entry는 합치거나 제거하지 않는다. 다만 `PlayerData.RestoreRunInventory()`는 첫 entry만 적용하고 뒤의 중복을 무시한다.
- `savedAtUnixTime`은 음수여도 그대로 둔다.
- 미등록 아이템은 메모리에서 제거되지만 로드만으로 원문은 바뀌지 않는다. 다음 저장 때 원문에서도 소실된다.

## 5. 콘텐츠와 밸런스 snapshot

### 5.1 입문 무공

| 순서/ordinal | 이름 | 공격 보정 | 방어·회피 보정 | 내력 비용 | 수련 배율 |
| --- | --- | ---: | ---: | ---: | ---: |
| 0 | 청풍검식 | +5 | 0.04 | 2 | 1.15 |
| 1 | 파문검식 | +1 | 0 | 4 | 1.05 |
| 2 | 회류보 | -2 | 0.28 | 0 | 1.00 |

`FirstFormSkillManager.FindCandidate()`는 이름을 먼저 찾고, 실패하면 enum ordinal을 fallback으로 사용한다.

### 5.2 육신

| 이름 | 체력 | 내력 | 검법 | 근력 | 공격 | 검법 성장 | 내력 회복 | 받는 피해 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 검문 제자 | +12 | +8 | +18 | +1 | +2 | 1.75 | 1.00 | 0.95 |
| 마교 잡역 | +55 | -8 | +2 | +7 | +9 | 0.85 | 0.55 | 0.92 |
| 약밭 견습 | +30 | +30 | +6 | -2 | -4 | 1.05 | 1.65 | 0.78 |

N번째 생에는 다섯 정수 보너스가 모두 `2 * (N - 1)`만큼 추가되고 배율은 그대로다. 후보 풀과 표시 후보 수가 모두 3이어서 매 생 세 육신이 전부 나오며 순서만 무작위다. 후보 snapshot은 저장하지 않는다.

### 5.3 경지

현재 enum과 표시명은 `Initiate/입문 = 0`, `Tempered/단련 = 1`, `Skilled/숙련 = 2`다. 목표 설계의 삼류~화경 18단계가 아니다.

| 돌파 | 검법 | 근력 | 최대 내력 |
| --- | ---: | ---: | ---: |
| 입문 → 단련 | 30 | 20 | 75 |
| 단련 → 숙련 | 80 | 38 | 105 |

한 단계 보너스는 최대 체력 +25, 최대 내력 +12, 경지 공격 +2, 받는 피해 배율 -0.04, 새 최대치 기준 체력·내력 35% 회복이다. 기본 캐릭터가 청풍검식을 선택하면 15 수련 틱에 검법 30, 근력 27, 최대 내력 75가 되어 약 30초에 돌파 선택으로 들어간다. 자동 출행 35초보다 먼저 수련이 중단되는 현재 결과를 PlayMode 테스트로 고정한다.

### 5.4 아이템

| ID | 종류 | 지속 | 최대 중첩 | 핵심 효과 |
| --- | --- | --- | ---: | --- |
| `rusty_sword` | Weapon | 현 생 | 3 | 중첩당 공격 배율 +10% |
| `worn_training_robe` | Clothing | 현 생 | 3 | 중첩당 최대·현재 체력 +20 |
| `cracked_jade_token` | Accessory | 현 생 | 3 | 중첩당 최대 내력 +10, 내력 회복 배율 +10% |
| `small_healing_pill` | Consumable | 즉시 | 1 | 최대 체력의 30% 회복 |
| `faded_soul_stone` | SoulItem | 즉시 | 1 | 혼백 포인트 +1 |

`LootItemCatalog.CreateAll()`은 복사본이 아니라 동일한 내부 배열을 반환한다. 이 역시 현재 계약으로 감시한다.

## 6. 핵심 상태 흐름 기준선

SampleScene의 정상 신규 시작과 테스트가 고정하는 흐름은 다음과 같다.

```text
None
  → FirstFormSelection
  → Training
  → Exploration
  → Battle
  → BattleVictory
  → Training 또는 Exploration
  → Battle
  → Death
  → BodySelection
  → Training (currentRun + 1, 입문 무공 유지)
```

- 무공 선택, 살아남은 사건 선택, 사망 보상, 새 육신 시작, 전투 승리, 돌파 성공 때 저장한다.
- 수련 틱, 출행 진입, 승리 뒤 계속 출행/수련 복귀, 돌파 실패는 저장하지 않는다.
- 승리 시 처치 수와 층, 혼백 포인트, 무작위 loot, 검법, 행운을 갱신한 뒤 저장하고 `BattleVictory`로 간다.
- 사망 시 출행 깊이를 0으로 만든 뒤 사망·혼백 보상을 저장하고 나서 `Death`로 간다.
- 여러 공개 상태 전환 메서드는 현재 상태 guard가 없고, `ReincarnationManager.SelectBody()`만 `BodySelection`을 검사한다.

## 7. SampleScene 계약

근거 씬은 `Assets/Scenes/SampleScene.unity`이며 빌드 설정의 활성화된 첫 씬이다.

- 씬 GUID: `8c9cfa26abfee488c85f1582747f6a02`
- 기준 SHA-256: `CD9B0E1AB53D4599B62ED33E3E0BE7D367C6656CB250C43055591D063977ACF2`
- 루트: `Main Camera`, `Global Light 2D`, `GameRoot`
- `GameRoot` 직렬화 컴포넌트: Transform, `GameManager`, `UIManager`
- `GameManager.startingState`: 1, 즉 `FirstFormSelection`
- GameManager의 manager inspector 참조 10개: 모두 null
- UIManager의 수동 패널·텍스트·버튼 참조: 모두 null
- `UIManager.enableKeyboardShortcuts`: true
- 필수 한글 TMP 폰트: `Assets/Art/Fonts/Pretendard-Regular SDF.asset`

null manager 참조는 씬 누락이 아니라 현재 부트스트랩 계약이다. `GameManager.Awake()`가 UIManager를 제외한 아홉 manager를 `GameRoot`에 붙여 초기화하고, `UIManager.Initialize()`가 `RuntimeUIBuilder`를 통해 Canvas, EventSystem, 패널과 버튼을 만든다. EditMode는 직렬화 상태를, PlayMode는 이 런타임 연결 결과를 각각 검증한다.

씬에는 코드에 나중에 추가된 다수의 직렬화 필드가 YAML로 기록되어 있지 않다. 필드 initializer 변경만으로 씬 diff 없이 시작 동작이 달라질 수 있다.

## 8. 현재 저장되지 않는 상태와 회귀 위험

| 범주 | 현재 저장되지 않는 값 | 결과 |
| --- | --- | --- |
| 플레이어 | 체력·내력·검법·근력·총 수련 시간과 현재 계산된 최대치 | 재로드 시 육신·혼백·경지·아이템 정의로 다시 계산한다. |
| 런 | 처치 수, 도달 층, 행운, 출행 깊이, 생존 시간 | 같은 생 번호라도 진행 세부가 초기화된다. |
| 상태 | `FirstFormGameState`, 현재 활동, 돌파 가능/알림 flag | 재로드는 씬 `startingState`에서 시작한다. |
| 전투 | 적, 타이머, 강공 대응, 보상 중복 guard, RNG | 전투 도중 이어하기와 결정론적 재현이 없다. |
| 사건 | 현재 사건, 선택 대기, 예약 전투 보정 | 중단한 사건을 이어갈 수 없다. |
| 육신 선택 | 후보 배열과 RNG | 재진입 시 같은 후보 순서를 보장하지 않는다. |

특히 사망 처리에서 체력 0과 `Death` 상태를 저장하지 않는다. 앱을 종료하고 다시 열면 같은 `currentRun`, 육신, 무공, 경지, 아이템은 남지만 체력과 내력이 정상치로 재계산되고 SampleScene의 `FirstFormSelection`에서 시작한다. 저장에 이미 무공이 있어도 시작 상태 자체는 바뀌지 않는다. P0.1은 이 결함을 고치지 않고 fixture와 테스트로 드러낸다.

추가 저장 위험은 다음과 같다.

- PlayerPrefs 단일 문자열에 백업·원자적 교체·checksum·version dispatch가 없다.
- 육신은 표시 이름, 무공은 표시 이름과 enum ordinal, 아이템은 하드코딩 ID로 다시 찾는다. 콘텐츠 이름·순서·수치 변경이 같은 JSON의 복원 결과를 바꿀 수 있다.
- 같은 version 1에 서로 다른 두 wire shape가 존재한다.
- 미래 version을 거부하지 않고 다음 저장에서 3으로 낮춘다.
- 미등록 아이템은 다음 저장 때 되돌릴 수 없이 사라진다.
- 현재 save key 이름과 payload version이 일치하지 않아 key suffix만으로 스키마를 판정할 수 없다.

## 9. 다음 단계 진입 조건

P0.2 이후 구현 PR은 이 테스트를 먼저 통과해야 한다. 의도적으로 계약을 바꾸는 경우에는 코드 변경과 함께 어떤 characterization 기대값을 왜 바꾸는지 PR에 기록한다. 실제 사용자 저장 표본이나 배포 근거가 확보되기 전에는 이 조사 fixture만으로 지원해야 할 migrator chain을 확정하지 않는다.

이번 기준선은 런타임 C#을 변경하지 않았다. 테스트·fixture·이 문서만 추가했으며, 이후 stable ID나 새 저장 구조는 별도 PR에서 다룬다.
