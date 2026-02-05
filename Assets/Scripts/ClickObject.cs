

using UnityEngine;
using UnityEngine.EventSystems;

// 继承 IPointerClickHandler 接口
public class ClickObject : MonoBehaviour, IPointerClickHandler
{
    public MenuController menuController;
    public GameObject TargetInterface;
    public void OnPointerClick(PointerEventData eventData)
    {
        // eventData.button 可以判断是左键还是右键
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log(gameObject.name + " clicked");
            // 在这里写打开菜单的代码
            ActiveInterface();
        }
    }

    void ActiveInterface()
    {
        if (menuController != null)
        {
            menuController.ShowTargetMenu(TargetInterface);
            Debug.Log( TargetInterface+ "active");
        }
          
    }
}