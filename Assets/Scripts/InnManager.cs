using UnityEngine;

public class InnManager : MonoBehaviour {
    public static InnManager Instance;

    public Transform spawnPoint;      // 客人出现的位置
    public Transform counterPoint;    // 客人走到柜台的位置
    public GameObject guestPrefab;    // 客人的预制体
    
    [Header("State")]
    public bool isCounterOccupied = false; // 柜台是否被占用

    private void Awake() {
        Instance = this;
        isCounterOccupied = false;
    }

    // 铃铛点击后调用的方法
    public void OnBellRung() {
        if (isCounterOccupied) {
            Debug.Log("柜台正忙，请稍后...");
            return;
        }

        SpawnNextGuest();
    }

    void SpawnNextGuest() {
        isCounterOccupied = true;
        GameObject newGuest = Instantiate(guestPrefab, spawnPoint.position, Quaternion.identity);
        
        // 让客人走向柜台
        GuestController controller = newGuest.GetComponent<GuestController>();
        controller.MoveTo(counterPoint.position);
    }

    // 当客人离开时调用（住宿或吃饭完成后）
    public void ClearCounter() {
        isCounterOccupied = false;
        Debug.Log("柜台已空闲，可以接待下一个客人。");
    }
}