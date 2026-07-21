# P0.2 안정 ID, legacy alias와 콘텐츠 카탈로그

> 문서 상태: `codex/p0-2-stable-id-content-catalog` 구현 기준
> 선행 기준선: [P0.1 현재 동작과 저장 형식 기준선](P0_1CharacterizationBaseline.md)
> 목적: 현재 저장 wire와 게임 결과를 바꾸지 않고 콘텐츠 의미를 표시명·Unity asset GUID·enum 순서에서 분리한다.

## 1. 구현 범위와 안전 경계

P0.2는 `GameContentCatalog`를 현재 콘텐츠 정의의 단일 원본으로 두고, 기존 manager가 `LegacyContentAdapter`를 통해 `FirstFormSkillData`, `BodyOriginData`, `ItemData`, `EnemyData`, `ExplorationEventData`를 받게 한다. 새 정의는 수치와 실행 키를 공급하지만 효과를 직접 실행하지 않는다.

이번 단계에서 유지하는 경계는 다음과 같다.

- `SaveData`, `SaveManager`의 JSON 필드, payload `version = 3`, PlayerPrefs 키를 변경하지 않는다.
- 새 저장 포맷, migrator, `LifeState`/`SoulState`, 오프라인 진행을 추가하지 않는다.
- 기존 `FirstFormSkillType`, `EnemyArchetype`, `ExplorationEventChoiceType` ordinal을 재정렬하지 않는다.
- 기존 무공·출신·아이템·적·사건의 수치, 배열 순서, RNG 후보 수와 결과 적용 순서를 바꾸지 않는다.
- 새 범용 effect resolver를 실행하지 않는다. 기존 `PlayerData`, `BattleManager`, `LootManager`, `ExplorationEventManager`가 효과를 정확히 한 번 적용한다.
- `rusty_sword`는 아직 실제 장착 검 instance가 아닌 legacy stack 효과다. `EquipmentInstanceIdentity` 계약을 추가했지만 기존 아이템을 instance로 변환하거나 검법 장착 조건을 충족시키지 않는다.
- SampleScene, UI, 프리팹, ScriptableObject 콘텐츠 에셋과 저장 fixture는 변경하지 않는다.

## 2. 구현 구조

| 책임 | 구현 | 현재 연결 |
| --- | --- | --- |
| stable ID 상수 | `Assets/Scripts/Content/ContentIds.cs` | 표시명이나 asset GUID를 참조하지 않는 영구 키 |
| 정의 snapshot | `Assets/Scripts/Content/ContentDefinitions.cs` | 출신, 주전투 계열, 병기 계열, 무공, 아이템, 적, 사건, 장비 ID 계약 |
| 현재 콘텐츠 원본 | `Assets/Scripts/Content/BuiltInGameContent.cs` | P0.1 수치와 순서를 코드 정의로 보존 |
| ID 인덱스와 alias 해석 | `Assets/Scripts/Content/GameContentCatalog.cs` | `StringComparer.Ordinal`, 이름 우선·ordinal fallback |
| 검증 | `Assets/Scripts/Content/GameContentCatalogValidator.cs` | 에디터와 CI에서 같은 순수 C# 검증 호출 |
| legacy 투영 | `Assets/Scripts/Content/LegacyContentAdapter.cs` | 새 정의를 기존 runtime DTO로 변환하며 효과는 실행하지 않음 |

P0.1 테스트는 런타임 타입을 `Assembly-CSharp`에서 reflection으로 찾는다. 별도 runtime asmdef 도입은 기준선을 깨고 legacy 타입 역참조를 만들 수 있으므로 P0.2 콘텐츠 코드는 `Assets/Scripts/Content`에 두되 기존 `Assembly-CSharp` 경계를 유지한다.

설계 초안의 장기 목표는 ScriptableObject authoring 정의를 검증 완료된 불변 snapshot으로 바꾸는 것이다. 이번 구현은 그 snapshot 계약과 validator를 먼저 고정한 **source-authored POCO 단계**다. 향후 ScriptableObject adapter는 같은 stable ID와 정의 필드를 생산해야 하며, 저장이나 도메인 규칙이 asset GUID를 읽게 해서는 안 된다.

## 3. stable ID 목록

### 3.1 출신

