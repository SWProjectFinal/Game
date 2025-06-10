using UnityEngine;

[ExecuteInEditMode] // 에디터에서 실행 가능하게!
public class ShrinkPolygonCollider : MonoBehaviour
{
    public PolygonCollider2D targetCollider;
    public float scaleFactor = 0.5f; // 0.5 = 절반 크기

    [ContextMenu("Shrink Collider")]
    void Shrink()
    {
        if (targetCollider == null) return;

        for (int i = 0; i < targetCollider.pathCount; i++)
        {
            Vector2[] path = targetCollider.GetPath(i);
            for (int j = 0; j < path.Length; j++)
            {
                path[j] *= scaleFactor;
            }
            targetCollider.SetPath(i, path);
        }

        Debug.Log("콜라이더 축소 완료!");
    }
}
