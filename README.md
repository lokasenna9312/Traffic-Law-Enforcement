# Traffic Law Enforcement

Cities: Skylines II용 교통 법규 단속 모드입니다. 대중교통 전용 차로 무단 주행, 도로 중간 횡단성 차선 이동, 교차로 진입 방향 위반을 감지하고 벌금을 부과합니다. 벌금 수입은 예산 UI에 반영되며, 반복 위반 가중치, 월간 리포트, 디버그 UI, 저장 데이터 유지 기능도 포함합니다.

## 주요 기능

- 대중교통 전용 차로 위반 단속
- 도로 중간 차선 횡단성 이동 단속
- 교차로 진입 방향 위반 단속
- 위반 유형별 벌금 액수 개별 설정
- 반복 위반 차량 가중 처벌
- 예산 UI에 벌금 수입 항목 추가
- 월간 단속 리포트 및 Chirper 연동
- 디버그 UI에서 통계, 최근 이벤트, 차량별 누적 벌금 확인
- `en-US`, `ko-KR` 로컬라이제이션 제공
- 저장/불러오기 시 설정과 단속 데이터 유지

## 현재 단속 대상

### 1. Public transport lane violation

대중교통 전용 차로를 허가되지 않은 차량이 사용하는 경우를 단속합니다.

기본적으로 허용 가능한 차량군은 다음과 같습니다.

- 노면 대중교통 차량
- 택시
- 경찰차
- 소방차
- 구급차
- 쓰레기 수거 차량
- 우편 차량
- 도로 유지보수 차량
- 제설차
- 차량 정비 차량

추가 실험용 허용 대상도 설정에서 켜고 끌 수 있습니다.

- 개인 승용차
- 배달 트럭
- 화물 운송 차량
- 영구차
- 죄수 이송 차량
- 공원 유지보수 차량

### 2. Mid-block crossing

차량이 교차로가 아닌 구간에서 반대 방향 차선으로 넘어가거나, 측면 접근 권한이 없는 차선에서 주차/차고/건물 접근 연결로를 가로지르는 경우를 휴리스틱 기반으로 감지합니다.

### 3. Intersection movement

진입 차선이 허용한 진행 방향과 실제 연결 차선의 방향이 맞지 않는 경우를 감지합니다. 예를 들어 직진 전용 차선에서 좌회전 연결로로 진입하는 상황을 단속 대상으로 볼 수 있습니다.

## 설정

모드 설정은 크게 네 영역으로 나뉩니다.

- `Current Save`: 현재 도시 저장파일에만 적용되는 실시간 단속 설정
- `New Save Defaults`: 새 도시 시작 시 사용할 기본값
- `Policy Impact`: 단속이 경로 선택과 회피 행동에 준 영향 요약
- `Debug`: 디버그 및 점검용 정보

주요 설정 항목:

- 전체 단속 활성화/비활성화
- 대중교통 차로 허용 차량군 세부 토글
- 위반 유형별 벌금 액수
- 반복 위반 가중치 사용 여부
- 반복 위반 판정 기간, 기준 횟수, 배수
- 대중교통 차로 이탈 압력 임계값

기본 벌금은 각 위반 유형별로 `250`입니다.

## UI 및 텔레메트리

- 예산 화면에 Traffic Law Enforcement 벌금 수입 항목이 추가됩니다.
- 게임 내 Debug UI에서 다음 정보를 볼 수 있습니다.
  - 활성 대중교통 차로 위반 차량 수
  - 위반 유형별 누적 건수
  - 총 벌금 액수
  - 최근 이벤트
  - 최근 벌금 기록
  - 차량별 누적 벌금 및 위반 횟수
  - 반복 위반 정책 요약

또한 월간 단속 집계와 정책 영향 추적 상태가 저장됩니다.

## 저장 데이터

다음 데이터가 저장/복원됩니다.

- 현재 저장파일용 단속 설정
- 위반 통계
- 최근 단속 기록
- 차량별 누적 벌금/위반 횟수
- 반복 위반 판정용 타임스탬프
- 벌금 수입 이벤트
- 월간 리포트
- 정책 영향 추적 데이터

