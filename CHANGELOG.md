# Changelog

All notable changes to TouchCursor for Windows will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.7.0] - 2026-11-22

### Added
- **WezTerm 커서 색도 한/영 상태에 따라 바꾸기** (기존 "커서 색" 토글이 Zed, WezTerm 둘 다 맡는다)
  - WezTerm 이 앞에 있을 때 한/영이 바뀌면 `%APPDATA%\TouchCursor\wezterm-cursor.txt` 에 색을 쓴다
  - `~/.wezterm.lua` 가 이 파일을 읽어 `colors.cursor_bg` 로 쓰고, `add_to_config_reload_watch_list` 로 감시한다
  - 토글을 끄면 파일을 비워 원래 커서 색으로 돌아간다

### Changed
- 시작프로그램 버튼 3개를 앱 스타일에 맞게 바꿈(모서리 4, ModernComboBox 와 같은 색). 등록 버튼은 강조색
- 빠져 있던 번역 추가: 활성화 오버레이 표시, 활성화 키 홀드 지연, 저장

### Technical Details
- `ZedCursorColorService` → `ImeCursorColorService` (앱별로 마지막 상태를 따로 기억)
- 추가: `TouchCursor.Support/Themes/Units/ModernButton.xaml` (`ModernButtonStyle`, `ModernPrimaryButtonStyle`)

## [2.6.0] - 2026-11-22

### Added
- **Esc 누르면 영문으로 바꾸기** (일반 설정 토글, 기본 꺼짐)
  - Zed, WezTerm(`wezterm-gui`) 이 앞에 있을 때 Esc 를 누르면 한글 입력을 영문으로 바꾼다.
    Vim 노멀 모드로 나올 때 한글 때문에 명령이 안 먹는 일을 막는다
  - 키는 막지 않고 그대로 앱에 보낸다. 전환은 UI 스레드에서 해서 키보드 훅을 붙잡지 않는다
  - 대상 앱은 `config.json` 의 `EscToEnglishProcessNames` 로 바꿀 수 있다

### Technical Details
- `KeyboardHookService`: `EscapePressed` 이벤트, `SwitchImeToEnglish()` (WM_IME_CONTROL / IMC_SETCONVERSIONMODE)
- `ZedCursorColorService.ForegroundProcessName()`

## [2.5.0] - 2026-11-22

### Added
- **Zed 커서 색을 한/영 상태에 따라 바꾸기** (일반 설정 토글, 기본 꺼짐)
  - Zed 가 앞에 있을 때 0.2초마다 IME 상태를 읽어, 바뀌면 `%APPDATA%\Zed\settings.json` 의
    `"experimental.theme_overrides"` → `players[0].cursor` 를 고쳐 쓴다. Zed 가 설정을 다시 읽어 바로 반영된다
  - 영문 `#7ee787`(초록), 한글 `#ff9696`(빨강). `config.json` 의 `ZedCursorColorEnglish` / `ZedCursorColorKorean` 으로 바꿀 수 있다
  - 상태가 바뀔 때만 쓰고, 같은 색이면 쓰지 않는다
  - settings.json 이 심볼릭 링크(dotfiles 동기화)이거나 주석이 있으면 건드리지 않는다
  - 토글을 끄면 넣었던 커서 색(과 빈 껍데기)만 지운다

### Technical Details
- 추가: `TouchCursor.Support/Local/Services/ZedCursorColorService.cs`
- 변경: `TouchCursorOptions`, `GeneralSettingsViewModel`, `GeneralSettingsView.xaml`, `TouchCursorWindowViewModel`, `Strings.*.json`

## [2.4.2] - 2026-08-28

