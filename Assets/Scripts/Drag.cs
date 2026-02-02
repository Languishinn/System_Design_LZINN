using UnityEngine;
using UnityEngine.EventSystems; // 必须引用

public class Drag: MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 offset;

    // 当鼠标/手指按下开始拖拽时
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 计算物体中心到点击点的偏移量，防止物体中心“瞬移”到鼠标位置
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        mousePos.z = 0; 
        offset = transform.position - mousePos;
        
        // 可以在这里改变透明度或缩放，给予视觉反馈
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.7f);
    }

    // 拖拽进行中
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        mousePos.z = 0;
        // 更新位置
        transform.position = mousePos + offset;
    }

    // 拖拽结束
    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复视觉效果
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}