using UnityEngine;
using System.Collections.Generic;

public class Blackhole : MonoBehaviour
{
    [Header("블랙홀 설정")]
    public float duration = 3f;
    public float suctionSpeed = 8f;
    public float finalRadius = 50f;
    public GameObject suctionEffectPrefab;
    public GameObject explosionEffectPrefab;

    [Header("콜라이더 설정")]
    public float minimumPixelPercentage = 0.1f;

    [Header("흡입 효과")]
    public float suctionRange = 30f;
    public float suctionForce = 15f;
    public AnimationCurve suctionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public LayerMask suctionTargets = -1;
    public bool affectPlayers = true;
    public bool affectEnemies = true;
    public bool affectProjectiles = true;
    public float maxSuctionVelocity = 20f;

    [Header("성능")]
    public float textureUpdateInterval = 0.05f;
    public int pixelBatchSize = 500;

    private SpriteRenderer groundRenderer;
    private Texture2D tex;
    private Color[] originalPixels;
    private Color[] currentPixels;
    private float suctionProgress = 0f;
    private float textureUpdateTimer = 0f;
    private PolygonCollider2D groundCollider;
    private bool isDestroyed = false;

    private bool[] pixelsChanged;
    private int textureWidth, textureHeight;
    private Vector2 blackholePixelPos;
    private float startTime;
    private float lastProcessedRadius = 0f;
    private GameObject groundObject;

    private List<Rigidbody2D> suctionTargetsList = new();
    private List<Transform> nonRigidbodyTargets = new();

    private static Texture2D sharedGroundTexture;
    private static Color[] sharedPixels;
    private static bool[] sharedPixelsChanged;
    private static int activeBlackholes = 0;

    void Start()
    {
        startTime = Time.time;
        InitializeBlackhole();
        activeBlackholes++;
    }

    void InitializeBlackhole()
    {
        groundObject = GameObject.FindGameObjectWithTag("Ground");
        if (!groundObject)
        {
            Debug.LogError("Ground 태그가 없음!");
            Destroy(gameObject);
            return;
        }

        groundRenderer = groundObject.GetComponent<SpriteRenderer>();
        groundCollider = groundObject.GetComponent<PolygonCollider2D>();

        if (!groundRenderer || !groundRenderer.sprite)
        {
            Debug.LogError("Ground에 SpriteRenderer나 Sprite가 없음!");
            Destroy(gameObject);
            return;
        }

        SetupTexture();
        CalculateBlackholePixelPosition();
        CreateSuctionEffect();
        StartSuctionDetection();

        Invoke(nameof(FinalizeDestruction), duration);
        Destroy(gameObject, duration + 1f);
    }

    void SetupTexture()
    {
        Texture2D originalTex = groundRenderer.sprite.texture;
        textureWidth = originalTex.width;
        textureHeight = originalTex.height;

        if (sharedGroundTexture == null)
        {
            sharedGroundTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            sharedGroundTexture.filterMode = FilterMode.Point;

            try
            {
                originalPixels = originalTex.GetPixels();
            }
            catch
            {
                Debug.LogError("Read/Write 체크 안됨!");
                return;
            }

            sharedPixels = new Color[originalPixels.Length];
            System.Array.Copy(originalPixels, sharedPixels, originalPixels.Length);
            sharedGroundTexture.SetPixels(sharedPixels);
            sharedGroundTexture.Apply();

            sharedPixelsChanged = new bool[originalPixels.Length];

            Sprite newSprite = Sprite.Create(
                sharedGroundTexture,
                groundRenderer.sprite.rect,
                groundRenderer.sprite.pivot / groundRenderer.sprite.rect.size,
                groundRenderer.sprite.pixelsPerUnit
            );
            groundRenderer.sprite = newSprite;
        }

        originalPixels = sharedPixels;
        currentPixels = sharedPixels;
        pixelsChanged = sharedPixelsChanged;
        tex = sharedGroundTexture;
    }

    void CalculateBlackholePixelPosition()
    {
        Vector3 localPos = groundRenderer.transform.InverseTransformPoint(transform.position);
        Bounds spriteBounds = groundRenderer.sprite.bounds;

        float normalizedX = (localPos.x + spriteBounds.extents.x) / spriteBounds.size.x;
        float normalizedY = (localPos.y + spriteBounds.extents.y) / spriteBounds.size.y;

        blackholePixelPos = new Vector2(
            Mathf.Clamp(normalizedX * textureWidth, 0, textureWidth - 1),
            Mathf.Clamp(normalizedY * textureHeight, 0, textureHeight - 1)
        );
    }

