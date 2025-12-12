# 환경
- Unity 6000.2.9f1

# 소개
이 프로젝트는 [블로그](https://charotiti9.github.io/)에 연재된 Unity 공용 스크립트 제작기의 실제 구현 코드입니다.

## 관련 블로그 포스트
- https://charotiti9.github.io/devlog/Core-Script-01.html
- https://charotiti9.github.io/devlog/Core-Script-02.html
- https://charotiti9.github.io/devlog/Core-Script-03.html
- https://charotiti9.github.io/devlog/Core-Script-04.html
- https://charotiti9.github.io/devlog/Core-Script-05.html
- https://charotiti9.github.io/devlog/Core-Script-06.html
- https://charotiti9.github.io/devlog/Core-Script-07.html
- https://charotiti9.github.io/devlog/Core-Script-08.html
- https://charotiti9.github.io/devlog/Core-Script-09.html

---

##  🎯 Core Systems
  - Addressable System - Centralized resource loading with automatic reference counting and memory management
  - CSV Data System - Automatic CSV parsing, C# class generation, and circular reference validation
  - Game Bootstrap - Structured game initialization and state management
  - GameFlow Manager - Centralized Update/FixedUpdate/LateUpdate management for predictable execution order
  - Input Manager - Event-based and polling input system with automatic code generation from Input Actions
  - Object Pool - High-performance object pooling with Addressable integration
  - State Machine - Generic state machine implementation for AI and game states
  - Singleton Pattern - Thread-safe singleton implementations (MonoBehaviour and POCO)

##  🎮 Common Features
  - Audio Manager - Multi-channel audio system (BGM, SFX, Voice) with fade effects and priority queue
  - UI Manager - Complete UI lifecycle management with layers, stacks, and automatic dim control

## ⛓️Required
  - Addressable Asset System: Required
  - UniTask: Required (https://github.com/Cysharp/UniTask)
  - DOTween: Required (Asset Store)

 ## 🚀 Quick Start
  1. Import the UnityPackage
  2. Install required packages:  
    - Addressable Asset System (Package Manager)  
    - UniTask ([git URL](https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask))  
    - DOTween (Asset Store)  
       - Add 'Scripting Define Symbol' Manually. Edit > Project Settings > Player > Other Player > Scripting Define Symbols > Add `UNITASK_DOTWEEN_SUPPORT` > Apply
  4. Add to your bootstrap scene:  
    - GameBootstrap component  
    - GameFlowManager component  
    - UIManager component  
    - AudioManager component  
  5. See Assets/Scripts/README.md for detailed documentation
  
---

##  🎯 핵심 시스템
  - Addressable System - 자동 참조 카운팅과 메모리 관리를 제공하는 중앙집중식 리소스 로딩 시스템
  - CSV 데이터 시스템 - CSV 자동 파싱, C# 클래스 생성, 순환 참조 검증
  - Game Bootstrap - 체계적인 게임 초기화 및 상태 관리
  - GameFlow Manager - 예측 가능한 실행 순서를 위한 중앙집중식 Update/FixedUpdate/LateUpdate 관리
  - Input Manager - Input Actions로부터 자동 코드 생성을 지원하는 이벤트 기반 및 폴링 입력 시스템
  - Object Pool - Addressable과 통합된 고성능 오브젝트 풀링
  - State Machine - AI 및 게임 상태를 위한 범용 상태 머신 구현
  - Singleton Pattern - 스레드 안전 싱글톤 구현 (MonoBehaviour 및 일반 클래스)

##  🎮 일반 시스템
  - Audio Manager - 페이드 효과 및 우선순위 큐를 갖춘 멀티 채널 오디오 시스템 (BGM, SFX, Voice)
  - UI Manager - 레이어, 스택, 자동 Dim 제어를 갖춘 완전한 UI 생명주기 관리

## ⛓️필요한 에셋
  - Addressable Asset System: 필수
  - UniTask: 필수 (https://github.com/Cysharp/UniTask)
  - DOTween: 필수 (에셋 스토어)  
    - Scripting Define Symbol을 수동으로 추가 필요. Edit > Project Settings > Player > Other Player > Scripting Define Symbols 항목에서 `UNITASK_DOTWEEN_SUPPORT` 를 추가 > Apply

 ## 🚀 빠른 시작
  1. UnityPackage 임포트
  2. 필수 패키지 설치:  
    - Addressable Asset System (Package Manager)  
    - UniTask ([git URL](https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask))  
    - DOTween (에셋 스토어)  
  4. 부트스트랩 씬에 추가:  
    - GameBootstrap 컴포넌트  
    - GameFlowManager 컴포넌트  
    - UIManager 컴포넌트  
    - AudioManager 컴포넌트  
  5. 자세한 문서는 Assets/Scripts/README.md 참조

--- 

# 기여하기
틀린 점이나 개선할 점을 발견하셨다면 언제든지 알려주세요!
- [Issue 생성](https://github.com/charotiti9/CoreLogics/issues)
- [Pull Request 제출](https://github.com/charotiti9/CoreLogics/pulls)
