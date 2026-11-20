using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public string message;           // "A/D로 이동"
        public GameObject arrowIndicator; // 화살표 오브젝트
        public KeyCode waitForKey;        // 이 키 누르면 다음 (KeyCode.None이면 아무 키)
    }
    public static bool IsActive { get; private set; } = false;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private float introFadeDuration = 1.2f;
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text skipText;   // "아무 키나 눌러 계속"
    [SerializeField] private List<TutorialStep> steps;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private float stepDelay = 0.2f;
    private InputAction _moveAction;

    private int _currentStep = 0;
    private static bool _completed = false;
    public static bool IsCompleted => _completed;
    
    private void OnEnable()
        => GameEvents.OnDayStarted += TryShowTutorial;
    private void OnDisable()
        => GameEvents.OnDayStarted -= TryShowTutorial;
    private void Start()
    {
        _moveAction = playerInput.actions.FindAction("Move");
    }
    private void TryShowTutorial(int day)
    {
        // Day1이고 아직 안 봤으면
        if (day == 1 && !_completed)
            StartCoroutine(RunTutorial());
    }

    private IEnumerator RunTutorial()
    {
        IsActive = true;

        if (introPanel != null)
        {
            introPanel.SetActive(true);
            yield return FadeIntro(0f, 1f);     // 페이드 인
            yield return null;                  // 이 프레임 입력 무시
            while (!Input.anyKeyDown)
                yield return null;
            yield return FadeIntro(1f, 0f);     // 페이드 아웃
            introPanel.SetActive(false);
            yield return new WaitForSecondsRealtime(0.1f);
        }

        panel.SetActive(true);
        _currentStep = 0;

        foreach (var step in steps)
        {
            // 메시지 페이드인 느낌 (선택)
            messageText.text = "";
            messagePanel?.SetActive(false);
            yield return new WaitForSeconds(stepDelay * 0.5f);
            messageText.text = step.message;
            messagePanel?.SetActive(true);

            step.arrowIndicator?.SetActive(true);
            yield return WaitForInput(step);
            step.arrowIndicator?.SetActive(false);
            messagePanel?.SetActive(false);

            // 스텝 완료 후 딜레이
            yield return new WaitForSeconds(stepDelay);
            _currentStep++;
        }

        _completed = true;

        // 패널 즉시 끄지 말고 페이드아웃 느낌으로
        yield return new WaitForSeconds(0.3f);
        IsActive = false;
        panel.SetActive(false);
    }

    private IEnumerator FadeIntro(float from, float to)
    {
        if (introCanvasGroup == null) yield break;
        introCanvasGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < introFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            introCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / introFadeDuration);
            yield return null;
        }
        introCanvasGroup.alpha = to;
    }

    private IEnumerator WaitForInput(TutorialStep step)
    {
        yield return null;
        if (step.waitForKey != KeyCode.None)
        {
            while (!Input.GetKeyDown(step.waitForKey))
                yield return null;
        }
        else
        {
            while (true)
            {
                Vector2 move = _moveAction.ReadValue<Vector2>();
                if (Mathf.Abs(move.x) > 0.1f) yield break;
                yield return null;
            }
        }
    }
}