    void CreateSuctionEffect()
    {
        if (suctionEffectPrefab)
        {
            GameObject fx = Instantiate(suctionEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.SetParent(transform);
            Destroy(fx, duration);
        }
    }

    void StartSuctionDetection()
    {
        InvokeRepeating(nameof(UpdateSuctionTargets), 0.1f, 0.2f);
    }

    void UpdateSuctionTargets()
    {
        if (isDestroyed) return;

        suctionTargetsList.Clear();
        nonRigidbodyTargets.Clear();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, suctionRange, suctionTargets);

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            bool shouldAffect = false;
            if (affectPlayers && col.CompareTag("Player")) shouldAffect = true;
            if (affectEnemies && col.CompareTag("Enemy")) shouldAffect = true;
            if (affectProjectiles && col.CompareTag("Projectile")) shouldAffect = true;

            if (!shouldAffect) continue;

            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb) suctionTargetsList.Add(rb);
            else nonRigidbodyTargets.Add(col.transform);
        }
    }

    void FixedUpdate()
    {
        if (isDestroyed) return;

        float elapsed = Time.time - startTime;

        if (elapsed <= duration)
        {
            suctionProgress += Time.fixedDeltaTime;
            float currentRadius = suctionProgress * suctionSpeed;

            DeletePixelsInRing(lastProcessedRadius, currentRadius);
            lastProcessedRadius = currentRadius;

            ApplySuctionForce();
        }

        textureUpdateTimer += Time.fixedDeltaTime;
        if (textureUpdateTimer >= textureUpdateInterval)
        {
            ApplyTextureChanges();
            textureUpdateTimer = 0f;
        }
    }

    void DeletePixelsInRing(float innerRadius, float outerRadius)
    {
        float scale = groundRenderer.sprite.pixelsPerUnit * 0.04f;
        int inner = Mathf.RoundToInt(innerRadius * scale);
        int outer = Mathf.RoundToInt(outerRadius * scale);
        int cx = Mathf.RoundToInt(blackholePixelPos.x);
        int cy = Mathf.RoundToInt(blackholePixelPos.y);

        for (int x = -outer; x <= outer; x++)
        {
            for (int y = -outer; y <= outer; y++)
            {
                int d2 = x * x + y * y;
                if (d2 > inner * inner && d2 <= outer * outer)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px < 0 || px >= textureWidth || py < 0 || py >= textureHeight) continue;

                    int i = py * textureWidth + px;
                    if (currentPixels[i].a > 0.01f)
                    {
                        currentPixels[i] = Color.clear;
                        pixelsChanged[i] = true;
                    }
                }
            }
        }
    }

    void ApplySuctionForce()
    {
        Vector2 pos = transform.position;

        foreach (var rb in suctionTargetsList)
        {
            if (rb == null) continue;
            Vector2 dir = pos - rb.position;
            float dist = dir.magnitude;
            if (dist > suctionRange || dist < 0.5f) continue;

            float norm = 1f - (dist / suctionRange);
            float force = suctionCurve.Evaluate(norm) * suctionForce;
            Vector2 vec = dir.normalized * force;

            Vector2 newVel = rb.velocity + vec * Time.fixedDeltaTime;
            if (newVel.magnitude > maxSuctionVelocity)
                newVel = newVel.normalized * maxSuctionVelocity;

            if (rb.CompareTag("Player"))
                rb.AddForce(vec, ForceMode2D.Force);
            else
                rb.AddForce(vec * 1.5f, ForceMode2D.Force);
        }

        foreach (var t in nonRigidbodyTargets)
        {
            if (t == null) continue;
            Vector2 dir = pos - (Vector2)t.position;
            float dist = dir.magnitude;
            if (dist > suctionRange || dist < 0.5f) continue;

            float norm = 1f - (dist / suctionRange);
            float move = suctionCurve.Evaluate(norm) * suctionForce * 0.1f;
            t.position = (Vector2)t.position + dir.normalized * move * Time.fixedDeltaTime;
        }
    }

    void ApplyTextureChanges()
    {
        if (tex == null) return;

        bool changed = false;
        for (int i = 0; i < pixelsChanged.Length; i++)
        {
            if (pixelsChanged[i])
            {
                changed = true;
                pixelsChanged[i] = false;
            }
        }

        if (changed)
        {
            tex.SetPixels(currentPixels);
            tex.Apply();
        }
    }

    void FinalizeDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        ApplyTextureChanges();

        if (explosionEffectPrefab)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        UpdateGroundCollider();
    }

    void UpdateGroundCollider()
    {
        if (groundCollider)
        {
            DestroyImmediate(groundCollider);
            groundCollider = null;
        }

        StartCoroutine(RegenerateColliderAfterDelay());
    }

    System.Collections.IEnumerator RegenerateColliderAfterDelay()
    {
        yield return new WaitForEndOfFrame();

        if (HasRemainingPixels())
        {
            var newCol = groundRenderer.gameObject.AddComponent<PolygonCollider2D>();
            if (newCol != null)
            {
                newCol.isTrigger = false;
                groundCollider = newCol;
            }
        }
        else
        {
            Debug.Log("Ground에 픽셀이 없어 Ground 오브젝트 비활성화 처리.");

            foreach (var col in groundObject.GetComponents<Collider2D>())
                DestroyImmediate(col);

            if (groundRenderer) groundRenderer.enabled = false;
            if (groundObject) groundObject.SetActive(false);
        }
    }

    bool HasRemainingPixels()
    {
        if (sharedPixels == null) return false;

        int count = 0;
        foreach (var p in sharedPixels)
        {
            if (p.a > 0.01f) count++;
        }

        float percent = (float)count / sharedPixels.Length * 100f;
        return percent >= minimumPixelPercentage;
    }

    void OnDestroy()
    {
        activeBlackholes--;
        CancelInvoke();
    }
}
