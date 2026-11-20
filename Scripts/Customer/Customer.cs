using System.Collections;
using UnityEngine;
using System;

public class Customer : MonoBehaviour
{
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float enterDistance = 10f;  // 오른쪽 밖에서 들어옴
    [SerializeField] private float swimY = -1f;  // 수중 이동 Y 고정값
    [SerializeField] private float reactionDuration = 0.8f;  // 서빙 후 반응 시간
    [SerializeField] private PatienceHeartUI heartUI;
    [SerializeField] private CustomerBeam beam;
    CustomerData data;  // 이 손님 유형 정보 (SO에서)
    SmoothieType actualOrder; // 진짜 원하는 것 (추리 정답)
    SmoothieType[] acceptableOrders;
    float patienceMax = 0;  // 최대 인내심
    float patienceCurrent= 0;  // 현재 남은 인내심
    
    int perfectTip = 50;
    int halfTip = 35;
    int pendingEarned = 0;
    public int seatIndex; // 튜브 자리
    public CustomerData Data => data;
    public int SeatIndex => seatIndex;
    public CustomerBeam Beam => beam;
    public event Action<Customer, int> OnProcessed;
    public CustomerState currentState { get; private set; } = CustomerState.Entering;
    public float PatienceNormalized => patienceMax > 0 ? patienceCurrent / patienceMax : 0f;

    void Update()
    {
        if (currentState != CustomerState.Waiting) return;
        if (TutorialUI.IsActive) return;
        
        patienceCurrent -= Time.deltaTime;
        heartUI?.UpdateHearts(patienceCurrent / patienceMax);
        
        if (patienceCurrent <= 0)
            OnPatienceRanOut();
    }
    private void ChangeState(CustomerState nextState)
    {
        if (currentState == nextState) return;

        currentState = nextState;
        switch (nextState)
        {
            case CustomerState.Entering:
                heartUI?.Hide();
                break;
            case CustomerState.Waiting:
                heartUI?.Show();
                break;
            case CustomerState.Leaving:
                heartUI?.Hide();
                if (gameObject.activeInHierarchy)
                    StartCoroutine(LeaveSequence());
                else
                    OnProcessed?.Invoke(this, pendingEarned); // 비활성 상태면 즉시 통보
                break;
        }
    }

    void OnPatienceRanOut()
    {
        pendingEarned = 0;
        SoundManager.Instance.PlayEffect(3);
        ChangeState(CustomerState.Leaving);
    }

    public void ReceiveSmoothie(SmoothieType served)
    {
        if (currentState != CustomerState.Waiting) return;
        int tip = JudgeAndGetTip(served);
        pendingEarned = tip;

        if (pendingEarned > 0)
            SoundManager.Instance.PlayEffect(2);
        else
            SoundManager.Instance.PlayEffect(3);
        ShowResultPopup(tip);
        ChangeState(CustomerState.Leaving);
    }

    private void ShowResultPopup(int tip)
    {
        string text = tip == perfectTip ? $"+{tip}"
                    : tip == halfTip   ? $"+{tip}"
                    : "X";

        // 손님 위치에 팝업 생성
        PopupUI.Instance?.Show(text, transform.position);
    }

    int JudgeAndGetTip(SmoothieType served)
    {   
        if (served == actualOrder) return perfectTip;  // 100%
        // 부분 정답 체크 (foreach로, using 불필요)
        if (acceptableOrders != null)
        {
            foreach (var ok in acceptableOrders)
            {
                if (served == ok)
                    return halfTip;   // 부분 정답
            }
        }
        return 0;  // 오답   
    }

    public void Setup(CustomerData data, int seatIndex)
    {
        this.data = data;
        this.seatIndex = seatIndex;
        beam?.Init(this);
        
        // 주문 설정
        actualOrder = data.actualOrder;
        acceptableOrders = data.acceptableOrders;
        
        // 인내심 설정
        patienceMax = data.patience;
        patienceCurrent = patienceMax;
        
        // 위치 배치 (seatIndex로 X좌표 결정)
        
        StartCoroutine(EnterDelay());
    }
    private IEnumerator EnterDelay()
    {
        yield return SwimInFromRight();
        ChangeState(CustomerState.Waiting);
    }

    private IEnumerator LeaveSequence()
    {
        bool satisfied = pendingEarned > 0;

        // 만족: 0.8초 반응, 불만족: 0.4초 반응
        float reaction = satisfied ? reactionDuration : reactionDuration * 0.5f;
        yield return new WaitForSeconds(reaction);

        yield return SwimOutToRight();
        OnProcessed?.Invoke(this, pendingEarned);
        Destroy(gameObject);
    }

    private IEnumerator SwimInFromRight()
    {
        Vector3 seat = transform.position;
        Vector3 endPos = new Vector3(seat.x, swimY, seat.z);
        Vector3 startPos = new Vector3(seat.x + enterDistance, swimY, seat.z);

        transform.position = startPos;

        float elapsed = 0f;
        float duration = enterDistance / swimSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        transform.position = endPos;
    }

    private IEnumerator SwimOutToRight()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.right * enterDistance;

        float elapsed = 0f;
        float duration = enterDistance / swimSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
    }

    

    private void EndDay()
    {
        GameEvents.RaiseDayEnded(pendingEarned);
    }
    public float GetXPosition() => transform.position.x;
}