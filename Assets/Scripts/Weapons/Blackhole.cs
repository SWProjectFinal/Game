using UnityEngine;

public class Blackhole : MonoBehaviour
{
    [Header("블랙홀 설정")]
    public float duration = 3f;
    public float suctionSpeed = 8f;
    public float finalRadius = 50f;
    public GameObject suctionEffectPrefab;
    public GameObject explosionEffectPrefab;

    [Header("콜라이더 설정")]
    public float minimumPixelPercentage = 0.1f; // 전체 픽셀의 0.1% 이상 남아있어야 콜라이더 생성
    public float textureUpdateInterval = 0.02f;
    public int pixelBatchSize = 1000;

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

    // 정적 변수로 텍스처 관리 (여러 블랙홀이 동시에 존재할 수 있으므로)
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
        GameObject ground = GameObject.FindGameObjectWithTag("Ground");
        if (ground == null)
        {
            Debug.LogError("Ground 태그가 있는 오브젝트를 찾을 수 없음!");
            Destroy(gameObject);
            return;
        }

        groundRenderer = ground.GetComponent<SpriteRenderer>();
        groundCollider = ground.GetComponent<PolygonCollider2D>();

        if (groundRenderer == null || groundRenderer.sprite == null)
        {
            Debug.LogError("Ground 오브젝트에 SpriteRenderer나 Sprite가 없음!");
            Destroy(gameObject);
            return;
        }

        SetupTexture();
        CalculateBlackholePixelPosition();
        CreateSuctionEffect();

