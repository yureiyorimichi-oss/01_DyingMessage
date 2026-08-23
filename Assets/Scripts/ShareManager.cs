using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class ShareManager : MonoBehaviour
{
    [Header("キャプチャ設定")]
    [SerializeField] private RectTransform captureArea;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Camera uiCamera;

    [Header("モーダルUI設定")]
    [SerializeField] private GameObject shareModalPanel; // 作成した ShareModalPanel
    [SerializeField] private RawImage previewRawImage;  // 作成した PreviewRawImage
    [SerializeField] private Button closeButton;        // 作成した CloseButton
    [SerializeField] private Button xShareButton;       // 作成した XShareButton

    private Texture2D lastCapturedTexture;

private void Start()
    {
        // ボタンに処理を割り当て
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShareModal);

        if (xShareButton != null)
            xShareButton.onClick.AddListener(OnXShareButtonClicked);

        // 開始時はモーダルを隠す
        if (shareModalPanel != null)
            shareModalPanel.SetActive(false);
    }
    
    public void OnShareButtonClicked()
    {
        StartCoroutine(CaptureAreaAndShare());
    }

    private IEnumerator CaptureAreaAndShare()
    {
        // 1. UIの描画完了を待つ
        yield return new WaitForEndOfFrame();

        // CanvasのRenderModeに応じてカメラを設定（Overlayの場合はnullが必要）
        Camera targetCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            targetCamera = uiCamera != null ? uiCamera : Camera.main;
        }

        // 2. RectTransformの画面上のピクセル位置・サイズを取得
        Vector3[] corners = new Vector3[4];
        captureArea.GetWorldCorners(corners);

        Vector2 minScreenPos = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[0]);
        Vector2 maxScreenPos = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[2]);

        int startX = Mathf.RoundToInt(minScreenPos.x);
        int startY = Mathf.RoundToInt(minScreenPos.y);
        int width  = Mathf.RoundToInt(maxScreenPos.x - minScreenPos.x);
        int height = Mathf.RoundToInt(maxScreenPos.y - minScreenPos.y);

        // 画面外はみ出しの安全計算
        int screenW = Screen.width;
        int screenH = Screen.height;

        int safeStartX = Mathf.Clamp(startX, 0, screenW - 1);
        int safeStartY = Mathf.Clamp(startY, 0, screenH - 1);
        int safeWidth  = Mathf.Clamp(width,  1, screenW - safeStartX);
        int safeHeight = Mathf.Clamp(height, 1, screenH - safeStartY);

        // 3. テクスチャの読み込み
        Texture2D croppedTexture = new Texture2D(safeWidth, safeHeight, TextureFormat.RGB24, false);
        croppedTexture.ReadPixels(new Rect(safeStartX, safeStartY, safeWidth, safeHeight), 0, 0);
        croppedTexture.Apply();

       // 前回のテクスチャが残っていれば破棄
        if (lastCapturedTexture != null) Destroy(lastCapturedTexture);

        lastCapturedTexture = croppedTexture;

        // RawImageに画像を反映
        if (previewRawImage != null)
        {
            previewRawImage.texture = lastCapturedTexture;
        }

        // Unity上のモーダルパネルを表示
        if (shareModalPanel != null)
        {
            shareModalPanel.SetActive(true);
        }
    } // ← この閉じ括弧を追加してください！

    // X（旧Twitter）でポストするボタンが押されたとき
    private void OnXShareButtonClicked()
    {
        // TopicManagerから現在選ばれているお題を取得
        TopicManager topicManager = FindObjectOfType<TopicManager>();
        string topicText = topicManager != null ? topicManager.GetCurrentTopic() : "";

        // ハッシュタグ用に記号やスペースを自動整形（例: "これだけは伝えたい…" ➔ "これだけは伝えたい"）
        string formattedTopic = topicText.Replace("…", "").Replace(" ", "").Replace(" ", "");

        // ポスト本文とハッシュタグの生成
        string tweetText = "ダイイングメッセージを作成しました！";
        string hashtags = "ダイイングメッセージメーカー";

        if (!string.IsNullOrEmpty(formattedTopic))
        {
            hashtags += $",{formattedTopic}";
        }

        string encodedText = UnityEngine.Networking.UnityWebRequest.EscapeURL(tweetText);
        string encodedHashtags = UnityEngine.Networking.UnityWebRequest.EscapeURL(hashtags);

        string shareUrl = $"https://x.com/intent/post?text={encodedText}&hashtags={encodedHashtags}";

        // ブラウザでXの投稿画面を開く
        Application.OpenURL(shareUrl);
    }

    // 閉じるボタンが押されたとき
    public void CloseShareModal()
    {
        if (shareModalPanel != null)
        {
            shareModalPanel.SetActive(false);
        }
    }
}