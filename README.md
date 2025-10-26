# TouchCursor - .NET 8 WPF Edition

**Version 2.2.0** - C++ 원본 TouchCursor 프로젝트를 .NET 8 WPF로 포팅한 버전입니다.

## 개요

TouchCursor는 키보드의 **Space 키를 활용 키로 사용**하여 홈 포지션을 유지하면서 화살표 키, Home/End, Page Up/Down 등을 사용할 수 있게 해주는 유틸리티입니다.

마우스나 화살표 키로 손을 이동시키지 않고도 커서를 제어할 수 있어, 타이핑 효율성을 크게 향상시킵니다.

## 주요 기능

### 기본 키 매핑 (Space + 키)

| 키 조합 | 결과 | 설명 |
|---------|------|------|
| Space + I/J/K/L | ↑/←/↓/→ | 화살표 키 (Vim 스타일) |
| Space + U/O | Home/End | 줄 시작/끝 이동 |
| Space + H/P | Page Up/Down | 페이지 스크롤 |
| Space + M/, | Backspace/Delete | 삭제 |
| Space + N/. | Ctrl+←/→ | 단어 단위 이동 |

### 추가 기능

- **Rollover Detection** 🆕: 빠른 타이핑 시 의도치 않은 커서 모드 활성화 방지
  - Space 후 짧은 시간(기본 50ms) 내 키 입력 시 정상 타이핑으로 처리
  - 설정에서 민감도 조절 가능 (0-200ms)
- **시스템 트레이 지원**: 백그라운드에서 실행
- **Windows 시작 프로그램 등록**: 부팅 시 자동 실행
- **Training Mode**: 잘못된 키 입력 시 소리로 알림
- **설정 저장**: JSON 형식으로 사용자 설정 보존
- **다중 Activation Key 프로필**: 여러 activation key 설정 가능

## 시스템 요구사항

- Windows 10/11
- .NET 8.0 Runtime

## 설치 및 실행

### 개발 환경에서 실행

```bash
cd touch-cursor
dotnet restore
dotnet run
```

### 빌드

#### 기본 빌드

```bash
dotnet build -c Release
```

빌드된 실행 파일은 `touch-cursor\bin\Release\net8.0-windows\` 폴더에 생성됩니다.

#### 특정 디렉토리로 빌드 (Publish)

```bash
# Desktop에 빌드 (Windows)
dotnet publish -c Release -o %USERPROFILE%\Desktop\TouchCursor

# Desktop에 빌드 (PowerShell)
dotnet publish -c Release -o $env:USERPROFILE\Desktop\TouchCursor

# 사용자 지정 경로에 빌드
dotnet publish -c Release -o C:\MyApps\TouchCursor
```

**옵션 설명:**
- `-c Release`: Release 구성으로 빌드 (최적화됨)
- `-o <경로>`: 빌드 결과물을 지정된 경로에 생성
- `publish` 명령은 런타임 포함 배포 가능한 패키지를 생성합니다

**Self-contained 배포 (런타임 포함):**

```bash
# .NET 런타임이 없는 PC에서도 실행 가능
dotnet publish -c Release -r win-x64 --self-contained true -o %USERPROFILE%\Desktop\TouchCursor

# 단일 파일로 배포 (선택사항)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o %USERPROFILE%\Desktop\TouchCursor
```

## 사용 방법

1. **프로그램 실행**
   - 실행 시 시스템 트레이에 아이콘이 나타납니다
   - 메인 창을 닫아도 백그라운드에서 계속 실행됩니다

2. **기본 사용**
   - Space 키를 누른 상태에서 매핑된 키를 누르면 해당 기능이 실행됩니다
   - Space 키만 누르고 떼면 일반 Space로 동작합니다

3. **설정 변경**
   - 시스템 트레이 아이콘 더블클릭 → 메인 창 표시
   - Enable/Disable 체크박스로 기능 on/off
   - Training Mode: 매핑되지 않은 키 입력 시 경고음

4. **종료**
   - 시스템 트레이 아이콘 우클릭 → Exit

## 프로젝트 구조

```
touch-cursor/
├── Models/
│   ├── TouchCursorOptions.cs    # 설정 모델
│   └── ModifierFlags.cs          # 수식키 플래그 정의
├── Services/
│   ├── KeyboardHookService.cs    # 저수준 키보드 후킹
│   └── KeyMappingService.cs      # 키 매핑 로직
├── MainWindow.xaml               # 메인 UI
└── MainWindow.xaml.cs            # 메인 로직
```

## 기술 스택

- **.NET 8.0**: 최신 .NET 플랫폼
- **WPF (Windows Presentation Foundation)**: UI 프레임워크
- **Win32 API**: 저수준 키보드 후킹
  - `SetWindowsHookEx`: 전역 키보드 후크
  - `SendInput`: 키 이벤트 주입
- **System.Text.Json**: 설정 직렬화

## C++ 원본과의 차이점

### 개선 사항
- ✅ Boost 라이브러리 의존성 제거 → 순수 .NET
- ✅ 복잡한 빌드 프로세스 제거 → 단일 명령어 빌드
- ✅ 레지스트리 대신 JSON 설정 파일 사용
- ✅ 현대적인 WPF UI
- ✅ 크로스 플랫폼 가능성 (Linux/Mac은 추가 작업 필요)

### 유지된 핵심 기능
- ✅ 저수준 키보드 후킹
- ✅ 동일한 기본 키 매핑
- ✅ Training Mode
- ✅ 시스템 트레이 지원
- ✅ Windows 시작프로그램 등록

## 향후 개발 계획

- [ ] 고급 설정 창 (키 매핑 커스터마이징)
- [ ] 프로그램별 활성화/비활성화 목록
- [ ] 키 매핑 프로필 (여러 설정 전환)
- [ ] 자동 업데이트 기능
- [ ] 설치 프로그램 (MSI/Installer)

## 라이선스

이 프로젝트는 원본 TouchCursor 프로젝트의 포팅 버전입니다.

**원본 프로젝트**: TouchCursor by Martin Stone
**원본 라이선스**: GNU General Public License v3.0
**포팅 날짜**: 2025년 10월

이 소프트웨어는 GPL v3 라이선스 하에 배포됩니다. 자세한 내용은 [GNU GPL v3](https://www.gnu.org/licenses/gpl-3.0.html)를 참조하세요.

## 기여

버그 리포트, 기능 제안, Pull Request 환영합니다!

## 문제 해결

### 키보드 후킹이 작동하지 않는 경우

1. **관리자 권한으로 실행**: 일부 환경에서는 관리자 권한이 필요할 수 있습니다
2. **보안 소프트웨어 확인**: 안티바이러스가 키보드 후킹을 차단할 수 있습니다
3. **다른 키보드 도구와 충돌**: 다른 키 리매핑 도구가 실행 중인지 확인하세요

### 설정이 저장되지 않는 경우

설정 파일 위치: `%APPDATA%\TouchCursor\config.json`

해당 폴더에 쓰기 권한이 있는지 확인하세요.

## 감사의 말

원본 TouchCursor를 개발한 **Martin Stone**에게 감사드립니다. 이 프로젝트가 없었다면 이 포팅 버전도 존재하지 않았을 것입니다.

---

**Made with ❤️ using .NET 8 and WPF**
