using UnityEngine;
using UnityEngine.UI;

public class PlayerPhysicsUI : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private PlayerController player;

    [Header("UI Toggles")]
    [SerializeField] private Toggle variableJumpToggle;
    [SerializeField] private Toggle coyoteTimeToggle;
    [SerializeField] private Toggle squashToggle;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        // 1. 플레이어의 현재 설정값을 UI 토글 상태에 최초 1회 동기화
        if (variableJumpToggle != null)
        {
            variableJumpToggle.isOn = player.UseVariableJump;
            // 값 변경 이벤트 등록 (람다식 바인딩)
            variableJumpToggle.onValueChanged.AddListener(isOn => player.UseVariableJump = isOn);
        }

        if (coyoteTimeToggle != null)
        {
            coyoteTimeToggle.isOn = player.UseCoyoteTime;
            coyoteTimeToggle.onValueChanged.AddListener(isOn => player.UseCoyoteTime = isOn);
        }

        if (squashToggle != null)
        {
            squashToggle.isOn = player.UseSquashAndStretch;
            squashToggle.onValueChanged.AddListener(isOn => player.UseSquashAndStretch = isOn);
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지 및 리스너 해제
        if (variableJumpToggle != null) variableJumpToggle.onValueChanged.RemoveAllListeners();
        if (coyoteTimeToggle != null) coyoteTimeToggle.onValueChanged.RemoveAllListeners();
        if (squashToggle != null) squashToggle.onValueChanged.RemoveAllListeners();
    }
}