### Fixed
- **다른 앱에서 IME 상태가 항상 영문(A)으로만 표시되던 문제**
  - `ImmGetContext`는 호출한 프로세스 소유의 창에만 유효한 HIMC를 반환한다.
    다른 앱의 창에서는 0이 나와 곧바로 `false`가 되어, TouchCursor 자기 창을
    포커스했을 때만 한/영이 전환되고 실제 사용 상황에서는 항상 `A`로 표시됨
  - `ImmGetDefaultIMEWnd`로 대상 스레드의 IME 창을 얻어 `WM_IME_CONTROL`
    (`IMC_GETOPENSTATUS` / `IMC_GETCONVERSIONMODE`) 메시지로 조회하도록 변경.
    메시지 방식이라 프로세스 경계를 넘어서 동작한다
  - 응답 없는 앱 때문에 UI 스레드가 멈추지 않도록
    `SendMessageTimeout(SMTO_ABORTIFHUNG, 50ms)` 사용
  - 메시지 조회에 실패하면 기존 `ImmGetContext` 경로로 폴백

### Technical Details
- 변경: `TouchCursor.Support/Local/Helpers/KeyboardHookService.cs`
  - `IsKoreanImeActive()`, `TryGetImeStateByMessage()`, `TryQueryIme()`

## [2.4.1] - 2026-08-28

### Changed
- **활성화 오버레이 표시등에 IME 상태를 글자로 표시**
  - 한글 입력 상태면 `한`, 영문 입력 상태면 `A`
  - 표시등 크기 18px → 22px (글자가 들어갈 공간 확보, 오버레이 창 크기 40x40은 유지)
  - 기존 표시는 그대로 유지: 대기 초록 / 활성 파랑, CapsLock 시 사각형, 한글 IME 시 금색 테두리

### Technical Details
- 변경: `TouchCursor.Support/Themes/Views/ActivationOverlayWindow.xaml`
- IME 상태는 기존 `KeyboardHookService.IsKoreanImeActive()` / `ActivationOverlayWindow.IsKoreanImeOn`을 그대로 사용

## [2.4.0] - 2026-08-28

### Added
- **시작프로그램 등록 버튼**: 동작하지 않던 "Run at Windows startup" 토글을 버튼으로 교체
  - `바로가기 만들고 시작프로그램에 등록` - `IShellLink` COM으로 `.lnk` 바로가기를 직접 생성
  - `등록 해제` - 바로가기 삭제 (등록된 상태에서만 활성)
  - `시작프로그램 폴더 열기` - 탐색기로 열기, 등록된 경우 해당 `.lnk`가 선택된 상태로 열림
  - 등록 위치: `%AppData%\Microsoft\Windows\Start Menu\Programs\Startup\TouchCursor.lnk`
  - 현재 등록 상태와 경로를 화면에 표시하고, 일반 설정 탭을 열 때마다 실제 폴더를 다시 확인

### Fixed
- **언어 전환이 UI에 반영되지 않던 문제**
  - `LocalizeExtension`이 XAML 로드 시점의 상수 문자열을 반환해 이후 언어 변경이 화면에 전달되지 않음
    → `INotifyPropertyChanged` 기반 `LocalizedString` 바인딩을 반환하도록 변경
  - 앱 시작 시 저장된 언어를 로드하지 않아 항상 영어로 뜨던 문제 (`App.RegisterTypes`에서 로드)
  - XAML에 한국어로 하드코딩되어 있던 라벨 3곳을 리소스 키로 교체
    (`언어 / Language:`, `오버레이 위치:`, 활성화 키 선택 안내 문구)

### Technical Details
- 신규: `TouchCursor.Support/Local/Services/StartupShortcutService.cs`
- 변경:
  - `TouchCursor.Support/Local/Services/LocalizationService.cs` (`LocalizedString`, 약한 참조 구독자 관리)
  - `TouchCursor.Main/Local/Localization/LocalizeExtension.cs`, `TouchCursor.Support/Local/Localization/LocalizeExtension.cs`
  - `TouchCursor.Main/Local/ViewModels/GeneralSettingsViewModel.cs`
  - `TouchCursor.Main/Themes/Views/GeneralSettingsView.xaml`, `ActivationKeyDialog.xaml`
  - `TouchCursor.Forms/Local/ViewModels/TouchCursorWindowViewModel.cs`, `UI/Views/TouchCursorWindow.cs`
  - `touch-cursor/App.cs`, `Resources/Strings.{en,ko}.json`

> 참고: 2.3.0 ~ 2.3.4 릴리즈는 CHANGELOG에 기록되지 않았습니다.

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
