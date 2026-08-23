using UnityEngine;
using TMPro;

public class ModalController : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private LongTextLoader textLoader;

    [Header("テキストデータ (.txt)")]
    [SerializeField] private TextAsset privacyPolicyText;
    [SerializeField] private TextAsset termsOfServiceText;

    public void OpenPrivacyPolicy()
    {
        if (titleText != null) titleText.text = "プライバシーポリシー";
        SetAndLoadText(privacyPolicyText);
    }

    public void OpenTermsOfService()
    {
        if (titleText != null) titleText.text = "利用規約";
        SetAndLoadText(termsOfServiceText);
    }

    private void SetAndLoadText(TextAsset textAsset)
    {
        // 先にモーダルパネルを表示させる
        if (modalPanel != null) modalPanel.SetActive(true);

        // その後にテキストをセットして読み込む
        if (textLoader != null)
        {
            textLoader.SetTextAssetAndLoad(textAsset);
        }
    }

    public void CloseModal()
    {
        if (modalPanel != null) modalPanel.SetActive(false);
    }
}