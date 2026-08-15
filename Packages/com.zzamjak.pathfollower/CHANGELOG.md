# Changelog

이 프로젝트의 주요 변경 사항을 기록합니다.
형식은 [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/)를 따르며, [Semantic Versioning](https://semver.org/lang/ko/)을 준수합니다.

## [1.0.2] - 2026-08-15

### Fixed

- 에디트 모드에서 `PathFollower.OnValidate()`가 오브젝트 위치를 현재 진행도 기준으로 다시 쓰던 문제를 수정했습니다.
- `PathRibbon`의 에디트 모드 UV 스크롤이 선택/마우스 이동 중 끊겨 보이던 문제를 방지했습니다. 스크롤은 Play Mode 또는 명시적인 10초 에디터 테스트 중에만 동작합니다.
- 10초 에디터 테스트 종료 직후 남은 마우스 입력으로 테스트가 즉시 재시작될 수 있던 문제를 방지했습니다.

### Changed

- PathRibbon 스크롤 설정을 속도 직접 입력 대신 PathFollower 이동 시간과 동기화되는 `enableScroll`/`invertScroll` 방식으로 정리했습니다.
- SceneView 경로 편집 가이드 오버레이, Alt 삽입 미리보기, P키 포인트 추가, 정점 균일화 도구를 추가했습니다.

## [1.0.1] - 2026-08-15

### Changed

- 리포지토리를 개발용 Unity 프로젝트 + 임베디드 패키지 구조로 전환 (설치 URL 경로 변경: ?path=/Packages/com.zzamjak.pathfollower)

## [1.0.0] - 2026-08-04

### Added

- **독립 UPM 패키지 최초 릴리스** (`com.zzamjak.pathfollower`)
  - 기존 프로젝트 내장 버전(`Assets/Plugins/CAT/PathFollower`, 내부 v1.3.2)을 별도 패키지로 분리
  - 스크립트 GUID 보존 — 기존 프로젝트에서 폴더 삭제 후 패키지 설치 시 씬/프리팹 참조 유지
- **PathFollower** — 베지어 곡선 경로 추종 컴포넌트
  - ScriptableObject 없이 컴포넌트 자체에 경로 데이터 저장, 런타임 경로 수정 API 제공
  - 이동 제어: `Play` / `Pause` / `Stop` / `SetProgress`, LoopType (None / Restart / Yoyo)
  - 이벤트: `OnComplete`, `OnLoop`
  - 경로 프리셋: `SetCircle`(원형), `SetPolygon`(다각형 + 모서리 둥글기), `SetStar`(별모양)
  - 경로 도구: `ExpandPath`, `RelaxPath`, `RotatePath`, `ScalePath` (전체/선택 정점)
  - 포인트 API: `AddPoint`, `InsertPoint`, `RemovePoint`, `SetPointPosition`, `GetPointAt`, `GetDirectionAt` 등
  - **에이전트 시스템**: 다수 오브젝트를 독립 타이밍으로 동일 경로 이동 (`AddAgent` / `RemoveAgent`)
  - **스냅샷 & 모핑**: 경로 스냅샷 저장/전환, 동일 정점 수 스냅샷 간 모핑 보간
  - UI 모드: Canvas 자식일 때 자동 활성화 (Z=0 고정, XY 평면 핸들)
- **PathRibbon** — Tiling 스프라이트 리본 메시 컴포넌트
  - UI 모드(`MaskableGraphic`) / Sprite 모드(`MeshRenderer`) 자동 감지
  - Loop 경로 이음매 자동 보정 + UV 스크롤(컨베이어 벨트) 연출
  - `flipX` / `flipY` UV 반전, 자식 SpriteRenderer flip과 XOR 결합
  - URP 기본 스프라이트 셰이더 폴백 자동 대체 (`CAT/PathFollower/Ribbon-Unlit`)
  - 모바일 최적화: 사전 할당 배열, GC 없는 변경 감지, 자동 서브 Canvas(상위 Canvas rebuild 격리)
- **커스텀 에디터**
  - SceneView 정점/핸들 편집, 박스 선택, 곡선 위 정점 삽입, 핸들 회전 모드, 에디터 테스트 재생
  - Path Tools (원형/다각형/별 생성, 확대/축소, Relax), 선택 영역 변형(회전/스케일), 스냅샷 관리 UI
- 모든 길이/두께/법선 계산을 경로 공간(부모 로컬 공간) 기준으로 통일 — 카메라/캔버스 스케일 독립적 타일링
