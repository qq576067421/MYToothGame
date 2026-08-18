using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Input = InputSystemCompat;

public class ShootingPanel : MainScene
{
    [FormerlySerializedAs("gyroDataTestDemo")]
    [SerializeField] GyroDataReader gyroDataReader;
    [Tooltip("The cursor RectTransform to track")]
    public RectTransform targetCursor;

    [Tooltip("The Canvas or Panel where balloons will be instantiated")]
    public RectTransform balloonContainer;

    [Tooltip("Time between balloon spawns")]
    public float spawnInterval = 3.0f;

    [Tooltip("How fast the balloon moves up")]
    public float balloonSpeed = 200f;

    [Tooltip("Cooldown time between shots")]
    public float shootInterval = 0.5f;

    [Header("Cursor Speed Control")]
    public float[] speedLevels = new float[] { 0.01f, 0.03f, 0.05f, 0.07f, 0.09f };
    public int currentSpeedLevel = 2; // 默认为第3挡 (0.05f)

    private Text speedLevelText;
    private Sprite balloonSprite;
    private Sprite balloonOKSprite;
    private AudioClip hitAudioClip;
    private AudioClip hitNoAudioClip;
    private AudioSource audioSource;
    private Sprite sightSprite;
    private Sprite sightNoSprite;
    private Sprite bulletHoleSprite;

    private float spawnTimer = 0f;
    private float shootTimer = 0f;

    // For initial balloons state
    private bool isInitialPhase = true;
    private int initialBalloonsRemaining = 0;

    // 保存当前所有的气球
    private List<BalloonBehavior> activeBalloons = new List<BalloonBehavior>();