| stable ID | 현재 표시명 | 후보 여부 | 태그 |
| --- | --- | --- | --- |
| `origin.ordinary_body` | 평범한 육신 | 아니오 | `origin_tag.ordinary` |
| `origin.sword_sect_disciple` | 검문 제자 | 예 | `origin_tag.sword_sect` |
| `origin.demonic_cult_laborer` | 마교 잡역 | 예 | `origin_tag.demonic_cult` |
| `origin.herb_garden_apprentice` | 약밭 견습 | 예 | `origin_tag.herb_garden` |

`평범한 육신`은 `PlayerData.ResetForFirstRun()`의 중립 기본 상태를 식별하기 위한 호환 정의다. `ReincarnationManager.CreateCandidatePool()`에는 포함하지 않으며 생 번호별 `+2` 보정도 적용하지 않는다. 따라서 기존 환생 후보는 계속 세 개이고 모두 한 번씩 등장한다.

Git 이력에서 저장 도입 전에 존재했던 여섯 육신 이름은 실제 저장 wire에 존재했다는 근거가 없다. 이 이름들을 현재 세 출신으로 임의 연결하면 의미가 왜곡되므로 alias로 추가하지 않았다.

### 3.2 주전투 계열, 병기 계열과 장비 instance

데이터 계약은 다음 여덟 주전투 계열 ID를 고정한다.

```text
combat_discipline.sword
combat_discipline.blade
combat_discipline.spear_halberd
combat_discipline.staff_club
combat_discipline.fist_palm
combat_discipline.hidden_weapon
combat_discipline.iron_fan_exotic
combat_discipline.whip_chain
```

검만 `PrototypeImplemented`이며 사용자 선택 가능 정의다. 다른 일곱 정의는 `ContractOnly`, `isPlayerSelectable = false`이고 효과·밸런스·사용자 UI를 추가하지 않는다.

현재 실제 병기 계열 정의는 `weapon_family.sword` 하나다. 다른 주전투 계열의 물리 장비가 구현되기 전에 병기 계열 ID를 성급히 고정하거나 가짜 맨손 장비를 만들지 않는다. 세 ID 책임은 다음처럼 분리한다.

| ID | 소유자 | 의미 |
| --- | --- | --- |
| `combat_discipline.*` | `CombatDisciplineDefinition` | 한 생의 주전투 계열 콘텐츠 |
| `weapon_family.*` | `WeaponFamilyDefinition` | 실제 장착 병기 정의의 물리 계열 |
| `equipment.*` | `EquipmentDefinition` | 장비 콘텐츠 정의 |
| `instance.*` 등 runtime 고유값 | `EquipmentInstanceIdentity.instanceId` | 한 생에 존재하는 특정 소유 개체 |

`EquipmentInstanceIdentity`는 `instanceId`와 `equipmentDefinitionId`만 가진다. 병기 계열은 `equipmentDefinitionId → EquipmentDefinition.weaponFamilyId`로 해석하며 instance에 별도 권위 값을 중복 저장하지 않는다.

### 3.3 무공

| stable ID | 분류 | 주전투 계열 | 병기 조건 | legacy ordinal |
| --- | --- | --- | --- | ---: |
| `martial.sword.cheongpung` | `WeaponTechnique` | `combat_discipline.sword` | `weapon_family.sword` 필수 | 0 |
| `martial.sword.pamun` | `WeaponTechnique` | `combat_discipline.sword` | `weapon_family.sword` 필수 | 1 |
| `martial.footwork.hoeryu` | `Footwork` | 제한 없음 | `weaponAgnostic` | 2 |

`FirstFormSkillData.stableId`는 저장되지 않는 runtime identity다. 전투와 수련 판정은 이 ID를 우선 사용하고, 직접 생성된 legacy 객체처럼 ID가 없을 때만 이름·`FirstFormSkillType` alias로 복원한다. `SaveData`는 계속 기존 표시명과 ordinal을 쓴다.

### 3.4 아이템

기존 저장에 이미 쓰이는 다음 ID를 그대로 stable ID로 동결한다.

```text
rusty_sword
worn_training_robe
cracked_jade_token
small_healing_pill
faded_soul_stone
```

`LootItemCatalog`는 호환 facade로 남고 `GameContentCatalog`가 한 번 만든 동일 `ItemData[]` backing array를 반환한다. `SaveData.Sanitize()`의 exact ID 조회, unknown·즉시형 제거, 최대 중첩 clamp와 중복 entry 유지 규칙은 바뀌지 않는다.

### 3.5 적과 사건

