using UnityEngine;
using UnityEngine.EventSystems; // 必须引用

// 继承这些接口，让 Sprite 像按钮一样工作
public class ClickObject : MonoBehaviour, IPointerClickHandler//,IBeginDragHandler, IDragHandler, IEndDragHandler 
{
    
    public enum ObjectType { Bell, Key, Menu, MenuFood}
    public ObjectType type;

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log(gameObject.name + " clicked");
            switch (type) {
                case ObjectType.Bell:
                    Debug.Log("这是铃铛，呼叫客人...");
                    InnManager.Instance.OnBellRung();
                    break;
                case ObjectType.Key:
                    Debug.Log("这是钥匙，准备拖拽...");
                    break;
                case ObjectType.MenuFood:
                    Debug.Log("这是菜单上的菜...");
                    break;
                case ObjectType.Menu:
                    Debug.Log("clicked the menu");
                    MenuClicked mc = GetComponent<MenuClicked>();
                    if (mc != null) {
                        mc.ActiveInterface();
                    } else {
                        Debug.LogWarning(gameObject.name + " 上没挂 MenuClicked 脚本！");
                    }
                    break;
            }
        }
    }
}