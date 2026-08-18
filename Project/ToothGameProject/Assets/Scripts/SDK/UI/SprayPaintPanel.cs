using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static YouDooSDKConstants;
using Input = InputSystemCompat;

public class SprayPaintPanel : MainScene
{
    [FormerlySerializedAs("gyroDataTestDemo")]
    [SerializeField] GyroDataReader gyroDataReader;
    [Tooltip("The cursor RectTransform to track")]
    public RectTransform targetCursor;

    [Tooltip("The Canvas or Panel where paint will be instantiated")]
    public RectTransform paintContainer;

    [Tooltip("The sprite to use for painting. Should be a circle.")]
    public Sprite paintSprite;

    [Tooltip("Color of the spray trail")]
    public Color paintColor = Color.red;

    [Tooltip("Time between paint dots (smaller = smoother but more objects)")]
    public float spawnInterval = 0.02f;

    [Header("Spray Effect")]
    [Tooltip("Random offset radius for spray effect")]
    public float scatterRadius = 5.0f;

    [Tooltip("Random scale variation")]
    public float scaleVariation = 0.2f;

    [SerializeField]
    private Button buttonSprayPaint; //2:清理

    [Header("Cursor Speed Control")]
    public float[] speedLevels = new float[] { 15.0f, 20.0f, 25.0f, 30.0f, 35.0f };
    public int currentSpeedLevel = 2; // 默认为第3挡 (0.05f)

    [Header("Shader Settings")]
    [Range(1f, 500f)] public float noiseScale = 100.0f;
    [Range(0f, 1f)] public float density = 0.5f;
    [Range(0.1f, 5.0f)] public float falloff = 2.0f;

    private float timer = 0f;
    private Sprite defaultSprite;
    private Material runtimeMaterial;

    private Text speedLevelText;

    private void EnsureSpeedLevelTextExists()
    {
        if (speedLevelText == null)
        {
            GameObject textObj = new GameObject("SpeedLevelText");
            textObj.transform.SetParent(this.transform, false);

            speedLevelText = textObj.AddComponent<Text>();
            speedLevelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            speedLevelText.fontSize = 36;
            speedLevelText.color = Color.white;
            speedLevelText.alignment = TextAnchor.UpperRight;
            speedLevelText.raycastTarget = false; // 不阻挡点击

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-30, -30);
            rect.sizeDelta = new Vector2(400, 60);

            textObj.transform.SetAsLastSibling();
        }
        else
        {
            speedLevelText.transform.SetAsLastSibling();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(true, false, false, false, false, (int)FilterType.NONE);
        ClearCanvas();
        ApplySpeedLevel();
        gyroDataReader.ResetGyroMappingState();
    }

    protected override void OnLeftArrowPressed()
    {
        base.OnLeftArrowPressed();
        if (currentSpeedLevel > 0)
        {
            currentSpeedLevel--;
            ApplySpeedLevel();
        }
    }

    protected override void OnRightArrowPressed()
    {
        base.OnRightArrowPressed();
        if (currentSpeedLevel < speedLevels.Length - 1)
        {
            currentSpeedLevel++;
            ApplySpeedLevel();
        }
    }
    protected override void OnButtonOKPressed()
    {
        gyroDataReader.ResetGyroMappingState();
    }


    private void ApplySpeedLevel()
    {
        if (gyroDataReader != null)
        {
            var gyroItem = gyroDataReader.GetTargetHardWareGyroItem();
            var p = gyroItem.GyroParams;
            p.HSensitivity = speedLevels[currentSpeedLevel];
            p.VSensitivity = speedLevels[currentSpeedLevel];
            gyroItem.GyroParams = p;
            gyroItem.SetGyroFilterParams();

            Debug.Log($"[SprayPaintPanel] Cursor speed level changed to {currentSpeedLevel + 1} (Sensitivity: {speedLevels[currentSpeedLevel]})");
        }

        EnsureSpeedLevelTextExists();
        string[] levelNames = { "一", "二", "三", "四", "五" };
        if (currentSpeedLevel >= 0 && currentSpeedLevel < levelNames.Length)
        {
            speedLevelText.text = $"当前陀螺仪速度为：{levelNames[currentSpeedLevel]}级";
        }
    }