| 종류 | stable ID 또는 동결 ID | legacy adapter |
| --- | --- | --- |
| 적 | `enemy.swift_scout`, `enemy.iron_guard`, `enemy.energy_sapper`, `enemy.berserker`, `enemy.stronghold_leader` | `EnemyArchetype` 0~4 |
| 사건 | `sword_mark_stele`, `poison_herb_field`, `injured_escort` | 기존 `eventId` 자체를 stable ID로 유지 |
| 사건 선택 | `event_choice.<event>.<choice>` 9개 | `ExplorationEventChoiceType` 0~8 |

적 정의는 전투 중 변하는 체력을 소유하지 않는다. `EnemyData.CreateForFloor()`는 stable 정의에서 매 전투 새 `EnemyData`를 만들어 기존 층·출행 깊이 계산을 적용한다. 사건 정의는 선택지와 legacy handler 키만 공급하고, 기존 `ExplorationEventManager.ApplyChoice()`가 결과를 한 번 실행한다.

## 4. legacy alias 규칙

alias key는 전역 문자열이나 전역 정수가 아니라 `(ContentKind, LegacyAliasKind, 값)`이다. 서로 다른 enum에서 같은 ordinal `0`을 사용해도 충돌하지 않는다. alias 표는 정의의 현재 `displayName`이나 enum의 현재 `ToString()`에서 자동 생성하지 않고, 과거 wire와 분기에 실제 쓰인 literal을 별도 역사 데이터로 동결한다. 따라서 표시명을 바꿔도 과거 이름 복원 경로가 사라지지 않는다.

| 범위 | legacy 값 | stable ID |
| --- | --- | --- |
| 무공 표시명 / enum 이름 / ordinal | 청풍검식 / `StableSword` / 0 | `martial.sword.cheongpung` |
| 무공 표시명 / enum 이름 / ordinal | 파문검식 / `RippleSword` / 1 | `martial.sword.pamun` |
| 무공 표시명 / enum 이름 / ordinal | 회류보 / `FlowStep` / 2 | `martial.footwork.hoeryu` |
| 출신 표시명 | 평범한 육신, 검문 제자, 마교 잡역, 약밭 견습 | 각 `origin.*` ID |
| 아이템 표시명 | 녹슨 검, 낡은 수련복, 깨진 옥패, 소형 회복단, 흐릿한 혼백석 | 기존 item ID |
| 적 표시명 / enum 이름 / ordinal | 현재 5개 적 stem / `EnemyArchetype` 이름 / 0~4 | 각 `enemy.*` ID |
| 사건 표시명 | 현재 3개 사건명 | 기존 event ID |
| 사건 선택 표시명 / enum 이름 / ordinal | 현재 9개 선택명 / choice enum 이름 / 0~8 | 각 `event_choice.*` ID |

무공 저장 복원은 기존처럼 **알려진 이름 우선, 이름을 해석하지 못할 때 ordinal fallback**이다. 예를 들어 `청풍검식 + ordinal 1`은 청풍검식으로, 미지 이름 + ordinal 1은 파문검식으로 복원된다. 문자열은 현재 동작처럼 `StringComparer.Ordinal`로 비교하며 자동 trim, slug 변환, 대소문자 무시를 하지 않는다.

## 5. 검증 규칙

`GameContentCatalogValidator`는 다음 오류를 수집하고 `ThrowIfInvalid()`에서는 초기화를 실패시킨다.

- 빈 stable ID, 허용하지 않는 ID 문자, 전 콘텐츠 범위의 중복 ID
- 1 미만 content revision과 빈 표시명
- alias 중복, 빈 alias, 존재하지 않거나 다른 콘텐츠 종류인 대상, 표시명/enum 이름 사이의 실제 resolver 충돌
- 현재 표시명과 무공·적·사건 선택 ordinal을 stable ID로 복원하지 못하는 alias 누락 또는 오연결
- 주전투 계열이 참조하는 존재하지 않는 병기 계열
- 미구현 주전투 계열의 사용자 선택 노출
- 무공이 참조하는 존재하지 않는 주전투 계열·병기 계열·선행 무공
- `weaponAgnostic = true`인데 `allowsNoMainWeapon` 또는 병기 계열 목록을 함께 둔 모순
- 병기가 필수인데 호환 병기 계열이 비어 있는 무공
- 무공에 적힌 각 병기 계열이 호환 주전투 계열 중 하나에 속하지 않거나, 각 주전투 계열이 요구 병기 계열 중 하나를 허용하지 않는 경우
- 병기 전용 무공인데 주전투 계열 또는 실제 병기 조건이 없는 경우
- 선행 무공 순환 참조
- 주병기 장비 정의에 병기 계열이 없거나 장비 정의가 존재하지 않는 병기 계열을 참조하는 경우
- 적·무공·사건 선택의 중복 또는 enum과 불일치하는 legacy ordinal
- 선택지가 없는 사건과 사건 선택의 끊어진 콘텐츠 참조

