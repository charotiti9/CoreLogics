# HojiTrain Scripts

---

## 📁 구조

```
Scripts/
├── Core/              # 핵심 시스템
│   ├── Addressable/   # 리소스 로딩 (참조 카운팅, 중복 방지)
│   ├── CSV/           # CSV 데이터 관리 (자동 파싱, 참조 해결)
│   ├── Game/          # 게임 부트스트랩 및 상태 관리
│   ├── GameFlow/      # 중앙집중식 Update 관리
│   ├── Pool/          # 오브젝트 풀링 (Addressable 통합)
│   ├── Singleton/     # 싱글톤 패턴
│   ├── StateMachine/  # 상태 머신
│   └── Utilities/     # 유틸리티
└── Common/            # 공통 기능
    └── UI/            # UI 관리 (레이어, 스택, Dim)
```

---

## ⚙️ 사전 설정

### 필수 패키지

**1. Addressable Asset System**
- 설치: Package Manager → Addressables
- 설정: Window → Addressables → Groups → Create Settings
- 용도: 모든 리소스 로딩 (Resources 폴더 사용 금지)

**2. DoTween**
- 설치: Asset Store → DOTween 임포트
- 설정: Tools → DOTween Utility Panel → Setup
- 용도: UI 애니메이션

**3. UniTask**
- 설치: Package Manager → Add from git URL
  - `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- 용도: 비동기 처리 (코루틴 대체, GC 0)

### 씬 설정

게임 시작 씬에 배치:
- `[GameBootstrap]` - GameBootstrap 컴포넌트
- `[GameFlowManager]` - GameFlowManager 컴포넌트