    private void LoadResources()
    {
        if (balloonSprite == null) balloonSprite = Resources.Load<Sprite>("Image/balloon");
        if (balloonOKSprite == null) balloonOKSprite = Resources.Load<Sprite>("Image/balloonOK");
        if (hitAudioClip == null) hitAudioClip = Resources.Load<AudioClip>("Music/hit");
        if (hitNoAudioClip == null) hitNoAudioClip = Resources.Load<AudioClip>("Music/hitNo");
        if (sightSprite == null) sightSprite = Resources.Load<Sprite>("Image/sight");
        if (sightNoSprite == null) sightNoSprite = Resources.Load<Sprite>("Image/sightNo");
        if (bulletHoleSprite == null) bulletHoleSprite = Resources.Load<Sprite>("Image/bulletHole");

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 确保是完全 2D 声音
            audioSource.volume = 1f; // 确保音量最大
        }
    }

    protected override void OnEnable()
    {
        LoadResources();
        base.OnEnable();
        AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(true, false, false, false, false, 0);
        ClearBalloons();
        ApplySpeedLevel();

        isInitialPhase = true;
        SpawnInitialBalloons();

        spawnTimer = spawnInterval; // 确保进入常规生成阶段时立即生成
        shootTimer = 0f; // 重置射击冷却

        gyroDataReader.ResetGyroMappingState();
    }

    private void SpawnInitialBalloons()
    {
        initialBalloonsRemaining = 5;
        List<Vector2> generatedPositions = new List<Vector2>();

        for (int i = 0; i < 5; i++)
        {
            GameObject balloonObj = new GameObject("Balloon_Initial_" + i);
            balloonObj.transform.SetParent(balloonContainer, false);

            Image img = balloonObj.AddComponent<Image>();
            img.sprite = balloonSprite;
            img.raycastTarget = false;

            RectTransform rect = balloonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 150f);

            if (balloonContainer != null)
            {
                float containerWidth = balloonContainer.rect.width;
                float containerHeight = balloonContainer.rect.height;

                Vector2 newPos = Vector2.zero;
                bool validPosition = false;
                int maxAttempts = 100;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float randomX = Random.Range(-containerWidth / 2f + 75f, containerWidth / 2f - 75f);
                    float randomY = Random.Range(-containerHeight / 2f + 75f, containerHeight / 2f - 75f);
                    newPos = new Vector2(randomX, randomY);

                    validPosition = true;
                    foreach (Vector2 pos in generatedPositions)
                    {
                        // 气球尺寸为 150x150，中心点距离小于 160 则判断为重叠（留出一点额外间隙）
                        if (Vector2.Distance(newPos, pos) < 160f)
                        {
                            validPosition = false;
                            break;
                        }
                    }

                    if (validPosition)
                    {
                        break;
                    }
                }

                rect.anchoredPosition = newPos;
                generatedPositions.Add(newPos);
            }

            BalloonBehavior behavior = balloonObj.AddComponent<BalloonBehavior>();
            behavior.containerRect = balloonContainer;
            behavior.floatSpeed = 0f;
            behavior.isStationary = true;
            behavior.onDestroyed = OnInitialBalloonDestroyed;
            behavior.hitSprite = balloonOKSprite;
            behavior.bulletHoleSprite = bulletHoleSprite;

            activeBalloons.Add(behavior);
            balloonObj.transform.SetAsFirstSibling();
        }
    }

    private void OnInitialBalloonDestroyed(BalloonBehavior b)
    {
        activeBalloons.Remove(b);
        initialBalloonsRemaining--;
        if (initialBalloonsRemaining <= 0 && isInitialPhase)
        {
            isInitialPhase = false;
            spawnTimer = spawnInterval; // Reset spawn timer for normal spawning
        }
    }

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

            Debug.Log($"[ShootingPanel] Cursor speed level changed to {currentSpeedLevel + 1} (Sensitivity: {speedLevels[currentSpeedLevel]})");
        }

        EnsureSpeedLevelTextExists();
        string[] levelNames = { "一", "二", "三", "四", "五" };
        if (currentSpeedLevel >= 0 && currentSpeedLevel < levelNames.Length)
        {
            speedLevelText.text = $"当前速度为：{levelNames[currentSpeedLevel]}级";
        }
    }

    void Update()
    {
        if (targetCursor == null || balloonContainer == null) return;
        targetCursor.transform.localPosition = gyroDataReader.CursorPosition;

        // --- 光标瞄准状态更新 ---
        bool isAiming = false;
        for (int i = activeBalloons.Count - 1; i >= 0; i--)
        {
            BalloonBehavior balloon = activeBalloons[i];
            if (balloon == null || balloon.isHit) continue;

            RectTransform balloonRect = balloon.GetComponent<RectTransform>();
            Vector3 localPos = balloonRect.InverseTransformPoint(targetCursor.position);

            if (balloonRect.rect.Contains(localPos))
            {
                isAiming = true;
                break;
            }
        }

        Image cursorImage = targetCursor.GetComponent<Image>();
        if (cursorImage != null)
        {
            if (isAiming && sightSprite != null)
                cursorImage.sprite = sightSprite;
            else if (!isAiming && sightNoSprite != null)
                cursorImage.sprite = sightNoSprite;
        }

        // --- 生成气球逻辑 ---
        if (!isInitialPhase)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnBalloon();
            }
        }

        // 更新射击冷却计时器
        if (shootTimer > 0f)
        {
            shootTimer -= Time.deltaTime;
        }

        // 射击检测: JoystickButton1 被按下且冷却结束
        if (Input.GetKeyDown(KeyCode.JoystickButton1) && shootTimer <= 0f)
        {
            shootTimer = shootInterval;
            Shoot();
        }
    }

    void SpawnBalloon()
    {
        GameObject balloonObj = new GameObject("Balloon");
        balloonObj.transform.SetParent(balloonContainer, false);

        Image img = balloonObj.AddComponent<Image>();
        img.sprite = balloonSprite;
        img.raycastTarget = false; // Disable interaction so it doesn't block clicks

        RectTransform rect = balloonObj.GetComponent<RectTransform>();
        // 大小 70~200 随机
        float randomSize = Random.Range(70f, 200f);
        rect.sizeDelta = new Vector2(randomSize, randomSize);

        // 计算随机 X 位置，留一点边距防止贴墙
        float containerWidth = balloonContainer.rect.width;
        float randomX = Random.Range(-containerWidth / 2f + randomSize / 2f, containerWidth / 2f - randomSize / 2f);

        // 初始位置在底部外面
        float startY = -balloonContainer.rect.height / 2f - rect.rect.height;
        rect.anchoredPosition = new Vector2(randomX, startY);

        // 添加行为组件
        BalloonBehavior behavior = balloonObj.AddComponent<BalloonBehavior>();
        behavior.containerRect = balloonContainer;
        // 速度有快有慢，在基础速度上乘上 0.5 到 1.5 的随机倍数
        behavior.floatSpeed = balloonSpeed * Random.Range(0.5f, 1.5f);
        behavior.onDestroyed = OnBalloonDestroyed;
        behavior.hitSprite = balloonOKSprite; // 传入击中后需要替换的图片
        behavior.bulletHoleSprite = bulletHoleSprite;

        activeBalloons.Add(behavior);

        // 将气球放在容器的最底层，从而保证后续原本在容器外或者在前面的光标不被遮挡，
        // 也避免每次调用 targetCursor.SetAsLastSibling() 可能引发层级问题
        balloonObj.transform.SetAsFirstSibling();
    }

    private void OnBalloonDestroyed(BalloonBehavior b)
    {
        activeBalloons.Remove(b);
    }

    void Shoot()
    {
        bool hitAny = false;
        // 倒序遍历以防止在循环中移除元素
        for (int i = activeBalloons.Count - 1; i >= 0; i--)
        {
            BalloonBehavior balloon = activeBalloons[i];
            if (balloon == null || balloon.isHit) continue; // 已经被击中的忽略

            RectTransform balloonRect = balloon.GetComponent<RectTransform>();
            Vector3 localPos = balloonRect.InverseTransformPoint(targetCursor.position);

            // 判断光标是否在气球矩形内
            if (balloonRect.rect.Contains(localPos))
            {
                balloon.Hit(localPos);
                hitAny = true;
                break; // 每次点击只击中一个气球
            }
        }

        if (audioSource != null)
        {
            if (hitAny && hitAudioClip != null)
            {
                audioSource.PlayOneShot(hitAudioClip);
            }
            else if (!hitAny && hitNoAudioClip != null)
            {
                audioSource.PlayOneShot(hitNoAudioClip);
            }
        }
    }

    public void ClearBalloons()
    {
        // 从后往前销毁以避免集合修改问题
        for (int i = activeBalloons.Count - 1; i >= 0; i--)
        {
            if (activeBalloons[i] != null)
            {
                activeBalloons[i].onDestroyed = null; // Prevent callback
                Destroy(activeBalloons[i].gameObject);
            }
        }
        activeBalloons.Clear();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // AndroidServerInfoDemo.Instance.SetHardWareRemoteControlConfig(false, true, true, false, true, (int)FilterType.NONE);
        ClearBalloons();
    }
}

