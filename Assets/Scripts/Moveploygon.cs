using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class MovePolygonPoints : MonoBehaviour
{
    public float moveY = 1f;  // 위로 얼마나 옮길지

    void Start()
    {
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();

        for (int i = 0; i < col.pathCount; i++)
        {
            Vector2[] path = col.GetPath(i);
            for (int j = 0; j < path.Length; j++)
            {
                path[j].y += moveY;
            }
            col.SetPath(i, path);
        }

        Debug.Log("Collider points moved up by " + moveY);
    }
}
