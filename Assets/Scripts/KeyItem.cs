using UnityEngine;

public enum ItemType
{
    Key,
    Rope
}

public class KeyItem : MonoBehaviour
{
    public ItemType itemType = ItemType.Key;
    public int keyId = -1;
}
