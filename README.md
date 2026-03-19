# Traffic Law Enforcement

## Description

This is a traffic law enforcement mod for Cities: Skylines II. It detects and penalizes unauthorized use of public transport lanes, mid-block median crossing (new), and intersection direction violations. Fine revenue is reflected in the budget UI. The mod includes repeat violation weighting, monthly reports, debug UI, and persistent save data.

### Key Features

- Enforcement of public transport lane violations
- Prevention of mid-block median crossing
- Enforcement of intersection direction violations
- Custom fine amounts per violation type
- Escalated penalties for repeat offenders
- Fine revenue integrated into the budget UI
- Monthly enforcement reports and Chirper integration
- Debug UI for statistics, recent events, and vehicle fine history
- Localization support for `en-US` and `ko-KR`
- Persistent settings and enforcement data on save/load

### Enforcement Targets

1. Public transport lane violation: Detects unauthorized vehicles using public transport lanes.
2. Mid-block median crossing prevention: Detects vehicles crossing the median outside intersections or accessing parking/building connections from unauthorized lanes.
3. Intersection direction violation: Detects mismatches between allowed lane directions and actual connection directions at intersections.

### Settings

- Current Save: Real-time enforcement settings applied only to the current city save file
- New Save Defaults: Default values used when starting a new city
- Policy Impact: Summary of enforcement effects on routing and avoidance behavior
- Debug: Information for debugging and inspection

Main settings include:

- Enable/disable all enforcement
- Detailed toggles for allowed vehicle types in public transport lanes
- Custom fine amounts per violation type
- Enable/disable repeat offender penalty escalation
- Repeat violation window, threshold, and multiplier
- Public transport lane exit pressure threshold

The default fine for each violation type is `250`.

## UI & Telemetry

- Fine revenue is added to the budget screen as a Traffic Law Enforcement item.
- The in-game Debug UI shows:
  - Number of active public transport lane violators
  - Cumulative counts per violation type
  - Total fine amount
  - Recent events
  - Recent fine records
  - Vehicle fine and violation history
  - Repeat offender policy summary

Monthly enforcement statistics and policy impact tracking are also saved.

## Save Data

The following data is saved and restored:

- Enforcement settings for the current save file
- Violation statistics
- Recent enforcement records
- Vehicle fine and violation history
- Timestamps for repeat violation checks
- Fine revenue events
- Monthly reports
- Policy impact tracking data

## License

Follows Paradox Mods license policy.

---

# 교통법규 단속

## 한국어 설명

Cities: Skylines II용 교통 법규 단속 모드입니다. 대중교통 전용 차로 무단 주행, 도로 중간 중앙선 횡단(신규), 교차로 진입 방향 위반을 감지하고 벌금을 부과합니다. 벌금 수입은 예산 UI에 반영되며, 반복 위반 가중치, 월간 리포트, 디버그 UI, 저장 데이터 유지 기능도 포함합니다.

### 주요 기능

- 대중교통 전용 차로 위반 단속
- 도로 중간 중앙선 횡단 방지
- 교차로 진입 방향 위반 단속
- 위반 유형별 벌금 액수 개별 설정
- 반복 위반 차량 가중 처벌
- 예산 UI에 벌금 수입 항목 추가
- 월간 단속 리포트 및 Chirper 연동
- 디버그 UI에서 통계, 최근 이벤트, 차량별 누적 벌금 확인
- `en-US`, `ko-KR` 로컬라이제이션 제공
- 저장/불러오기 시 설정과 단속 데이터 유지

### 단속 대상

1. 대중교통 전용 차로 위반: 허가되지 않은 차량이 대중교통 전용 차로를 사용하는 경우 단속
2. 도로 중간 중앙선 횡단 방지: 교차로가 아닌 구간에서 중앙선을 넘어 반대 방향 차선으로 진입하거나, 허가되지 않은 차선에서 주차/건물 연결로를 사용하는 경우 감지
3. 교차로 진입 방향 위반: 진입 차선의 허용 방향과 실제 연결 방향이 일치하지 않는 경우 감지

### 설정

- Current Save: 현재 도시 저장파일에만 적용되는 실시간 단속 설정
- New Save Defaults: 새 도시 시작 시 사용할 기본값
- Policy Impact: 단속이 경로 선택과 회피 행동에 준 영향 요약
- Debug: 디버그 및 점검용 정보

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

## 라이선스

Paradox Mods 저작권 정책을 따릅니다.