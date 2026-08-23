using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class DyingMessageCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("キャンバス設定")]
    [SerializeField] private int textureWidth = 1080;
    [SerializeField] private int textureHeight = 1080;

    [Header("ペン設定")]
    [SerializeField] private Color currentPenColor = new Color(0.8f, 0.05f, 0.05f, 1f); // 赤（血痕風）
    [SerializeField] private int currentBrushSize = 14;

    [Header("血のシミ・飛び散り演出設定")]
    [SerializeField] private bool enableSplatter = true;
    [Range(0f, 1f)]
    [SerializeField] private float splatterChance = 0.35f; // シミが発生する確率 (0〜1)
    [SerializeField] private int splatterRadius = 35;       // シミが飛ぶ半径

    [Header("手の画像演出")]
    [SerializeField] private RectTransform handUI; // 追従させる手のUI (RectTransform)
    [SerializeField] private Vector2 handOffset = new Vector2(150f, -200f); // 指先を描画位置に合わせるための位置ズレ補正

    private RawImage rawImage;
    private Texture2D drawableTexture;
    private Color32[] pixels;
    private Color32 clearColor = new Color32(0, 0, 0, 0);

    private Vector2 lastTouchPos;
    private bool isDrawing = false;
    private Camera uiCamera;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        InitTexture();

        if (handUI != null)
        {
            handUI.gameObject.SetActive(false); // 初期状態は非表示
        }
    }

    public void InitTexture()
    {
        drawableTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawableTexture.filterMode = FilterMode.Bilinear;
        pixels = new Color32[textureWidth * textureHeight];
        ClearCanvas();
        rawImage.texture = drawableTexture;
    }

    public void ClearCanvas()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clearColor;
        }
        drawableTexture.SetPixels32(pixels);
        drawableTexture.Apply();
    }

    // --- タッチ / マウスイベント ---

    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform, eventData.position, uiCamera, out Vector2 localPos))
        {
            Vector2 pixelPos = LocalToPixelPos(localPos);
            DrawBrush(pixelPos);
            lastTouchPos = pixelPos;
            isDrawing = true;

            UpdateHandPosition(eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform, eventData.position, uiCamera, out Vector2 localPos))
        {
            Vector2 currentPixelPos = LocalToPixelPos(localPos);
            DrawLine(lastTouchPos, currentPixelPos);
            lastTouchPos = currentPixelPos;

            UpdateHandPosition(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;

        // 手の画像を非表示に
        if (handUI != null)
        {
            handUI.gameObject.SetActive(false);
        }
    }

    // --- 手の位置移動 ---

    private void UpdateHandPosition(Vector2 screenPos)
    {
        if (handUI == null) return;

        if (!handUI.gameObject.activeSelf)
        {
            handUI.gameObject.SetActive(true);
        }

        // スキャン座標からUI座標へ変換し、指先のオフセットを加算
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handUI.parent as RectTransform, screenPos, uiCamera, out Vector2 localPoint);

        handUI.anchoredPosition = localPoint + handOffset;
    }

    // --- 描画＆血痕演出ロジック ---

    private Vector2 LocalToPixelPos(Vector2 localPos)
    {
        Rect rect = rawImage.rectTransform.rect;
        float xNorm = (localPos.x - rect.x) / rect.width;
        float yNorm = (localPos.y - rect.y) / rect.height;

        int x = Mathf.Clamp(Mathf.RoundToInt(xNorm * textureWidth), 0, textureWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(yNorm * textureHeight), 0, textureHeight - 1);

        return new Vector2(x, y);
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(distance);

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            DrawBrush(point);

            // 血のシミ・飛沫演出（ランダム付与）
            if (enableSplatter && Random.value < splatterChance)
            {
                GenerateBloodSplatter(point);
            }
        }

        drawableTexture.SetPixels32(pixels);
        drawableTexture.Apply();
    }

    private void DrawBrush(Vector2 center)
    {
        int cx = (int)center.x;
        int cy = (int)center.y;
        Color32 drawColor = currentPenColor;

        for (int x = cx - currentBrushSize; x <= cx + currentBrushSize; x++)
        {
            for (int y = cy - currentBrushSize; y <= cy + currentBrushSize; y++)
            {
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                {
                    // 少し不規則な円形（手描きの滲み感）
                    float distSqr = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (distSqr <= currentBrushSize * currentBrushSize)
                    {
                        pixels[y * textureWidth + x] = drawColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 血のシミ・飛び散りを線の周囲にランダム生成
    /// </summary>
    private void GenerateBloodSplatter(Vector2 center)
    {
        // 飛び散る小粒の数
        int drops = Random.Range(1, 4);
        Color32 drawColor = currentPenColor;

        for (int d = 0; d < drops; d++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * splatterRadius;
            int dropX = Mathf.Clamp((int)(center.x + randomOffset.x), 0, textureWidth - 1);
            int dropY = Mathf.Clamp((int)(center.y + randomOffset.y), 0, textureHeight - 1);

            int dropSize = Random.Range(1, 4); // 小さな粒

            for (int x = dropX - dropSize; x <= dropX + dropSize; x++)
            {
                for (int y = dropY - dropSize; y <= dropY + dropSize; y++)
                {
                    if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                    {
                        pixels[y * textureWidth + x] = drawColor;
                    }
                }
            }
        }
    }

    // --- パブリック制御用 ---

    public void SetPenColorHex(string hexCode)
    {
        if (ColorUtility.TryParseHtmlString(hexCode, out Color color))
        {
            currentPenColor = color;
        }
    }

    public void SetEraserMode() => currentPenColor = clearColor;
    public void SetBrushSize(int size) => currentBrushSize = size;
}