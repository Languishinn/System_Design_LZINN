using UnityEngine;

public class GuestController : MonoBehaviour {
    public float moveSpeed = 4f;
    private Vector3 targetPos;
    private bool isMoving = false;

    public void MoveTo(Vector3 position) {
        targetPos = position;
        isMoving = true;
    }

    void Update() {
        if (isMoving) {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.01f) {
                isMoving = false;
                Speak();
            }
        }
    }

    void Speak() {
        DialogueUI.Instance.ShowDemand();
    }

    // 模拟离开
    public void Leave() {
        // 逻辑完成后调用
        DialogueUI.Instance.ShowDemand();
        InnManager.Instance.ClearCounter();
        Destroy(gameObject);
    }
}