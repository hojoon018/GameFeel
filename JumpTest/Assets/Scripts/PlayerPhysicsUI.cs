using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerPhysicsUI : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private PlayerController player;

    [Header("UI Toggles")]
    [SerializeField] private Toggle variableJumpToggle;
    [SerializeField] private Toggle coyoteTimeToggle;
    [SerializeField] private Toggle squashToggle;
    
    [Header("Jump Status Image (ON/OFF)")] 
        [Tooltip("상태를 표시할 UI 이미지")] 
        [SerializeField] private Image spaceBarImage;
        [Tooltip("키를 눌렀을 때 (ON) 색상")]
        [SerializeField] private Color onColor = Color.green; // 기본값 초록색
        [Tooltip("키를 떼었을 때 (OFF) 색상")]
        [SerializeField] private Color offColor = new Color(1f, 1f, 1f, 0.5f); // 기본값 반투명 흰색

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
        
        if (spaceBarImage != null)
        {
            spaceBarImage.color = offColor;
        }
    }

    private void Update()
    {
        if (spaceBarImage == null || Keyboard.current == null) return;
        
        bool isSpacePressed = Keyboard.current.spaceKey.isPressed;
        
        spaceBarImage.color = isSpacePressed ? onColor : offColor;
    }
    private void OnDestroy()
    {
        // 메모리 누수 방지 및 리스너 해제
        if (variableJumpToggle != null) variableJumpToggle.onValueChanged.RemoveAllListeners();
        if (coyoteTimeToggle != null) coyoteTimeToggle.onValueChanged.RemoveAllListeners();
        if (squashToggle != null) squashToggle.onValueChanged.RemoveAllListeners();
    }
}
