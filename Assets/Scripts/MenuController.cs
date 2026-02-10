using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuController : MonoBehaviour
{
    public GameObject[] allMenus;
    public GameObject backgroundMask;
    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        HideMenu(); 
    }

public void ShowTargetMenu(GameObject target)
{
    Debug.Log("准备显示菜单: " + target.name);
    if (target == null) return;

    foreach (GameObject menu in allMenus)
    {
        if (menu != null) menu.SetActive(false);
    }

    target.SetActive(true);

    if (backgroundMask != null)
    {
        backgroundMask.SetActive(true);
    }
    else
    {
        Debug.LogError("警告：backgroundMask 引用是空的！请检查 Inspector");
    }

    cg.alpha = 1;
    cg.interactable = true;
    cg.blocksRaycasts = true;
}

    public void HideMenu()
    {
        // 1. 关闭总控制
        if (cg != null) // 保险判断
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        
        // 2. 彻底关闭子物体
        if (backgroundMask != null) backgroundMask.SetActive(false); // 这一行补上

        foreach (GameObject menu in allMenus)
        {
            if (menu != null) menu.SetActive(false);
        }
    }
}