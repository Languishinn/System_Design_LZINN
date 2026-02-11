using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    // 单例模式，方便任何顾客都能直接找到这个唯一的固定对话框
    public static DialogueUI Instance;

    public GameObject panel; // 对话框的背景图物体
    public TextMeshProUGUI textDisplay;

    void Awake() {
        Instance = this;
        panel.SetActive(false); // 初始隐藏
    }

    // 提供一个简单的接口供顾客调用
    public void ShowDemand() {
        panel.SetActive(true);
        panel.transform.localScale = Vector3.zero;
        panel.transform.localScale = Vector3.one; 
    }

    public void Hide() {
        panel.SetActive(false);
    }
}