추가로 단속 이력 텍스트 파일은 아래 경로에 유지됩니다.

`%LocalAppData%\..\LocalLow\Colossal Order\Cities Skylines II\Traffic Law Enforcement\enforcement-history.txt`

## 개발 환경 요구사항

- Windows
- Cities: Skylines II 설치
- Cities: Skylines II Modding Toolchain 설치
- .NET Framework 4.8 빌드 환경
- MSBuild

프로젝트는 `Lib.Harmony`를 사용하며 타깃 프레임워크는 `net48`입니다.

기본 툴체인 경로 가정값:

`C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Content\Game\.ModdingToolchain`

## 빌드

### Visual Studio / MSBuild

프로젝트 파일:

`Traffic Law Enforcement/Traffic Law Enforcement.csproj`

직접 빌드할 때는 `CSII_TOOLPATH` 환경 변수가 Modding Toolchain 폴더를 가리켜야 합니다.

예:

```powershell
$env:CSII_TOOLPATH = 'C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Content\Game\.ModdingToolchain'
msbuild '.\Traffic Law Enforcement\Traffic Law Enforcement.csproj' /restore /t:Build /p:Configuration=Release /p:TargetFramework=net48
```

### 로컬 스모크 테스트 배포

루트의 `deploy-local-test.ps1`는 다음 작업을 수행합니다.

- MSBuild 탐색
- `CSII_TOOLPATH` 설정
- Release 빌드 실행
- 생성된 `dll`, `pdb`, `PublishConfiguration.xml`을 로컬 모드 폴더로 복사

배포 대상 경로:

`%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\Traffic Law Enforcement`

실행 예:

```powershell
.\deploy-local-test.ps1
```

## 프로젝트 구성

- `Mod.cs`: 모드 로드, 설정 등록, 시스템 업데이트 순서 구성
- `Setting.cs`: 옵션 UI와 저장 기본값 정의
- `PublicTransportLaneViolationSystem.cs`: 대중교통 차로 위반 감지
- `VehicleLaneHistorySystem.cs`: 차량 차선 이력 추적
- `LaneTransitionViolationSystem.cs`: 중간 횡단 및 교차로 방향 위반 감지
- `EnforcementPenaltyService.cs`: 벌금 계산 및 반복 위반 가중 처리
- `EnforcementFineMoneySystem.cs`: 실제 벌금 수입 반영
- `BudgetUIPatches.cs`: 예산 UI 통합
- `EnforcementSaveDataSystem.cs`: 저장/불러오기 처리
- `MonthlyEnforcementChirperSystem.cs`: 월간 리포트 발행
- `TrafficLawEnforcementDebugUI.cs`: 디버그 UI 구성

## 구현 메모

- ECS 기반 구조를 따르며 시스템 책임을 파일별로 분리했습니다.
- 일부 위반 판정은 현재 휴리스틱 기반입니다.
- 차선 접근/진입 판정은 게임 내부 lane/component 데이터 해석에 의존합니다.
- 시간 흐름을 변경하는 다른 모드가 있으면 반복 위반 판정의 체감 기간이 달라질 수 있습니다.

## 알려진 한계

- `Mid-block crossing`, `Intersection movement`는 현재 전이 기반 휴리스틱이므로 모든 케이스를 완벽하게 분류하지는 않습니다.
- 건물/서비스 접근 연결로 관련 판정은 일부 보완되어 있지만, 모든 경로 계획 단계에서 완전 차단을 보장하지는 않습니다.
- 게임 업데이트로 내부 메서드나 필드 구조가 바뀌면 Harmony 패치가 영향을 받을 수 있습니다.

## 향후 확장 아이디어

- 더 많은 위반 유형 추가
- 차단 교차로, 불법 유턴 등 세부 분류 강화
- 정책 영향 분석 고도화
- UI 리포트 개선

## 라이선스

별도 라이선스 파일이 없으므로 필요한 경우 저장소 정책에 맞게 추가하세요.