    void Start()
    {
        // 绑定按钮事件
        if (buttonSprayPaint != null)
        {
            buttonSprayPaint.onClick.AddListener(() => { ClearCanvas(); Debug.Log("SprayPaint: Canvas Cleared"); });
        }

        // Load default sprite if needed
        if (paintSprite == null)
        {
            defaultSprite = Resources.Load<Sprite>("Image/CircleBrush");
            if (defaultSprite == null)
            {
                // Fallback to SightBead if CircleBrush not found
                defaultSprite = Resources.Load<Sprite>("Image/SightBead");
            }
        }

        // Setup Material
        Shader shader = Shader.Find("UI/SprayPaint");
        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
        }
        else
        {
            Debug.LogWarning("SprayPaint: Could not find shader 'UI/SprayPaint'");
        }
    }

    void Update()
    {
        // Update material properties in real-time for tuning
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat("_NoiseScale", noiseScale);
            runtimeMaterial.SetFloat("_Density", density);
            runtimeMaterial.SetFloat("_Falloff", falloff);
        }

        if (targetCursor == null || paintContainer == null) return;

        targetCursor.transform.localPosition = gyroDataReader.CursorPosition;

        bool isCursorOverButton = false;
        if (buttonSprayPaint != null)
        {
            RectTransform btnRect = buttonSprayPaint.GetComponent<RectTransform>();
            Vector3 localPos = btnRect.InverseTransformPoint(targetCursor.position);
            isCursorOverButton = btnRect.rect.Contains(localPos);
        }

        // 当光标在按钮上按下时，执行按钮点击（即清理画布）
        if (Input.GetKeyDown(KeyCode.JoystickButton1) && isCursorOverButton)
        {
            buttonSprayPaint.onClick.Invoke();
        }

        // Check for JoystickButton1 press
        // 如果不在按钮上，才执行喷漆逻辑
        if (Input.GetKey(KeyCode.JoystickButton1) && !isCursorOverButton)
        {
            timer += Time.deltaTime;
            while (timer >= spawnInterval)
            {
                timer -= spawnInterval;
                SpawnPaint();
            }
        }
        else
        {
            timer = spawnInterval; // Ensure immediate spawn on next press
        }
    }

    void SpawnPaint()
    {
        GameObject paintObj = new GameObject("PaintDot");
        // Set parent to container
        paintObj.transform.SetParent(paintContainer, false);

        Image img = paintObj.AddComponent<Image>();
        img.raycastTarget = false; // Disable interaction so it doesn't block clicks
        img.sprite = paintSprite != null ? paintSprite : defaultSprite;
        img.color = paintColor;

        // Assign the spray material
        if (runtimeMaterial != null)
        {
            img.material = runtimeMaterial;
        }

        RectTransform paintRect = paintObj.GetComponent<RectTransform>();
        Vector2 baseSize = targetCursor.rect.size;
        float randomScale = 1.0f + Random.Range(-scaleVariation, scaleVariation);
        paintRect.sizeDelta = baseSize * randomScale;
        paintRect.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        Vector2 pos = targetCursor.anchoredPosition;
        Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
        paintRect.anchoredPosition = pos + randomOffset;
        if (targetCursor.parent == paintContainer)
        {
            int cursorIndex = targetCursor.GetSiblingIndex();
            // Using a lower index puts it behind the cursor in default overlay rendering
            paintRect.SetSiblingIndex(cursorIndex);
        }
    }

    // Public method to clear canvas
    public void ClearCanvas()
    {
        if (paintContainer == null) return;
        // Loop backwards to destroy children
        for (int i = paintContainer.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = paintContainer.transform.GetChild(i);
            if (child == targetCursor.transform) continue;
            Destroy(child.gameObject);
        }
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(false, true, true, false, true, (int)FilterType.NONE);
    }

}
