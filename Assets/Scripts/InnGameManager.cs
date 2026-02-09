using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InnGameManager : MonoBehaviour
{
    private const string NameKeyPrefix = "\u5BA2\u623F\u94A5\u5319"; // ????
    private const string NameKeyRack = "\u94A5\u5319\u67B6_0"; // ???_0
    private const string NameRope = "\u9EBB\u7EF3_0"; // ??_0
    private const string NameSupply = "\u4F9B\u8D27\u7A97\u53E3_0"; // ????_0
    private const string NameDungeon = "\u5730\u7262\u5165\u53E3_0"; // ????_0
    private const string NameDrawerLeftPanel = "\u62BD\u5C49\uFF08\u5DE6\uFF09";
    private const string NameDrawerRightPanel = "\u62BD\u5C49\uFF08\u53F3\uFF09";
    private const string NameMenuPanel = "\u83DC\u5355";

    private const string PathCustomerMale = "Assets/Assets Pack/Character/Common/\u987E\u5BA2\uFF08\u7537\uFF09.png";
    private const string PathCustomerFemale = "Assets/Assets Pack/Character/Common/\u987E\u5BA2\uFF08\u5973\uFF09.png";
    private const string SpriteCustomerMale = "\u987E\u5BA2\uFF08\u7537\uFF09_0";
    private const string SpriteCustomerFemale = "\u987E\u5BA2\uFF08\u5973\uFF09_0";

    [Header("Game Rules")]
    public float gameDuration = 90f;
    public float customerPatience = 12f;
    public float wrongPenaltySeconds = 3f;
    public float timeoutPenaltySeconds = 5f;
    [Range(0f, 1f)] public float banditChance = 0.2f;
    [Range(0f, 1f)] public float mealChance = 0.25f;
    [Range(0, 8)] public int hiddenKeyCount = 3;
    public float drawerSearchCooldown = 2f;

    private float timeRemaining;
    private int score;
    private bool gameRunning;
    private int day = 1;
    private float currentBanditChance;
    private float currentMealChance;
    private float currentPatience;
    private float nextDrawerSearchTime;

    private Camera mainCamera;

    private Vector3 customerSpawnPos = new Vector3(8f, -1f, 0f);
    private Vector3 dungeonPos = new Vector3(-10f, -6f, 0f);

    private readonly List<Drag> draggableItems = new List<Drag>();
    private readonly List<KeyItem> items = new List<KeyItem>();
    private readonly List<KeyItem> hiddenKeys = new List<KeyItem>();
    private Customer currentCustomer;
    private Sprite[] customerSprites;

    private MenuController menuController;

    private Text scoreText;
    private Text timeText;
    private Text dayText;
    private Text requestText;
    private Text messageText;
    private Text helpText;
    private Button serveButton;
    private Button nextDayButton;
    private Coroutine messageRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<InnGameManager>() != null) return;
        GameObject go = new GameObject("InnGameManager");
        go.AddComponent<InnGameManager>();
    }

    void Start()
    {
        LoadCustomerSprites();
        ResolveSceneAnchors();
        SetupItems();
        CreateUI();
        SetupInteractionPanels();
        StartDay();
    }

    void Update()
    {
        if (!gameRunning) return;
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            EndGame();
            return;
        }
        UpdateTimeUI();
    }

    private void ApplyDayDifficulty()
    {
        currentBanditChance = Mathf.Clamp01(banditChance + (day - 1) * 0.05f);
        currentMealChance = Mathf.Clamp01(mealChance + (day - 1) * 0.03f);
        currentPatience = Mathf.Max(6f, customerPatience - (day - 1) * 1f);
    }

    private void ResetItems()
    {
        foreach (KeyItem item in items)
        {
            if (item == null) continue;
            item.gameObject.SetActive(true);
            Drag drag = item.GetComponent<Drag>();
            if (drag != null)
            {
                drag.SetLocked(false);
                drag.ResetToInitialPosition();
            }
        }
    }

    private void HideRandomKeys()
    {
        hiddenKeys.Clear();
        List<KeyItem> keys = new List<KeyItem>();
        foreach (KeyItem item in items)
        {
            if (item != null && item.itemType == ItemType.Key)
            {
                keys.Add(item);
            }
        }

        int hideCount = Mathf.Clamp(hiddenKeyCount, 0, keys.Count);
        for (int i = 0; i < hideCount; i++)
        {
            int index = Random.Range(0, keys.Count);
            KeyItem key = keys[index];
            keys.RemoveAt(index);
            key.gameObject.SetActive(false);
            hiddenKeys.Add(key);
        }
    }

    private void UpdateDayUI()
    {
        if (dayText != null) dayText.text = "Day " + day;
    }

    private void StartDay()
    {
        ApplyDayDifficulty();
        ResetItems();
        HideRandomKeys();
        timeRemaining = gameDuration;
        gameRunning = true;
        UpdateScoreUI();
        UpdateTimeUI();
        UpdateDayUI();
        CloseMenus();
        if (requestText != null)
        {
            requestText.text = "Day " + day + " started";
        }
        if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);
        if (serveButton != null) serveButton.gameObject.SetActive(true);
        SpawnNextCustomer();
    }

    private void EndGame()
    {
        gameRunning = false;
        UpdateTimeUI();
        if (requestText != null)
        {
            requestText.text = "Day " + day + " complete! Score: " + score;
        }
        ShowMessage("Day Over", Color.white, 2f);
        foreach (Drag drag in draggableItems)
        {
            if (drag != null) drag.SetLocked(true);
        }
        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }
        if (nextDayButton != null) nextDayButton.gameObject.SetActive(true);
        if (serveButton != null) serveButton.gameObject.SetActive(false);
    }

    private void ResolveSceneAnchors()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera != null && mainCamera.orthographic)
        {
            float camHeight = mainCamera.orthographicSize;
            float camWidth = camHeight * mainCamera.aspect;
            Vector3 camPos = mainCamera.transform.position;
            customerSpawnPos = new Vector3(camPos.x + camWidth * 0.55f, camPos.y - camHeight * 0.15f, 0f);
        }

        GameObject supply = GameObject.Find(NameSupply);
        if (supply != null)
        {
            customerSpawnPos = supply.transform.position + new Vector3(-3.5f, -2.5f, 0f);
        }

        GameObject dungeon = GameObject.Find(NameDungeon);
        if (dungeon != null)
        {
            dungeonPos = dungeon.transform.position;
        }

        customerSpawnPos = ClampToCamera(customerSpawnPos, new Vector2(1.2f, 1.2f));
    }

    private Vector3 ClampToCamera(Vector3 position, Vector2 padding)
    {
        if (mainCamera == null || !mainCamera.orthographic) return position;
        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        Vector3 camPos = mainCamera.transform.position;
        float minX = camPos.x - camWidth + padding.x;
        float maxX = camPos.x + camWidth - padding.x;
        float minY = camPos.y - camHeight + padding.y;
        float maxY = camPos.y + camHeight - padding.y;
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY),
            position.z
        );
    }

    private void SetupItems()
    {
        draggableItems.Clear();
        items.Clear();

        for (int i = 1; i <= 8; i++)
        {
            string name = NameKeyPrefix + i + "_0";
            GameObject key = GameObject.Find(name);
            if (key == null) continue;
            KeyItem item = key.GetComponent<KeyItem>();
            if (item == null) item = key.AddComponent<KeyItem>();
            item.itemType = ItemType.Key;
            item.keyId = i;
            items.Add(item);
            Drag drag = key.GetComponent<Drag>();
            if (drag != null) draggableItems.Add(drag);
        }

        GameObject rope = GameObject.Find(NameRope);
        if (rope != null)
        {
            KeyItem item = rope.GetComponent<KeyItem>();
            if (item == null) item = rope.AddComponent<KeyItem>();
            item.itemType = ItemType.Rope;
            item.keyId = -1;
            items.Add(item);
            Drag drag = rope.GetComponent<Drag>();
            if (drag != null) draggableItems.Add(drag);
        }

        foreach (Drag drag in draggableItems)
        {
            if (drag != null) drag.SetLocked(false);
        }
    }

    private void SpawnNextCustomer()
    {
        if (!gameRunning || currentCustomer != null) return;
        if (customerSprites == null || customerSprites.Length == 0)
        {
            ShowMessage("Missing customer sprites", Color.red, 2f);
            return;
        }

        RequestType requestType = RollRequestType();
        int requestedKeyId = requestType == RequestType.Key ? Random.Range(1, 9) : -1;

        GameObject go = new GameObject(requestType == RequestType.Bandit ? "BanditCustomer" : "Customer");
        go.transform.position = customerSpawnPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = customerSprites[Random.Range(0, customerSprites.Length)];
        sr.sortingOrder = 4;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        if (sr.sprite != null)
        {
            collider.size = sr.sprite.bounds.size;
            collider.offset = sr.sprite.bounds.center;
        }
        collider.isTrigger = true;

        Customer customer = go.AddComponent<Customer>();
        customer.Init(this, requestType, requestedKeyId, currentPatience);
        currentCustomer = customer;

        if (requestText != null)
        {
            requestText.text = requestType == RequestType.Bandit
                ? "Suspicious guest! Use the rope."
                : (requestType == RequestType.Meal
                    ? "Guest wants a meal. Open the menu."
                    : "Guest wants room key #" + requestedKeyId);
        }
    }

    private RequestType RollRequestType()
    {
        float roll = Random.value;
        if (roll < currentBanditChance) return RequestType.Bandit;
        if (roll < currentBanditChance + currentMealChance) return RequestType.Meal;
        return RequestType.Key;
    }

    private IEnumerator SpawnNextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNextCustomer();
    }

    public void OnCustomerServed(Customer customer, KeyItem item)
    {
        if (!gameRunning || customer == null) return;
        if (currentCustomer == customer) currentCustomer = null;

        Drag drag = item != null ? item.GetComponent<Drag>() : null;
        if (drag != null) drag.ResetToInitialPosition();

        int delta = customer.requestType == RequestType.Bandit ? 2 : 1;
        score += delta;
        UpdateScoreUI();

        if (customer.requestType == RequestType.Bandit)
        {
            ShowMessage("Bandit caught! +" + delta, Color.cyan, 1.2f);
            StartCoroutine(MoveToDungeonAndDestroy(customer.transform, 0.6f));
        }
        else
        {
            string msg = customer.requestType == RequestType.Meal ? "Meal served! +" : "Checked in! +";
            ShowMessage(msg + delta, Color.green, 1.2f);
            Destroy(customer.gameObject);
        }

        StartCoroutine(SpawnNextAfterDelay(0.4f));
    }

    public void OnCustomerWrong(Customer customer, KeyItem item)
    {
        if (!gameRunning) return;
        timeRemaining = Mathf.Max(0f, timeRemaining - wrongPenaltySeconds);
        UpdateTimeUI();
        ShowMessage("Wrong item! -" + wrongPenaltySeconds + "s", Color.red, 1.2f);
    }

    public void OnCustomerTimeout(Customer customer)
    {
        if (!gameRunning || customer == null) return;
        if (currentCustomer == customer) currentCustomer = null;
        timeRemaining = Mathf.Max(0f, timeRemaining - timeoutPenaltySeconds);
        UpdateTimeUI();
        ShowMessage("Guest left! -" + timeoutPenaltySeconds + "s", Color.yellow, 1.2f);
        Destroy(customer.gameObject);
        StartCoroutine(SpawnNextAfterDelay(0.4f));
    }

    private IEnumerator MoveToDungeonAndDestroy(Transform target, float duration)
    {
        if (target == null) yield break;
        Vector3 start = target.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, dungeonPos, t);
            yield return null;
        }
        Destroy(target.gameObject);
    }

    private void CreateUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        GameObject root = new GameObject("InnUI");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        scoreText = CreateText(root.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), TextAnchor.UpperLeft, 20);
        timeText = CreateText(root.transform, "TimeText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), TextAnchor.UpperRight, 20);
        dayText = CreateText(root.transform, "DayText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.UpperCenter, 18);
        requestText = CreateText(root.transform, "RequestText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), TextAnchor.UpperCenter, 22);
        messageText = CreateText(root.transform, "MessageText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), TextAnchor.MiddleCenter, 28);
        if (messageText != null) messageText.text = string.Empty;

        helpText = CreateText(root.transform, "HelpText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), TextAnchor.LowerLeft, 16);
        if (helpText != null)
        {
            RectTransform helpRt = helpText.GetComponent<RectTransform>();
            if (helpRt != null) helpRt.sizeDelta = new Vector2(520f, 120f);
            helpText.text = "Drag keys/rope to guests.\nClick drawers to search keys.\nClick menu to serve meals.";
        }

        serveButton = CreateButton(root.transform, "ServeButton", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 40f), new Vector2(180f, 52f), "\u63A5\u5F85", OnServeButtonPressed);
        nextDayButton = CreateButton(root.transform, "NextDayButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(200f, 52f), "\u4E0B\u4E00\u5929", OnNextDayPressed);
        if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);
    }

    private Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, TextAnchor alignment, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(500f, 60f);

        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(onClick);

        Text text = CreateText(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 22);
        if (text != null)
        {
            text.text = label;
            text.color = Color.white;
        }
        return button;
    }

    private void SetupInteractionPanels()
    {
        menuController = FindObjectOfType<MenuController>();
        if (menuController == null) return;

        if (menuController.backgroundMask != null)
        {
            AddCloseHandler(menuController.backgroundMask);
        }

        if (menuController.allMenus == null) return;
        foreach (GameObject menu in menuController.allMenus)
        {
            if (menu == null) continue;
            if (menu.name == NameDrawerLeftPanel || menu.name == NameDrawerRightPanel)
            {
                SetupDrawerPanel(menu);
            }
            else if (menu.name == NameMenuPanel)
            {
                SetupMenuPanel(menu);
            }
        }
    }

    private void AddCloseHandler(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(CloseMenus);
    }

    private void SetupDrawerPanel(GameObject panel)
    {
        if (panel == null) return;
        CreateButton(panel.transform, "SearchButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(160f, 44f), "\u641C\u7D22", OnDrawerSearchPressed);
        CreateButton(panel.transform, "CloseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(70f, 40f), "X", CloseMenus);
    }

    private void SetupMenuPanel(GameObject panel)
    {
        if (panel == null) return;
        CreateButton(panel.transform, "ServeMealButton", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(180f, 44f), "\u4E0A\u83DC", OnMenuServePressed);
        CreateButton(panel.transform, "CloseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(70f, 40f), "X", CloseMenus);
    }

    private void OnDrawerSearchPressed()
    {
        if (!gameRunning)
        {
            ShowMessage("Not running", Color.yellow, 1.0f);
            return;
        }

        if (Time.time < nextDrawerSearchTime)
        {
            ShowMessage("Please wait", Color.yellow, 0.8f);
            return;
        }

        nextDrawerSearchTime = Time.time + drawerSearchCooldown;

        if (hiddenKeys.Count == 0)
        {
            ShowMessage("No keys left", Color.yellow, 1.0f);
            return;
        }

        int index = Random.Range(0, hiddenKeys.Count);
        KeyItem key = hiddenKeys[index];
        hiddenKeys.RemoveAt(index);
        key.gameObject.SetActive(true);
        Drag drag = key.GetComponent<Drag>();
        if (drag != null) drag.ResetToInitialPosition();

        ShowMessage("Found key #" + key.keyId, Color.green, 1.2f);
        CloseMenus();
    }

    private void OnMenuServePressed()
    {
        if (!gameRunning)
        {
            ShowMessage("Not running", Color.yellow, 1.0f);
            return;
        }

        if (currentCustomer == null)
        {
            ShowMessage("No guest", Color.yellow, 1.0f);
            return;
        }

        if (currentCustomer.requestType != RequestType.Meal)
        {
            ShowMessage("No meal requested", Color.yellow, 1.0f);
            return;
        }

        OnCustomerServed(currentCustomer, null);
        CloseMenus();
    }

    private void CloseMenus()
    {
        if (menuController != null) menuController.HideMenu();
    }

    private void OnNextDayPressed()
    {
        if (gameRunning) return;
        day += 1;
        StartDay();
    }

    private void OnServeButtonPressed()
    {
        if (!gameRunning)
        {
            ShowMessage("Not running", Color.yellow, 1.0f);
            return;
        }

        if (currentCustomer == null)
        {
            ShowMessage("No guest", Color.yellow, 1.0f);
            return;
        }

        KeyItem item = null;
        if (currentCustomer.requestType == RequestType.Meal)
        {
            OnCustomerServed(currentCustomer, null);
            return;
        }

        if (currentCustomer.requestType == RequestType.Bandit)
        {
            item = FindItem(ItemType.Rope, -1);
        }
        else
        {
            item = FindItem(ItemType.Key, currentCustomer.requestedKeyId);
        }

        if (item == null)
        {
            ShowMessage("Item missing", Color.red, 1.2f);
            return;
        }

        OnCustomerServed(currentCustomer, item);
    }

    private KeyItem FindItem(ItemType type, int keyId)
    {
        foreach (KeyItem item in items)
        {
            if (item == null) continue;
            if (!item.gameObject.activeInHierarchy) continue;
            if (item.itemType != type) continue;
            if (type == ItemType.Key && item.keyId != keyId) continue;
            return item;
        }
        return null;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    private void UpdateTimeUI()
    {
        if (timeText != null) timeText.text = "Time: " + Mathf.CeilToInt(timeRemaining);
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine(message, color, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, Color color, float duration)
    {
        messageText.text = message;
        messageText.color = color;
        yield return new WaitForSeconds(duration);
        messageText.text = string.Empty;
    }

    private void LoadCustomerSprites()
    {
        List<Sprite> sprites = new List<Sprite>();

#if UNITY_EDITOR
        Sprite male = LoadSpriteFromPath(PathCustomerMale, SpriteCustomerMale);
        Sprite female = LoadSpriteFromPath(PathCustomerFemale, SpriteCustomerFemale);
        if (male != null) sprites.Add(male);
        if (female != null) sprites.Add(female);
#endif

        if (sprites.Count == 0)
        {
            Sprite[] resources = Resources.LoadAll<Sprite>("Customers");
            if (resources != null && resources.Length > 0)
            {
                sprites.AddRange(resources);
            }
        }

        customerSprites = sprites.ToArray();
        if (customerSprites.Length == 0)
        {
            Debug.LogError("InnGameManager: Could not load customer sprites. Check Paths or add sprites to Resources/Customers.");
        }
    }

#if UNITY_EDITOR
    private Sprite LoadSpriteFromPath(string assetPath, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }
        return null;
    }
#endif
}
