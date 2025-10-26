# Changelog

All notable changes to TouchCursor for Windows will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2025-10-26

### Added
- **롤오버 예외 키 기능**: 특정 키를 롤오버 감지에서 제외
  - Key Mappings 탭에 "Ignore Rollover" 체크박스 컬럼 추가
  - 키마다 다른 롤오버 동작 설정 가능
  - 예: Space + I/J/K/L은 즉시 반응, Space + H는 롤오버 적용
- UI 번역 완성 (한국어/영어)
  - SettingsWindow 전체 번역
  - KeyMappingEditorDialog 번역
  - Activation Key Profiles 관련 번역
- README.md에 특정 디렉토리로 빌드하는 방법 추가
  - dotnet publish 명령어 예제
  - Self-contained 배포 옵션 설명

### Changed
- `TouchCursorOptions`: `RolloverExceptionKeys` 속성 추가
- `KeyMappingService`: 예외 키 체크 로직 구현
- `KeyMappingDisplay`: `INotifyPropertyChanged` 구현 및 `IgnoreRollover` 프로퍼티 추가
- .gitignore에 .idea/, *.user, *.suo 추가

### Fixed
- Modifier 키(Ctrl, Alt)를 활성화 키 목록에서 제거
  - 이유: Modifier 키는 ProcessKey를 거치지 않아 작동하지 않음

### Technical Details
- 관련 파일:
  - `Models/TouchCursorOptions.cs`
  - `Services/KeyMappingService.cs`
  - `SettingsWindow.xaml`
  - `SettingsWindow.xaml.cs`
  - `Resources/Strings.en.json`
  - `Resources/Strings.ko.json`

## [2.1.0] - 2025-10-24

### Added
- **Rollover Detection**: 빠른 타이핑 시 의도치 않은 커서 모드 활성화 방지
  - Space 키 후 짧은 시간(기본 50ms) 내에 키 입력 시 정상 입력으로 처리
  - 설정에서 threshold 조절 가능 (0-200ms)
  - 0ms로 설정하여 비활성화 가능
- Version 정보를 .csproj에 중앙화
- CHANGELOG.md 추가

### Changed
- `TouchCursorOptions`: `RolloverThresholdMs` 속성 추가 (기본값: 50ms)
- `KeyMappingService`: 타임스탬프 기반 rollover 감지 로직 구현
- `SettingsWindow`: Rollover threshold 조절 UI 추가

### Technical Details
- 관련 파일:
  - `Models/TouchCursorOptions.cs`
  - `Services/KeyMappingService.cs`
  - `SettingsWindow.xaml`
  - `SettingsWindow.xaml.cs`

## [2.0.0] - 2025-10-23

### Added
- C++ 원본 TouchCursor를 .NET 8 WPF로 완전 포팅
- 다중 Activation Key 프로필 지원
- 각 Activation Key별 독립적인 키 매핑
- Modifier 키 조합 지원 (Shift, Ctrl, Alt, Win)
- 설정 UI 개선 (탭 기반 인터페이스)
- 다국어 지원 (한국어, 영어)
- JSON 기반 설정 저장

### Features
- **기본 키 매핑** (Space + 키):
  - I/J/K/L → ↑/←/↓/→ (Vim 스타일 화살표)
  - U/O → Home/End
  - H → Left
  - P → Backspace
  - M → Delete
  - N/. → Ctrl+←/→ (단어 이동)
- **시스템 트레이 지원**
- **Windows 시작 프로그램 등록**
- **Training Mode**: 잘못된 키 입력 시 소리로 알림
- **프로그램별 활성화/비활성화**

### Changed
- Boost 라이브러리 의존성 제거 → 순수 .NET
- 레지스트리 → JSON 설정 파일
- Win32 MFC UI → 현대적인 WPF UI

### Technical Details
- .NET 8.0 + WPF
- Win32 API (SetWindowsHookEx, SendInput)
- System.Text.Json

---

## Version Numbering

버전 번호는 Semantic Versioning을 따릅니다: `MAJOR.MINOR.PATCH`

- **MAJOR**: 호환되지 않는 API 변경
- **MINOR**: 이전 버전과 호환되는 기능 추가
- **PATCH**: 이전 버전과 호환되는 버그 수정

### 버전 업데이트 가이드

1. **touch-cursor.csproj** 파일에서 버전 번호 업데이트:
   ```xml
   <Version>2.2.0</Version>
   <AssemblyVersion>2.2.0.0</AssemblyVersion>
   <FileVersion>2.2.0.0</FileVersion>
   ```

2. **CHANGELOG.md** 파일에 변경 사항 기록

3. Git 태그 생성:
   ```bash
   git tag -a v2.2.0 -m "Release v2.2.0: Rollover Exception Keys"
   git push origin v2.2.0
   ```
