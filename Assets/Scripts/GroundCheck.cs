using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public Transform groundCheckPoint; // �� �� ��ġ
    public float checkRadius = 0.1f;
    public LayerMask groundLayer;

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, checkRadius, groundLayer);
    }
}
