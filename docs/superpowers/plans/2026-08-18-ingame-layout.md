# InGame Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 UI 에셋과 동작을 유지하면서 `InGame.unity`의 전체 HUD를 참조 이미지와 같은 상단 HUD, 오른쪽 1~5 슬롯, 하단 스킬 구성으로 재배치한다.

**Architecture:** 새 코드나 UI 오브젝트를 만들지 않고 씬에 직렬화된 기존 RectTransform과 `BattleHUDView` 슬롯 좌표만 수정한다. 모든 버튼 이벤트와 MonoBehaviour 참조는 유지하며 Unity Editor에서 시각 상태와 입력 동작을 검증한다.

**Tech Stack:** Unity 6000.3.19f1, uGUI, TextMeshPro, Unity MCP

## Global Constraints

- CanvasScaler 기준 해상도 `1080 x 1920`을 유지한다.
- 기존 이미지, 색상, 게임 동작과 데이터 연결은 변경하지 않는다.
- 새 스크립트, 새 에셋, 새 UI 컴포넌트, 별도 레이아웃 시스템은 추가하지 않는다.
- 수정 대상은 `Assets/_Game/Scenes/InGame.unity` 하나로 제한한다.

---

### Task 1: 전체 HUD 재배치 및 PlayMode 검증

**Files:**
- Modify: `Assets/_Game/Scenes/InGame.unity`
- Reference: `Assets/_Game/Scripts/InGame/UI/BattleHUDView.cs`
- Reference: `Assets/_Game/Scripts/InGame/Player/PlayerSwapManager.cs`

**Interfaces:**
- Consumes: `BattleHUDView.m_skillSlots`, `m_fieldSlotPositions`, `m_reserveSlotPositions`와 기존 버튼 이벤트 참조
- Produces: 상단 정보 띠, 오른쪽 1~5 캐릭터 슬롯, 하단 함선 스킬 배치를 가진 동일한 InGame HUD

- [ ] **Step 1: 정확한 Unity 인스턴스와 씬 상태 확인**

Unity MCP의 `mcpforunity://instances`에서 프로젝트 루트가 `/Users/woodenshield/Desktop/UNITY/Projects/space_captain/InGame`인 인스턴스를 선택한다. `mcpforunity://project/info`와 `mcpforunity://editor/state`에서 Unity `6000.3.19f1`, `ready_for_tools=true`, `is_compiling=false`를 확인하고 `Assets/_Game/Scenes/InGame.unity`를 연다.

- [ ] **Step 2: 현재 화면 기준 캡처와 연결 상태 확인**

`1080 x 1920` Game View 또는 Game 카메라 스크린샷을 저장한다. `Canvas`, `Text_Group`, `ActiveSkill_Group`, `MasterShipSkill_Group`, `BattleHUDView`를 조회하고 기존 `m_skillSlots` 5개와 버튼 이벤트가 모두 연결됐는지 확인한다.

- [ ] **Step 3: 캐릭터 슬롯을 오른쪽 1~5 세로열로 배치**

`BattleHUDView`의 위치 배열을 다음 값으로 수정한다.

```yaml
m_fieldSlotPositions:
- {x: 414, y: 287}
- {x: 414, y: 127}
- {x: 414, y: -33}
m_reserveSlotPositions:
- {x: 414, y: -193}
- {x: 414, y: -353}
```

`ActiveSkill_Group`의 기존 슬롯 순서와 `SkillSlotUI` 참조는 변경하지 않는다.

- [ ] **Step 4: 상단 HUD를 한 줄 정보 띠로 정돈**

상단 HUD의 두 줄 기준선을 유지하고 다음 RectTransform 값을 적용한다.

```yaml
LEVEL_text: {position: {x: 131.5675, y: 866}, size: {x: 198.1612, y: 53.6778}}
EXP_Slider: {position: {x: 89.9848, y: 866}, size: {x: 710.7604, y: 67.8553}}
Play_speed_Text: {position: {x: 279.2, y: 783}, size: {x: 88.1929, y: 50}}
Time_text: {position: {x: 427.10358, y: 783}, size: {x: 200, y: 50}}
Wave_Text: {position: {x: 135, y: 783}, size: {x: -784, y: 50}}
Kill_Count_Text: {position: {x: 375, y: 783}, size: {x: -784, y: 50}}
```

글꼴, 문자열, 색상, 컴포넌트는 변경하지 않는다. 이 값은 현재 상단 정보 배치를 보존하는 명시적 기준이며, 참조 이미지와 이미 일치하는 영역에는 불필요한 diff를 만들지 않는다.

- [ ] **Step 5: 하단 함선 스킬을 왼쪽 가로열로 이동**

`MasterShipSkill_Group`의 가로 정렬과 자식 순서를 유지한 채 하단 왼쪽으로 이동한다. 앵커는 bottom-center를 유지하고 `m_AnchoredPosition`을 `{x: -120, y: 120}`으로 설정한다. 기존 `ShipSkillButton` 4개의 이벤트와 크기는 변경하지 않는다.

- [ ] **Step 6: 씬 저장 후 정적 diff 확인**

Run:

```bash
git diff --check -- Assets/_Game/Scenes/InGame.unity
git diff --unified=0 -- Assets/_Game/Scenes/InGame.unity
```

Expected: 공백 오류가 없고 RectTransform 및 `m_fieldSlotPositions`/`m_reserveSlotPositions` 외 MonoBehaviour 참조, Sprite, 이벤트 변경이 없다.

- [ ] **Step 7: Unity 컴파일과 PlayMode 동작 확인**

컴파일 종료 후 Console Error가 0인지 확인한다. PlayMode에서 슬롯 1~5가 오른쪽에 순서대로 노출되고 1~3 전투와 4~5 예비 선택·교대가 동작하는지 확인한다. 함선 스킬 4개를 눌러 기존 입력이 유지되는지 확인한다.

- [ ] **Step 8: 최종 화면과 작업 범위 확인**

`1080 x 1920` 최종 스크린샷을 저장해 상단 HUD, 오른쪽 슬롯, 하단 스킬이 겹치거나 화면 밖으로 나가지 않는지 확인한다. PlayMode 종료 후 `git status --short`와 씬 diff를 다시 확인해 런타임 직렬화 오염이 있으면 제거한다.

- [ ] **Step 9: 씬 변경 커밋**

```bash
git add -- Assets/_Game/Scenes/InGame.unity
git commit -m "인게임 HUD 전체 레이아웃 재배치"
```
