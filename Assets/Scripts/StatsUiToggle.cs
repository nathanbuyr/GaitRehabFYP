using UnityEngine;
using UnityEngine.UI;

public class StatsUiToggle : MonoBehaviour
{
    [Header("References")]
    public WaypointSystemManager waypointManager;

    [Header("UI Layout")]
    public float uiDistance = 1.6f;
    public Vector3 uiOffset = new Vector3(0.35f, -0.25f, 0f); // bottom-right of view
    public Vector2 panelSize = new Vector2(220f, 120f);
    public Vector2 buttonSize = new Vector2(140f, 45f);
    public float followSpeed = 8f;
    public float rotateSpeed = 10f;

    private GameObject canvasObj;
    private GameObject panelObj;
    private Text panelText;
    private Button toggleButton;
    private Transform cameraTransform;

    void Start()
    {
        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointSystemManager>();
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        CreateUi();
    }

    void CreateUi()
    {
        // Canvas
        canvasObj = new GameObject("StatsUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Place UI in front of camera (world space) and smooth follow in LateUpdate
        if (cameraTransform != null)
        {
            canvasObj.transform.position = GetTargetPosition();
            canvasObj.transform.rotation = GetTargetRotation();
            canvasObj.transform.localScale = Vector3.one * 0.0015f; // scale for world-space UI
        }

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 300);

        // Panel
        panelObj = new GameObject("StatsPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = new Vector2(0f, 60f);

        // Panel text
        GameObject textObj = new GameObject("StatsText");
        textObj.transform.SetParent(panelObj.transform, false);
        panelText = textObj.AddComponent<Text>();
        panelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        panelText.fontSize = 32;
        panelText.color = Color.white;
        panelText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = panelText.GetComponent<RectTransform>();
        textRect.sizeDelta = panelSize;
        textRect.anchoredPosition = Vector2.zero;

        // Toggle button
        GameObject buttonObj = new GameObject("StatsToggleButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0.6f, 0.1f, 0.9f);
        toggleButton = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;
        buttonRect.anchoredPosition = new Vector2(0f, -40f);

        // Button label
        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        btnText.fontSize = 28;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.text = "Stats";
        RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
        btnTextRect.sizeDelta = buttonSize;
        btnTextRect.anchoredPosition = Vector2.zero;

        toggleButton.onClick.AddListener(TogglePanel);

        // Start hidden
        panelObj.SetActive(false);
    }

    void Update()
    {
        if (panelObj == null || panelText == null)
            return;

        if (panelObj.activeSelf && waypointManager != null)
        {
            panelText.text = waypointManager.GetStatsText();
        }
    }

    void LateUpdate()
    {
        if (canvasObj == null || cameraTransform == null)
            return;

        Vector3 targetPos = GetTargetPosition();
        Quaternion targetRot = GetTargetRotation();

        canvasObj.transform.position = Vector3.Lerp(canvasObj.transform.position, targetPos, Time.deltaTime * followSpeed);
        canvasObj.transform.rotation = Quaternion.Slerp(canvasObj.transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
    }

    Vector3 GetTargetPosition()
    {
        return cameraTransform.position
               + cameraTransform.forward * uiDistance
               + cameraTransform.right * uiOffset.x
               + cameraTransform.up * uiOffset.y
               + cameraTransform.forward * uiOffset.z;
    }

    Quaternion GetTargetRotation()
    {
        Vector3 toCamera = cameraTransform.position - canvasObj.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.001f)
        {
            toCamera = -cameraTransform.forward;
        }
        return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    void TogglePanel()
    {
        if (panelObj == null)
            return;

        panelObj.SetActive(!panelObj.activeSelf);
    }
}
