using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class DyingMessageCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("キャンバス設定")]
    [SerializeField] private int textureWidth = 1080;
    [SerializeField] private int textureHeight = 1080;
    [SerializeField] private int maxUndoSteps = 5;
    [SerializeField] private Color canvasBackgroundColor = new Color(1f, 1f, 1f, 0.5f); // 半透明の白 (Alpha: 0.5)

    [Header("ペン・太さ可変設定")]
    [SerializeField] private Color currentPenColor = new Color(1.0f, 0.0f, 0.698f, 1.0f);
    [SerializeField] private bool enableDynamicSize = true;
    [SerializeField] private int minBrushSize = 5;
    [SerializeField] private int maxBrushSize = 20;
    [SerializeField] private float fastSpeedThreshold = 2500f;

    [Header("血のシミ・飛び散り演出設定")]
    [SerializeField] private bool enableSplatter = true;
    [SerializeField] private float slowSpeedThreshold = 800f;
    [Range(0f, 1f)]
    [SerializeField] private float splatterChance = 0.45f;
    [SerializeField] private int splatterRadius = 40;

    [Header("手の画像演出")]
    [SerializeField] private RectTransform handUI;
    [SerializeField] private Vector2 handOffset = new Vector2(150f, -200f);
    [SerializeField] private Vector2 defaultHandPosition = new Vector2(-350f, -650f);
    [SerializeField] private float returnSpeed = 8f;

    private RawImage rawImage;
    private Texture2D drawableTexture;
    private Color32[] pixels;
    private Color32 clearColor; // キャンバス背景色

    private Vector2 lastTouchPos;
    private bool isDrawing = false;
    private Camera uiCamera;
    private float currentMoveSpeed = 0f;

   private bool isFirstDrag = false; // 追加

   private float idleTimer = 0f; // 👈 この行を追加してください

    private Stack<Color32[]> undoHistory = new Stack<Color32[]>();

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        clearColor = canvasBackgroundColor;
        InitTexture();

        if (handUI != null)
        {
            handUI.gameObject.SetActive(true);
            handUI.anchoredPosition = defaultHandPosition;
        }
    }

    private void Update()
    {
        if (handUI == null) return;

        // タイマーの進む速度を 3f から 1.2f に落としてゆっくりに
        idleTimer += Time.deltaTime * 1.2f;
        float breathX = Mathf.Sin(idleTimer * 1.2f) * 8f + (Mathf.PingPong(idleTimer * 0.6f, 1f) - 0.5f) * 5f;
        float breathY = Mathf.Cos(idleTimer * 1.8f) * 10f;
        Vector2 breathOffset = new Vector2(breathX, breathY);

        // 描いていない（手を離している）時は defaultHandPosition に向けて揺れながら戻る
        if (!isDrawing)
        {
            Vector2 targetPos = defaultHandPosition + breathOffset;
            handUI.anchoredPosition = Vector2.Lerp(
                handUI.anchoredPosition,
                targetPos,
                Time.deltaTime * returnSpeed
            );
        }
    }

    public void InitTexture()
    {
        drawableTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawableTexture.filterMode = FilterMode.Bilinear;
        pixels = new Color32[textureWidth * textureHeight];
        undoHistory.Clear();
        ClearCanvas();
        rawImage.texture = drawableTexture;
    }

    public void ClearCanvas()
    {
        SaveUndoState();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clearColor;
        }
        drawableTexture.SetPixels32(pixels);
        drawableTexture.Apply();
    }

    private void SaveUndoState()
    {
        if (undoHistory.Count >= maxUndoSteps)
        {
            Stack<Color32[]> tempStack = new Stack<Color32[]>();
            while (undoHistory.Count > 1)
            {
                tempStack.Push(undoHistory.Pop());
            }
            undoHistory.Clear();
            while (tempStack.Count > 0)
            {
                undoHistory.Push(tempStack.Pop());
            }
        }

        Color32[] currentPixelsCopy = new Color32[pixels.Length];
        System.Array.Copy(pixels, currentPixelsCopy, pixels.Length);
        undoHistory.Push(currentPixelsCopy);
    }

    public void Undo()
    {
        if (undoHistory.Count <= 1)
        {
            return;
        }

        pixels = undoHistory.Pop();
        drawableTexture.SetPixels32(pixels);
        drawableTexture.Apply();
    }

    // --- タッチ / マウスイベント ---

    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform, eventData.position, uiCamera, out Vector2 localPos))
        {
            SaveUndoState();

            Vector2 pixelPos = LocalToPixelPos(localPos);
            
            DrawBrush(pixelPos, maxBrushSize);
            if (enableSplatter && currentPenColor.a > 0.9f) // 不透明なペン時のみ血飛沫
            {
                GenerateBloodSplatter(pixelPos);
            }

            lastTouchPos = pixelPos;
            isDrawing = true;
            currentMoveSpeed = 0f;
            isFirstDrag = true; // 👈 ここを追加（描き始めフラグをON）

            UpdateHandPositionWithBreath(eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform, eventData.position, uiCamera, out Vector2 localPos))
        {
            Vector2 currentPixelPos = LocalToPixelPos(localPos);

            float distance = Vector2.Distance(lastTouchPos, currentPixelPos);
            if (Time.deltaTime > 0)
            {
                currentMoveSpeed = distance / Time.deltaTime;
            }

           Vector2 shakenPixelPos = currentPixelPos;

            if (isFirstDrag)
            {
                isFirstDrag = false;
            }
            // 速度が 400 を超えたらブレを開始
            else if (distance >= 5f && currentMoveSpeed > 400f)
            {
                // 先ほど調整していただいたいい感じのブレ幅（最大 20f）
                float speedRatio = Mathf.Clamp01((currentMoveSpeed - 400f) / 1400f);
                float maxShakeOffset = 20f * speedRatio;

                Vector2 randomShake = Random.insideUnitCircle * maxShakeOffset;
                shakenPixelPos += randomShake;
            }

            // 線を引き、次回の起点（lastTouchPos）はブレていない本来の位置を保持する
            DrawLine(lastTouchPos, shakenPixelPos);
            lastTouchPos = currentPixelPos; 

            UpdateHandPositionWithBreath(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDrawing && enableSplatter && currentPenColor.a > 0.9f)
        {
            GenerateBloodSplatter(lastTouchPos);
            drawableTexture.SetPixels32(pixels);
            drawableTexture.Apply();
        }

        isDrawing = false;
    }

    private void UpdateHandPosition(Vector2 screenPos)
    {
        UpdateHandPositionWithBreath(screenPos);
    }

    private void UpdateHandPositionWithBreath(Vector2 screenPos)
    {
        if (handUI == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handUI.parent as RectTransform, screenPos, uiCamera, out Vector2 localPoint);

        // 息遣いの揺れオフセットを計算（Updateと同じ数値に合わせる）
        float breathX = Mathf.Sin(idleTimer * 1.2f) * 8f + (Mathf.PingPong(idleTimer * 0.6f, 1f) - 0.5f) * 5f;
        float breathY = Mathf.Cos(idleTimer * 1.8f) * 10f;
        Vector2 breathOffset = new Vector2(breathX, breathY);

        // カーソル位置 + 手のオフセット + 息遣いの揺れ
        handUI.anchoredPosition = localPoint + handOffset + breathOffset;
    }

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

        int activeBrushSize = maxBrushSize;
        if (enableDynamicSize && currentPenColor.a > 0.9f)
        {
            float speedRatio = Mathf.Clamp01(currentMoveSpeed / fastSpeedThreshold);
            activeBrushSize = Mathf.RoundToInt(Mathf.Lerp(maxBrushSize, minBrushSize, speedRatio));
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            DrawBrush(point, activeBrushSize);

            if (enableSplatter && currentPenColor.a > 0.9f && currentMoveSpeed < slowSpeedThreshold && Random.value < splatterChance)
            {
                GenerateBloodSplatter(point);
            }
        }

        drawableTexture.SetPixels32(pixels);
        drawableTexture.Apply();
    }

    private void DrawBrush(Vector2 center, int brushSize)
    {
        int cx = (int)center.x;
        int cy = (int)center.y;
        Color32 drawColor = currentPenColor;

        for (int x = cx - brushSize; x <= cx + brushSize; x++)
        {
            for (int y = cy - brushSize; y <= cy + brushSize; y++)
            {
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                {
                    float distSqr = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (distSqr <= brushSize * brushSize)
                    {
                        pixels[y * textureWidth + x] = drawColor;
                    }
                }
            }
        }
    }

    private void GenerateBloodSplatter(Vector2 center)
    {
        int drops = Random.Range(1, 5);
        Color32 drawColor = currentPenColor;

        for (int d = 0; d < drops; d++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * splatterRadius;
            int dropX = Mathf.Clamp((int)(center.x + randomOffset.x), 0, textureWidth - 1);
            int dropY = Mathf.Clamp((int)(center.y + randomOffset.y), 0, textureHeight - 1);

            int dropSize = Random.Range(1, 4);

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
            color.a = 1f; // 文字の不透明度を100%に強制設定
            currentPenColor = color;
        }
    }

    /// <summary> 消しゴムモード（描いた場所を半透明の白に戻す） </summary>
    public void SetEraserMode() => currentPenColor = clearColor;
}