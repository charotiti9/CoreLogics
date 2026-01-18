#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Cheat
{
    /// <summary>
    /// 치트 입력 UI (Editor 전용)
    /// [`] 키로 열고 닫습니다.
    /// </summary>
    public class EditorCheatConsole : EagerMonoSingleton<EditorCheatConsole>
    {
        // UI 표시 여부
        private bool isVisible = false;

        // 입력 텍스트
        private string inputText = "";

        // 자동완성 목록
        private List<CheatData> suggestions = new List<CheatData>();

        // 선택된 자동완성 인덱스
        private int selectedSuggestionIndex = 0;

        // 정확히 일치하는 치트 (파라미터 표시용)
        private CheatData selectedCheat = null;

        // 현재 입력 중인 파라미터 인덱스
        private int currentParamIndex = 0;

        // 스크롤 위치
        private Vector2 scrollPosition;

        // 히스토리
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;

        // UI 스타일
        private GUIStyle windowStyle;
        private GUIStyle inputStyle;
        private GUIStyle suggestionStyle;
        private GUIStyle selectedSuggestionStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle logStyle;
        private bool stylesInitialized = false;

        // 로그 메시지
        private List<string> logMessages = new List<string>();
        private const int MAX_LOG_MESSAGES = 10;

        // UI 크기
        private const float WINDOW_WIDTH = 600f;
        private const float WINDOW_HEIGHT = 400f;
        private const float SUGGESTION_HEIGHT = 30f;
        private const int MAX_VISIBLE_SUGGESTIONS = 6;

        // 포커스 제어용
        private bool shouldFocusInput = false;
        private const string INPUT_CONTROL_NAME = "CheatInput";

        protected override void Initialize()
        {
            base.Initialize();
            InputManager.Instance.EnableCheatInput();
        }

        private void Update()
        {
            // [`] 키 입력 감지 (BackQuote) - 닫혀있을 때만 열기
            if (!isVisible && InputManager.Instance.IsOpenCheatPressed())
            {
                Show();
            }

            // ESC로 닫기
            if (isVisible && InputManager.Instance.IsCloseCheatPressed())
            {
                Hide();
            }
        }

        /// <summary>
        /// UI 표시/숨김 토글
        /// </summary>
        private void ToggleVisibility()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// UI 표시
        /// </summary>
        private void Show()
        {
            isVisible = true;
            inputText = "";
            selectedSuggestionIndex = 0;
            selectedCheat = null;
            currentParamIndex = 0;
            historyIndex = -1;
            shouldFocusInput = true;

            // 치트 데이터가 로드되지 않았으면 다시 로드 시도
            if (!CheatManager.Instance.HasCheatData())
            {
                CheatManager.Instance.ReloadCheatData();
            }

            UpdateSuggestions();
        }

        /// <summary>
        /// UI 숨김
        /// </summary>
        private void Hide()
        {
            isVisible = false;
            inputText = "";
        }

        /// <summary>
        /// 스타일 초기화
        /// </summary>
        private void InitializeStyles()
        {
            if (stylesInitialized)
            {
                return;
            }

            // 윈도우 스타일
            windowStyle = new GUIStyle(GUI.skin.box);
            windowStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            windowStyle.padding = new RectOffset(10, 10, 10, 10);

            // 입력 필드 스타일
            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = 24;
            inputStyle.normal.textColor = Color.white;
            inputStyle.focused.textColor = Color.white;
            inputStyle.padding = new RectOffset(10, 10, 8, 8);

            // 자동완성 항목 스타일
            suggestionStyle = new GUIStyle(GUI.skin.label);
            suggestionStyle.fontSize = 24;
            suggestionStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            suggestionStyle.padding = new RectOffset(10, 10, 4, 4);

            // 선택된 자동완성 항목 스타일
            selectedSuggestionStyle = new GUIStyle(suggestionStyle);
            selectedSuggestionStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.8f));
            selectedSuggestionStyle.normal.textColor = Color.white;

            // 설명 스타일
            descriptionStyle = new GUIStyle(GUI.skin.label);
            descriptionStyle.fontSize = 20;
            descriptionStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            descriptionStyle.padding = new RectOffset(20, 10, 0, 4);

            // 로그 스타일
            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.fontSize = 24;
            logStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);
            logStyle.padding = new RectOffset(5, 5, 2, 2);

            stylesInitialized = true;
        }

        /// <summary>
        /// 단색 텍스처 생성
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            InitializeStyles();

            // 이벤트 처리
            Event e = Event.current;
            HandleKeyboardInput(e);

            // 윈도우 위치 (화면 상단 중앙)
            float windowX = (Screen.width - WINDOW_WIDTH) / 2f;
            float windowY = 50f;

            // 윈도우 그리기
            GUILayout.BeginArea(new Rect(windowX, windowY, WINDOW_WIDTH, WINDOW_HEIGHT), windowStyle);
            {
                // 타이틀
                GUILayout.Label("Cheat Console", new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                });

                GUILayout.Space(5);

                // 입력 필드
                GUI.SetNextControlName(INPUT_CONTROL_NAME);
                string newInput = GUILayout.TextField(inputText, inputStyle, GUILayout.Height(40));

                // 입력이 변경되면 자동완성 업데이트
                if (newInput != inputText)
                {
                    inputText = newInput;
                    selectedSuggestionIndex = 0;
                    UpdateSuggestions();
                }

                // 포커스 설정
                if (shouldFocusInput)
                {
                    GUI.FocusControl(INPUT_CONTROL_NAME);
                    shouldFocusInput = false;
                }

                GUILayout.Space(5);

                // 자동완성 목록 또는 파라미터 가이드
                if (selectedCheat != null && suggestions.Count == 0)
                {
                    // 파라미터 입력 모드
                    DrawParameterGuide();
                }
                else
                {
                    // 자동완성 목록
                    DrawSuggestions();
                }

                // 로그 메시지
                DrawLogMessages();
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// 키보드 입력 처리
        /// </summary>
        private void HandleKeyboardInput(Event e)
        {
            if (e.type != EventType.KeyDown)
            {
                return;
            }

            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    ExecuteCommand();
                    e.Use();
                    break;

                case KeyCode.Tab:
                    // Tab: 가장 유사한 항목으로 자동완성
                    AutoCompleteWithTab();
                    e.Use();
                    break;

                case KeyCode.UpArrow:
                    if (e.control)
                    {
                        // Ctrl+↑: 히스토리 탐색
                        NavigateHistory(-1);
                    }
                    else if (suggestions.Count > 0)
                    {
                        // 자동완성 목록에서 위로 이동
                        selectedSuggestionIndex--;
                        if (selectedSuggestionIndex < 0)
                        {
                            selectedSuggestionIndex = suggestions.Count - 1;
                        }
                    }
                    e.Use();
                    break;

                case KeyCode.DownArrow:
                    if (e.control)
                    {
                        // Ctrl+↓: 히스토리 탐색
                        NavigateHistory(1);
                    }
                    else if (suggestions.Count > 0)
                    {
                        // 자동완성 목록에서 아래로 이동
                        selectedSuggestionIndex++;
                        if (selectedSuggestionIndex >= suggestions.Count)
                        {
                            selectedSuggestionIndex = 0;
                        }
                    }
                    e.Use();
                    break;
            }
        }

        /// <summary>
        /// 자동완성 목록 업데이트
        /// </summary>
        private void UpdateSuggestions()
        {
            string searchText = inputText;
            int spaceIndex = inputText.IndexOf(' ');

            if (spaceIndex > 0)
            {
                // 공백이 있으면 ID 부분만 추출
                string cheatId = inputText.Substring(0, spaceIndex);
                var exactMatch = CheatManager.Instance.GetExactMatch(cheatId);

                if (exactMatch != null)
                {
                    // 정확히 일치하는 치트가 있으면 파라미터 입력 모드
                    selectedCheat = exactMatch;
                    suggestions.Clear();

                    // 현재 입력된 파라미터 개수 계산
                    string paramPart = inputText.Substring(spaceIndex + 1);
                    currentParamIndex = CountInputParameters(paramPart);
                    return;
                }
                else
                {
                    // 일치하는 치트가 없으면 자동완성 비활성화
                    selectedCheat = null;
                    suggestions.Clear();
                    return;
                }
            }

            // 공백이 없는 경우: ID 입력 중
            selectedCheat = null;
            currentParamIndex = 0;

            // 정확히 일치하는 치트가 있는지 확인
            var exact = CheatManager.Instance.GetExactMatch(searchText);
            if (exact != null)
            {
                // 정확히 일치하면 해당 치트만 표시
                selectedCheat = exact;
                suggestions.Clear();
                suggestions.Add(exact);
            }
            else
            {
                // 유사한 치트 목록 표시
                suggestions = CheatManager.Instance.GetMatchingCheats(searchText);
            }

            // 인덱스 범위 보정
            if (selectedSuggestionIndex >= suggestions.Count)
            {
                selectedSuggestionIndex = suggestions.Count > 0 ? 0 : -1;
            }
        }

        /// <summary>
        /// 입력된 파라미터 개수를 계산합니다.
        /// </summary>
        /// <param name="paramPart">파라미터 부분 문자열</param>
        /// <returns>입력된 파라미터 개수</returns>
        private int CountInputParameters(string paramPart)
        {
            if (string.IsNullOrEmpty(paramPart))
            {
                return 0;
            }

            int count = 0;
            bool inQuotes = false;
            bool hasContent = false;

            for (int i = 0; i < paramPart.Length; i++)
            {
                char c = paramPart[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasContent = true;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (hasContent)
                    {
                        count++;
                        hasContent = false;
                    }
                }
                else
                {
                    hasContent = true;
                }
            }

            return count;
        }

        /// <summary>
        /// 자동완성 목록 그리기
        /// </summary>
        private void DrawSuggestions()
        {
            if (suggestions.Count == 0)
            {
                return;
            }

            float listHeight = Mathf.Min(suggestions.Count, MAX_VISIBLE_SUGGESTIONS) * (SUGGESTION_HEIGHT + 20);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(listHeight));
            {
                for (int i = 0; i < suggestions.Count; i++)
                {
                    var cheat = suggestions[i];
                    bool isSelected = (i == selectedSuggestionIndex);
                    GUIStyle style = isSelected ? selectedSuggestionStyle : suggestionStyle;

                    // 사용법 표시
                    string usage = cheat.GetUsage();

                    // 구현 여부 표시
                    bool hasImplementation = CheatManager.Instance.HasCheatType(cheat.ID);
                    string statusIcon = hasImplementation ? "" : " [미구현]";

                    if (GUILayout.Button(usage + statusIcon, style, GUILayout.Height(SUGGESTION_HEIGHT)))
                    {
                        // 클릭 시 해당 치트 ID로 자동완성
                        inputText = cheat.ID + " ";
                        selectedSuggestionIndex = 0;
                        UpdateSuggestions();
                        shouldFocusInput = true;
                    }

                    // 설명 표시
                    if (!string.IsNullOrEmpty(cheat.Description))
                    {
                        GUILayout.Label("  " + cheat.Description, descriptionStyle);
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// 파라미터 가이드 그리기
        /// </summary>
        private void DrawParameterGuide()
        {
            if (selectedCheat == null)
            {
                return;
            }

            var parameters = selectedCheat.GetParameterInfoList();

            // 치트 ID 및 사용법 표시
            GUILayout.Label(selectedCheat.GetUsage(), selectedSuggestionStyle, GUILayout.Height(SUGGESTION_HEIGHT));

            // 설명 표시
            if (!string.IsNullOrEmpty(selectedCheat.Description))
            {
                GUILayout.Label("  " + selectedCheat.Description, descriptionStyle);
            }

            // 파라미터가 없으면 여기서 종료
            if (parameters.Count == 0)
            {
                GUILayout.Label("  파라미터 없음 - Enter로 실행", descriptionStyle);
                return;
            }

            GUILayout.Space(5);

            // 파라미터 목록 표시
            GUILayout.Label("필요한 파라미터:", new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            });

            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                bool isCurrent = (i == currentParamIndex);

                // 현재 입력 중인 파라미터 하이라이트
                GUIStyle paramStyle;
                string prefix;

                if (isCurrent)
                {
                    paramStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.4f, 0.8f, 1f) }
                    };
                    prefix = "  → ";
                }
                else if (i < currentParamIndex)
                {
                    // 이미 입력 완료된 파라미터
                    paramStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        normal = { textColor = new Color(0.5f, 0.7f, 0.5f) }
                    };
                    prefix = "  ✓ ";
                }
                else
                {
                    // 아직 입력하지 않은 파라미터
                    paramStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    prefix = "    ";
                }

                GUILayout.Label($"{prefix}{param.Name} ({param.Type})", paramStyle);
            }
        }

        /// <summary>
        /// 로그 메시지 그리기
        /// </summary>
        private void DrawLogMessages()
        {
            if (logMessages.Count == 0)
            {
                return;
            }

            GUILayout.Space(10);
            GUILayout.Label("Log:", new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            });

            for (int i = logMessages.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(logMessages[i], logStyle);
            }
        }

        /// <summary>
        /// 명령어 실행
        /// </summary>
        private void ExecuteCommand()
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return;
            }

            string command = inputText.Trim();

            // 히스토리에 추가
            if (commandHistory.Count == 0 || commandHistory[commandHistory.Count - 1] != command)
            {
                commandHistory.Add(command);
            }
            historyIndex = -1;

            // 치트 실행
            bool success = CheatManager.Instance.ExecuteCheat(command);

            // 로그 추가
            string logMessage = success
                ? $"> {command}"
                : $"> {command} (실패)";
            AddLogMessage(logMessage);

            // 입력 초기화
            inputText = "";
            UpdateSuggestions();
            shouldFocusInput = true;
        }

        /// <summary>
        /// Tab 키로 자동완성
        /// 현재 입력과 가장 유사한(선택된) 항목으로 자동완성합니다.
        /// 예: "AddI" 입력 후 Tab → "AddItem "으로 자동완성
        /// </summary>
        private void AutoCompleteWithTab()
        {
            if (suggestions.Count == 0)
            {
                return;
            }

            // 선택된 인덱스가 유효한지 확인
            int index = selectedSuggestionIndex;
            if (index < 0 || index >= suggestions.Count)
            {
                index = 0;
            }

            // 선택된 치트 ID로 자동완성 (공백 추가하여 매개변수 입력 준비)
            inputText = suggestions[index].ID + " ";
            selectedSuggestionIndex = 0;
            UpdateSuggestions();
            shouldFocusInput = true;
        }

        /// <summary>
        /// 히스토리 탐색
        /// </summary>
        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0)
            {
                return;
            }

            historyIndex += direction;

            if (historyIndex < 0)
            {
                historyIndex = 0;
            }
            else if (historyIndex >= commandHistory.Count)
            {
                historyIndex = commandHistory.Count - 1;
                inputText = "";
                return;
            }

            inputText = commandHistory[commandHistory.Count - 1 - historyIndex];
            UpdateSuggestions();
        }

        /// <summary>
        /// 로그 메시지 추가
        /// </summary>
        private void AddLogMessage(string message)
        {
            logMessages.Add(message);
            if (logMessages.Count > MAX_LOG_MESSAGES)
            {
                logMessages.RemoveAt(0);
            }
        }
    }
}
#endif
