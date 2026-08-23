using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LongTextLoader : MonoBehaviour
{
    [Header("読み込むテキストファイル (.txt)")]
    [SerializeField] private TextAsset textFile;

    [Header("使用するTMPフォント")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float fontSize = 24f;

    [Header("1ブロックあたりの文字数制限 (頂点切れ防止)")]
    [SerializeField] private int maxCharsPerBlock = 600;

    private void Start()
    {
     LoadAndGenerateText();
    }
    
    public void LoadAndGenerateText()
    {
        if (textFile == null)
        {
            Debug.LogError("TextAssetがセットされていません。");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        string fullText = textFile.text;
        int length = fullText.Length;

        // 長文を自動分割してUI要素を生成
        for (int i = 0; i < length; i += maxCharsPerBlock)
        {
            int chunkSize = Mathf.Min(maxCharsPerBlock, length - i);
            string chunk = fullText.Substring(i, chunkSize);

            CreateTextSegment(chunk);
        }

        // レイアウトを強制再計算してスクロール可動域を自動調整
        Canvas.ForceUpdateCanvases();
        if (TryGetComponent<ContentSizeFitter>(out var fitter))
        {
            fitter.SetLayoutVertical();
        }
    }

    private void CreateTextSegment(string textContent)
    {
        GameObject textObj = new GameObject("TextSegment", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
        textObj.transform.SetParent(transform, false);

        // RectTransform 設定
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 1f);

        // TextMeshPro 設定
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.fontSize = fontSize;
        tmp.text = textContent;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        // Auto Height 設定
        ContentSizeFitter csf = textObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ModalControllerからテキストを差し替えて再読み込みするための処理
    public void SetTextAssetAndLoad(TextAsset newTextAsset)
    {
        textFile = newTextAsset;
        LoadAndGenerateText();
    }
}