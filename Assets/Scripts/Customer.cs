using UnityEngine;

public enum RequestType
{
    Key,
    Meal,
    Bandit
}

[RequireComponent(typeof(BoxCollider2D))]
public class Customer : MonoBehaviour
{
    public RequestType requestType;
    public int requestedKeyId;
    public float patience = 12f;

    private InnGameManager manager;
    private float remaining;
    private bool resolved;

    public void Init(InnGameManager owner, RequestType type, int keyId, float patienceSeconds)
    {
        manager = owner;
        requestType = type;
        requestedKeyId = keyId;
        patience = Mathf.Max(1f, patienceSeconds);
        remaining = patience;
    }

    void Update()
    {
        if (resolved || manager == null) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            resolved = true;
            manager.OnCustomerTimeout(this);
        }
    }

    public bool TryServe(Drag drag)
    {
        if (resolved || manager == null || drag == null) return false;
        KeyItem item = drag.GetComponent<KeyItem>();
        if (item == null) return false;

        if (requestType == RequestType.Meal)
        {
            manager.OnCustomerWrong(this, item);
            return false;
        }

        bool correct = requestType == RequestType.Bandit
            ? item.itemType == ItemType.Rope
            : (item.itemType == ItemType.Key && item.keyId == requestedKeyId);

        if (correct)
        {
            resolved = true;
            manager.OnCustomerServed(this, item);
            return true;
        }

        manager.OnCustomerWrong(this, item);
        return false;
    }
}
