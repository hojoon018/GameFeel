using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 9f;
    private float horizontalInput;

    [Header("Jump Physics")]
    [SerializeField] private float jumpForce = 16f;

    [Header("Feature Toggles")]
    [Tooltip("체크 시 키를 떼었을 때 낮게 뛰는 가변 점프 활성화")]
    [SerializeField] private bool useVariableJump = true;
    [Tooltip("체크 시 낙하 직후 유예 시간을 주는 코요테 타임 활성화")]
    [SerializeField] private bool useCoyoteTime = true;
    [Tooltip("체크 시 점프/착지 찌그러짐 시각 연출 활성화")]
    [SerializeField] private bool useSquashAndStretch = true;

    [Header("Variable Jump Tuning")]
    [Range(0f, 1f)]
    [Tooltip("가변 점프: 점프 도중 키를 뗐을 때 y속도를 줄이는 비율")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Gravity Tuning (Fast Fall)")]
    [Tooltip("기본 상승 시 중력 스케일")]
    [SerializeField] private float defaultGravityScale = 3.5f;
    [Tooltip("정점을 찍고 낙하할 때 적용할 중력 배수")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [Tooltip("가변 점프 활성화 상태에서 스페이스바를 일찍 뗐을 때 급격히 떨어지게 만드는 중력 배수")]
    [SerializeField] private float fastFallMultiplier = 2.5f;

    [Header("Jump Buffering")]
    [Tooltip("착지 전 점프 입력을 유효하게 유지할 시간 (초 단위)")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    [Header("Coyote Time")]
    [Tooltip("발판을 벗어난 뒤 점프를 허용하는 유예 시간")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;
    private bool wasGroundedLastFrame;

    [Header("Squash & Stretch (Visual)")]
    [Tooltip("스프라이트가 있는 자식 오브젝트의 Transform")]
    [SerializeField] private Transform spriteTransform;
    [Tooltip("점프 시 늘어나는 스케일 (X는 얇게, Y는 길게)")]
    [SerializeField] private Vector3 jumpStretchScale = new Vector3(0.7f, 1.4f, 1f);
    [Tooltip("착지 시 찌그러지는 스케일 (X는 넓게, Y는 낮게)")]
    [SerializeField] private Vector3 landSquashScale = new Vector3(1.35f, 0.65f, 1f);
    [Tooltip("원래 크기로 복귀하는 속도")]
    [SerializeField] private float squashResetSpeed = 12f;

    private Vector3 originalScale = Vector3.one;
    private Coroutine squashCoroutine;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = defaultGravityScale;

        // Sprite Transform을 따로 지정하지 않았을 경우 처리
        if (spriteTransform == null)
        {
            Transform child = transform.Find("SpriteHolder");
            if (child != null)
            {
                spriteTransform = child;
            }
            else
            {
                spriteTransform = transform;
                Debug.LogWarning("[PlayerController] spriteTransform이 지정되지 않아 Player 본체를 변형합니다. 콜라이더도 함께 변형될 수 있습니다.");
            }
        }

        originalScale = spriteTransform.localScale;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1. 좌우 이동 입력
        horizontalInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;

        // 2. 바닥 체크
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // 3. 착지 순간 감지 -> 스쿼시 연출
        if (isGrounded && !wasGroundedLastFrame)
        {
            if (useSquashAndStretch)
            {
                TriggerSquash(landSquashScale);
            }
        }
        wasGroundedLastFrame = isGrounded;

        // 4. 코요테 타임 타이머 갱신 (토글 적용)
        if (useCoyoteTime)
        {
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }
        else
        {
            // 코요테 타임을 쓰지 않을 때는 바닥에 있을 때만 즉각 점프 가능
            coyoteTimeCounter = isGrounded ? 1f : 0f;
        }

        // 5. 점프 입력 버퍼링 타이머 갱신
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 6. 점프 발동 조건 체크
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            if (useSquashAndStretch)
            {
                TriggerSquash(jumpStretchScale);
            }
            ExecuteJump();
        }

        // 7. 가변 점프: 도중에 스페이스바를 떼면 상승 속도 감소 (토글 적용)
        if (useVariableJump && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            float currentYVelocity = GetVelocity().y;
            if (currentYVelocity > 0f)
            {
                SetVelocity(new Vector2(GetVelocity().x, currentYVelocity * jumpCutMultiplier));
                coyoteTimeCounter = 0f;
            }
        }

        // 8. 중력 조절
        ApplyGravityScale();
    }

    private void FixedUpdate()
    {
        SetVelocity(new Vector2(horizontalInput * moveSpeed, GetVelocity().y));
    }

    private void ExecuteJump()
    {
        SetVelocity(new Vector2(GetVelocity().x, jumpForce));
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void ApplyGravityScale()
    {
        float currentYVelocity = GetVelocity().y;

        if (isGrounded)
        {
            rb.gravityScale = defaultGravityScale;
        }
        else if (currentYVelocity < 0f) // 떨어질 때 빠른 하강
        {
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        // 가변 점프가 켜져 있을 때만 키를 뗐을 때의 빠른 감속 중력 적용
        else if (useVariableJump && currentYVelocity > 0f && !Keyboard.current.spaceKey.isPressed)
        {
            rb.gravityScale = defaultGravityScale * fastFallMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    // 스쿼시 & 스트레치 실행
    private void TriggerSquash(Vector3 targetScale)
    {
        if (squashCoroutine != null)
        {
            StopCoroutine(squashCoroutine);
        }
        squashCoroutine = StartCoroutine(SquashRoutine(targetScale));
    }

    private IEnumerator SquashRoutine(Vector3 targetScale)
    {
        spriteTransform.localScale = targetScale;

        while (Vector3.Distance(spriteTransform.localScale, originalScale) > 0.01f)
        {
            spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, originalScale, Time.deltaTime * squashResetSpeed);
            yield return null;
        }

        spriteTransform.localScale = originalScale;
        squashCoroutine = null;
    }

    // 인스펙터에서 실시간으로 Squash 토글을 끌 때 원래 크기로 안전 복귀
    private void OnValidate()
    {
        if (!useSquashAndStretch && spriteTransform != null && originalScale != Vector3.zero)
        {
            spriteTransform.localScale = originalScale;
        }
    }

    private Vector2 GetVelocity()
    {
        #if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
        #else
        return rb.velocity;
        #endif
    }

    private void SetVelocity(Vector2 velocity)
    {
        #if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
        #else
        rb.velocity = velocity;
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
    
    public bool UseVariableJump
    {
        get => useVariableJump;
        set => useVariableJump = value;
    }

    public bool UseCoyoteTime
    {
        get => useCoyoteTime;
        set
        {
            useCoyoteTime = value;
            // 다시 켰을 때 바닥에 서 있다면 즉시 유예시간 재충전
            if (value && isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
        }
    }

    public bool UseSquashAndStretch
    {
        get => useSquashAndStretch;
        set
        {
            useSquashAndStretch = value;
            // 껐을 때 진행 중인 코루틴 멈추고 크기 즉각 원복
            if (!value)
            {
                if (squashCoroutine != null)
                {
                    StopCoroutine(squashCoroutine);
                    squashCoroutine = null;
                }
                if (spriteTransform != null)
                {
                    spriteTransform.localScale = originalScale;
                }
            }
        }
    }
}
