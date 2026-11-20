using UnityEngine;

public class PenguinServer : MonoBehaviour
{
    [SerializeField] private PenguinHands hands;
    [SerializeField] private SwimModeCoordinator swimCoordinator;
    [SerializeField] private float serveRange = 2f;

    private void Awake()
    {
        if (hands == null) hands = GetComponent<PenguinHands>();
        if (swimCoordinator == null) swimCoordinator = GetComponent<SwimModeCoordinator>();
    }

    private void OnEnable()
    {
        swimCoordinator.OnReachedSurface += TryServe;
    }

    private void OnDisable()
    {
        swimCoordinator.OnReachedSurface -= TryServe;
    }

    // 수면에 떠오른 순간 호출됨
    private void TryServe()
    {
        // 스무디 들고 있을 때만 서빙
        if (hands.Content != PenguinHands.HandContent.Smoothie)
        {
            Debug.Log("스무디 없음 (산소 회복만)");
            return;
        }

        // 내 X좌표로 가장 가까운 손님 찾기
        float myX = transform.position.x;
        Customer target = DayManager.Instance.GetCustomerAtPosition(myX, serveRange);

        if (target == null)
        {
            Debug.Log("근처에 서빙할 손님 없음");
            return;
        }

        // 서빙!
        target.ReceiveSmoothie(hands.HeldSmoothie);
        hands.Clear();
        Debug.Log($"서빙 완료: {target.name}");
    }
}