기본 카탈로그도 첫 접근 때 이 validator를 통과해야 사용할 수 있다. EditMode 테스트는 기본 카탈로그의 성공뿐 아니라 빈 ID, 중복 ID, 끊어진 참조, alias 대상 누락, 모순된 병기 조건이 실패하는 것을 검증한다.

## 6. 기존 manager 연결과 중복 효과 방지

| 기존 경로 | P0.2 변경 | 실행 책임 |
| --- | --- | --- |
| `FirstFormSkillManager.BuildCandidates()` | 무공 정의를 legacy DTO로 투영 | 기존 `PlayerData`·`BattleManager` 효과 분기 유지 |
| `ReincarnationManager.CreateCandidatePool()` | 후보 출신 정의 세 개를 투영 | 기존 생 번호별 `+2`와 `ApplyBodyOrigin()` 유지 |
| `LootItemCatalog` | 카탈로그 item 정의의 동일 backing array facade | 기존 `LootManager`·`PlayerData`가 한 번만 효과 적용 |
| `EnemyData.CreateForFloor()` | legacy cycle ordinal로 적 정의를 찾고 새 runtime 적 생성 | 기존 전투 상성·층/깊이 계산 유지 |
| `ExplorationEventManager.BuildEventCatalog()` | 사건·선택 정의를 legacy DTO로 투영 | 기존 choice enum switch가 한 번만 결과 적용 |
| `SaveManager.ApplySaveData()` | 기존 body name과 skill name/ordinal을 manager alias에서 해석 | JSON wire와 `Sanitize()` 유지 |

`PlayerData`는 저장되지 않는 `currentOriginId`와 태그를 가진다. `BattleManager`의 `Contains("마교")`, `Contains("약밭")` 규칙은 stable 출신 태그를 우선 사용한다. 알려지지 않은 legacy 육신 문자열은 기존 substring 결과까지 보존하기 위해 stable ID를 해석하지 못한 경우에만 동일 substring fallback을 사용한다.

## 7. 자동 테스트

`Assets/Tests/EditMode/GameContentCatalogTests.cs`가 다음을 추가로 고정한다.

- 기본 카탈로그의 ID 수, 검만 구현·선택 가능인 주전투 계열 계약, 세 출신 후보와 중립 기본 육신 분리
- 무공·출신·아이템·적·사건 alias와 이름 우선·ordinal fallback
- 세 무공, 세 후보 출신, 다섯 적, 세 사건과 아홉 선택지의 stable ID 투영
- 난수가 개입하지 않는 사건 선택의 기존 체력·내력·숙련·근력·다음 전투 보정 결과
- 표시명을 바꾼 청풍검식의 수련·공격 결과와 마교 출신의 철갑 산적 상성이 stable ID·태그 기준으로 동일함
- 빈/중복 ID, alias 대상·현재 표시명·ordinal 누락, alias 해석 충돌, 끊어진 참조, 주전투 계열–병기 계열 불일치와 주병기 계열 누락의 검증 실패
- `CombatDiscipline`, `WeaponFamily`, `EquipmentDefinition`, `EquipmentInstanceIdentity` ID 책임 분리

P0.1의 기존 EditMode 24건과 P0.2의 EditMode 11건, PlayMode 3건을 실행하며 기존 기대값은 수정하지 않는다. 테스트 명령은 [P0.1 문서의 실행 절차](P0_1CharacterizationBaseline.md#2-자동-테스트-구성과-실행)를 그대로 사용한다.

## 8. 후속 단계로 넘기는 결정

- ScriptableObject authoring asset과 `IGameContentSnapshot` 변환기는 stable snapshot 계약을 유지한 채 별도 PR에서 도입한다.
- 다른 일곱 주전투 계열의 병기 계열, 실제 효과, 밸런스와 사용자 선택 UI는 P0.2 범위가 아니다.
- `EquipmentInstance`의 저장·인벤토리·장착과 legacy stack projection은 P1 장비 단계에서 구현한다.
- 실제 사용자 저장 또는 배포 근거가 없는 과거 육신 이름은 alias 대상으로 추정하지 않는다.
- 새 저장에 stable ID를 쓰는 전환은 원본 백업·version 판별·migrator가 준비된 후 별도 단계에서 수행한다.