// 控制气球飞行的内部组件类
public class BalloonBehavior : MonoBehaviour
{
    public RectTransform containerRect;
    public float floatSpeed = 200f;
    public System.Action<BalloonBehavior> onDestroyed;
    public bool isHit = false;
    public Sprite hitSprite; // 击中时的图片
    public Sprite bulletHoleSprite; // 弹孔图片
    public bool isStationary = false; // 是否为静止的初始气球

    private RectTransform rectTransform;
    private Image img;
    private float fadeSpeed = 3f;

    private int framesSinceHit = 0;
    private GameObject bulletHoleObj;

    private int trajectoryType;
    private float startX;
    private float timeAlive = 0f;
    private float sinFrequency;
    private float sinAmplitude;
    private float horizontalSpeed;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();

        trajectoryType = Random.Range(0, 3);
        startX = rectTransform.anchoredPosition.x;
        sinFrequency = Random.Range(1f, 3f);
        sinAmplitude = Random.Range(30f, 150f);
        horizontalSpeed = Random.Range(-100f, 100f);

        // 防止斜飞速度太小没效果
        if (Mathf.Abs(horizontalSpeed) < 30f)
        {
            horizontalSpeed = (horizontalSpeed > 0 ? 30f : -30f);
        }
    }

    void Update()
    {
        if (isHit)
        {
            framesSinceHit++;

            // 延迟两帧替换成破裂图片
            if (framesSinceHit == 2)
            {
                if (img != null && hitSprite != null)
                {
                    img.sprite = hitSprite;
                }
            }

            // 击中后缩小且变透明
            transform.localScale -= Vector3.one * fadeSpeed * Time.deltaTime;

            if (img != null)
            {
                Color c = img.color;
                c.a -= fadeSpeed * Time.deltaTime;
                img.color = c;
            }

            if (bulletHoleObj != null)
            {
                Image holeImg = bulletHoleObj.GetComponent<Image>();
                if (holeImg != null)
                {
                    Color hc = holeImg.color;
                    hc.a -= fadeSpeed * Time.deltaTime;
                    holeImg.color = hc;
                }
            }

            // 当完全缩小或透明时销毁
            if (transform.localScale.x <= 0 || (img != null && img.color.a <= 0))
            {
                DestroySelf();
            }
        }
        else
        {
            if (isStationary) return;

            timeAlive += Time.deltaTime;
            float newY = rectTransform.anchoredPosition.y + floatSpeed * Time.deltaTime;
            float newX = rectTransform.anchoredPosition.x;

            if (trajectoryType == 0)
            {
                // 轨迹0: 稍微倾斜的直线
                newX += horizontalSpeed * 0.5f * Time.deltaTime;
            }
            else if (trajectoryType == 1)
            {
                // 轨迹1: 正弦波浪形
                newX = startX + Mathf.Sin(timeAlive * sinFrequency) * sinAmplitude;
            }
            else if (trajectoryType == 2)
            {
                // 轨迹2: 碰到边缘反弹的斜飞
                newX += horizontalSpeed * Time.deltaTime;
                if (containerRect != null)
                {
                    float halfWidth = containerRect.rect.width / 2f - rectTransform.rect.width / 2f;
                    if (newX > halfWidth)
                    {
                        newX = halfWidth;
                        horizontalSpeed = -Mathf.Abs(horizontalSpeed);
                    }
                    else if (newX < -halfWidth)
                    {
                        newX = -halfWidth;
                        horizontalSpeed = Mathf.Abs(horizontalSpeed);
                    }
                }
            }

            rectTransform.anchoredPosition = new Vector2(newX, newY);

            // 飞出顶部屏幕边缘判断
            if (containerRect != null)
            {
                float topEdgeY = containerRect.rect.height / 2f + rectTransform.rect.height;
                if (rectTransform.anchoredPosition.y > topEdgeY)
                {
                    DestroySelf();
                }
            }
        }
    }

    public void Hit(Vector2 localHitPosition)
    {
        if (isHit) return;
        isHit = true;
        framesSinceHit = 0;

        // 生成弹孔
        if (bulletHoleSprite != null)
        {
            bulletHoleObj = new GameObject("BulletHole");
            bulletHoleObj.transform.SetParent(this.transform, false);

            Image holeImg = bulletHoleObj.AddComponent<Image>();
            holeImg.sprite = bulletHoleSprite;
            holeImg.raycastTarget = false;
            holeImg.color = Color.black; // 弹孔为黑色  
            RectTransform holeRect = bulletHoleObj.GetComponent<RectTransform>();
            // 设置一个合适的弹孔大小
            holeRect.sizeDelta = new Vector2(35f, 35f);
            holeRect.anchoredPosition = localHitPosition;
        }
    }

    private void DestroySelf()
    {
        onDestroyed?.Invoke(this);
        Destroy(gameObject);
    }


}
