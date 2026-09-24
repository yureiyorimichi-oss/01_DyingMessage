using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TopicManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI topicText;

    [Header("デフォルトお題")]
    [SerializeField] private string defaultTopic = "これだけは伝えたい…";

    [Header("お題リスト（切り替え用）")]
    [SerializeField] private List<string> topicList = new List<string>()
    {
        "犯人は…",
        "昨日の夕飯は…",
        "突然の告白…",
        "最期の言い残し…",
        "実は私…",
        "来世は…",
        "好きな○○は…",
        "黒幕の正体は…"
    };

    private string currentTopic = "";

    private void Start()
    {
        // 起動時は必ずデフォルトお題を設定
        currentTopic = defaultTopic;
        UpdateUI();
    }

    /// <summary>
    /// ランダムにお題を切り替える（ボタンから呼び出す）
    /// </summary>
    public void ChangeRandomTopic()
    {
        if (topicList == null || topicList.Count == 0) return;

        string nextTopic = currentTopic;
        while (nextTopic == currentTopic && topicList.Count > 1)
        {
            int randomIndex = Random.Range(0, topicList.Count);
            nextTopic = topicList[randomIndex];
        }

        currentTopic = nextTopic;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (topicText != null)
        {
            topicText.text = currentTopic; // 「お題：」なしで表示
        }
    }

    /// <summary>
    /// 現在のお題を取得
    /// </summary>
    public string GetCurrentTopic()
    {
        return currentTopic;
    }
}