        Invoke(nameof(FinalizeDestruction), duration);
        Destroy(gameObject, duration + 1f);
    }

    void SetupTexture()
    {
        Texture2D originalTex = groundRenderer.sprite.texture;
        textureWidth = originalTex.width;
        textureHeight = originalTex.height;

        // 첫 번째 블랙홀이거나 기존 텍스처가 없는 경우에만 새로 생성
        if (sharedGroundTexture == null)
        {
            // 새 텍스처 생성 (읽기/쓰기 가능)
            sharedGroundTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            sharedGroundTexture.filterMode = FilterMode.Point;

            // 원본 텍스처가 읽기 가능한지 확인
            try
            {
                originalPixels = originalTex.GetPixels();
            }
            catch (UnityException)
            {
                Debug.LogError("Ground 텍스처가 읽기 불가능합니다. Import Settings에서 Read/Write Enabled를 체크하세요!");
                return;
            }

            sharedPixels = new Color[originalPixels.Length];
            System.Array.Copy(originalPixels, sharedPixels, originalPixels.Length);

            sharedGroundTexture.SetPixels(sharedPixels);
            sharedGroundTexture.Apply();

            sharedPixelsChanged = new bool[originalPixels.Length];

            // 새 스프라이트 생성 및 적용
            Sprite newSprite = Sprite.Create(
                sharedGroundTexture,
                groundRenderer.sprite.rect,
                groundRenderer.sprite.pivot / groundRenderer.sprite.rect.size,
                groundRenderer.sprite.pixelsPerUnit
            );
            groundRenderer.sprite = newSprite;
        }
        else
        {
            // 기존 공유 텍스처 사용
            originalPixels = sharedPixels;
        }

        // 각 블랙홀은 공유 데이터 참조
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

        Debug.Log($"블랙홀 픽셀 위치: ({blackholePixelPos.x}, {blackholePixelPos.y})");
    }

    void CreateSuctionEffect()
    {
        if (suctionEffectPrefab != null)
        {
            GameObject fx = Instantiate(suctionEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.SetParent(transform);
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
            
            // 점진적으로 원형 확장 (링 형태로 처리)
            DeletePixelsInRing(lastProcessedRadius, currentRadius);
            lastProcessedRadius = currentRadius;
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
        // Unity 단위를 픽셀로 변환 (스케일 조정)
        float pixelScale = groundRenderer.sprite.pixelsPerUnit * 0.08f;
        int innerPixelRadius = Mathf.RoundToInt(innerRadius * pixelScale);
        int outerPixelRadius = Mathf.RoundToInt(outerRadius * pixelScale);

        int centerX = Mathf.RoundToInt(blackholePixelPos.x);
        int centerY = Mathf.RoundToInt(blackholePixelPos.y);

        int innerRadiusSquared = innerPixelRadius * innerPixelRadius;
        int outerRadiusSquared = outerPixelRadius * outerPixelRadius;

        // 링 영역 내의 픽셀 삭제
        for (int x = -outerPixelRadius; x <= outerPixelRadius; x++)
        {
            for (int y = -outerPixelRadius; y <= outerPixelRadius; y++)
            {
                int distanceSquared = x * x + y * y;
                
                if (distanceSquared > innerRadiusSquared && distanceSquared <= outerRadiusSquared)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    if (IsValidPixelCoordinate(pixelX, pixelY))
                    {
                        int pixelIndex = pixelY * textureWidth + pixelX;

                        if (currentPixels[pixelIndex].a > 0.01f)
                        {
                            currentPixels[pixelIndex] = Color.clear;
                            pixelsChanged[pixelIndex] = true;
                        }
                    }
                }
            }
        }
    }

    bool IsValidPixelCoordinate(int x, int y)
    {
        return x >= 0 && x < textureWidth && y >= 0 && y < textureHeight;
    }

    void ApplyTextureChanges()
    {
        if (sharedGroundTexture == null) return;

        bool hasChanges = false;
        for (int i = 0; i < pixelsChanged.Length; i++)
        {
            if (pixelsChanged[i])
            {
                hasChanges = true;
                pixelsChanged[i] = false;
            }
        }

        if (hasChanges)
        {
            sharedGroundTexture.SetPixels(currentPixels);
            sharedGroundTexture.Apply();
        }
    }

    void FinalizeDestruction()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        ApplyTextureChanges();

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        UpdateGroundCollider();
    }

    void UpdateGroundCollider()
    {
        if (groundCollider != null)
        {
            DestroyImmediate(groundCollider);
        }

        // 프레임 지연 후 콜라이더 재생성
        StartCoroutine(RegenerateColliderAfterDelay());
    }

    System.Collections.IEnumerator RegenerateColliderAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        
        // 남은 픽셀이 있는지 확인
        if (HasRemainingPixels())
        {
            PolygonCollider2D newCollider = groundRenderer.gameObject.AddComponent<PolygonCollider2D>();
            if (newCollider != null)
            {
                newCollider.isTrigger = false;
            }
        }
        else
        {
            Debug.Log("Ground에 남은 픽셀이 없어서 콜라이더를 생성하지 않습니다.");
        }
    }

    bool HasRemainingPixels()
    {
        if (sharedPixels == null) return false;
        
        int visiblePixelCount = 0;
        int totalPixels = sharedPixels.Length;
        
        // 투명하지 않은 픽셀 수 계산
        for (int i = 0; i < totalPixels; i++)
        {
            if (sharedPixels[i].a > 0.01f)
            {
                visiblePixelCount++;
            }
        }
        
        // 최소 픽셀 비율 확인
        float pixelPercentage = (float)visiblePixelCount / totalPixels * 100f;
        
        Debug.Log($"남은 픽셀: {visiblePixelCount}/{totalPixels} ({pixelPercentage:F2}%)");
        
        return pixelPercentage >= minimumPixelPercentage;
    }

    void OnDestroy()
    {
        activeBlackholes--;
        
        // 마지막 블랙홀이 파괴될 때만 텍스처 정리
        // 하지만 Ground가 여전히 사용 중이므로 텍스처는 파괴하지 않음
        // 대신 Scene이 변경될 때 정리하도록 함
        
        // 만약 정말 텍스처를 정리하고 싶다면 Ground에서 텍스처를 분리한 후 정리
        // 하지만 일반적으로는 Scene 전환 시 자동으로 정리됨
    }

    // Scene 전환 시 정적 변수 초기화
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CleanupStaticResources();
        }
    }

    // 정적 리소스 정리 (필요시 호출)
    public static void CleanupStaticResources()
    {
        if (sharedGroundTexture != null && activeBlackholes == 0)
        {
            DestroyImmediate(sharedGroundTexture);
            sharedGroundTexture = null;
            sharedPixels = null;
            sharedPixelsChanged = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            float currentRadius = suctionProgress * suctionSpeed;
            Gizmos.DrawWireSphere(transform.position, currentRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, finalRadius);
        }
    }
}