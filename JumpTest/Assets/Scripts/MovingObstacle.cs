using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("위아래 이동 거리 (반경)")]
    [SerializeField] private float moveDistance = 2f;
    [Tooltip("움직이는 속도")]
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        // 시작 기준 위치 저장
        startPosition = transform.position;
    }

    private void Update()
    {
        // Sine 함수를 이용해 위아래 부드러운 왕복 운동
        float newY = startPosition.y + Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    // 에디터 씬 뷰에서 이동 경로 미리보기
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center + Vector3.up * moveDistance, center + Vector3.down * moveDistance);
    }
}
