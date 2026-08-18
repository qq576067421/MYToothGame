using UnityEngine;
using static YouDooSDKConstants;

/// <summary>
/// 简单的双手光标信息显示组件。
/// 在屏幕左上角以文本显示光标数据，并在屏幕对应位置绘制圆形光标。
/// </summary>
public class DrawHandsCursorInfoDemo : MonoBehaviour
{
    [SerializeField] PlayerTextuerShow playerTextuerShow;
    // 存储最新的光标数据，用于绘制
    private HandsCursorData currentData;

    // 用于屏幕绘制的纹理（一个简单的圆形）
    private Texture2D cursorTexture;

    // 光标的大小（像素）
    private int cursorSize = 50;

    /// <summary>
    /// 初始化光标纹理
    /// </summary>
    private void Start()
    {
        CreateCursorTexture();
        playerTextuerShow.ShowHandsCursorDataAction += ShowHandsCursorData;
    }

    /// <summary>
    /// 创建一个简单的白色圆形纹理作为光标
    /// </summary>
    private void CreateCursorTexture()
    {
        cursorTexture = new Texture2D(cursorSize, cursorSize);
        Color[] colors = new Color[cursorSize * cursorSize];

        // 计算圆的中心
        float center = cursorSize / 2f;
        float radius = cursorSize / 2f;

        for (int y = 0; y < cursorSize; y++)
        {
            for (int x = 0; x < cursorSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // 如果像素在圆内，设置为白色（带一点透明度）
                if (distance <= radius)
                {
                    colors[y * cursorSize + x] = new Color(1f, 1f, 1f, 0.8f);
                }
                else
                {
                    colors[y * cursorSize + x] = Color.clear;
                }
            }
        }

        cursorTexture.SetPixels(colors);
        cursorTexture.Apply();
    }

    /// <summary>
    /// 每帧被调用，传入最新的光标数据。
    /// </summary>
    /// <param name="handsCursorData">当前帧的双手光标数据</param>
    public void ShowHandsCursorData(HandsCursorData handsCursorData)
    {
        // 保存数据供 OnGUI 使用
        currentData = handsCursorData;

        if(currentData  != null )
        {
          Debug.Log( "手势" +  JsonUtility.ToJson(currentData, true) );
            
        }
    }

    /// <summary>
    /// 绘制 GUI 元素
    /// </summary>
    private void OnGUI()
    {
        if (currentData == null) return;

        // 显示文本信息在左上角
        GUILayout.BeginArea(new Rect(10, 10, 300, 150), "Hands Cursor Info", GUI.skin.window);
        GUILayout.Label(RenderAPI.GetTextByLanId("sdk_demo_hand_code", currentData.code));
        GUILayout.Label(RenderAPI.GetTextByLanId("sdk_demo_hand_left_press", currentData.leftPress == 1 ? "Pressed" : "Released"));
        GUILayout.Label(RenderAPI.GetTextByLanId("sdk_demo_hand_left_pos", currentData.leftX.ToString("F2"), currentData.leftY.ToString("F2")));
        GUILayout.Label(RenderAPI.GetTextByLanId("sdk_demo_hand_right_press", currentData.rightPress == 1 ? "Pressed" : "Released"));
        GUILayout.Label(RenderAPI.GetTextByLanId("sdk_demo_hand_right_pos", currentData.rightX.ToString("F2"), currentData.rightY.ToString("F2")));
        GUILayout.EndArea();

        // 绘制光标（如果纹理存在）
        if (cursorTexture == null) return;

        // 绘制左手光标（绿色）
        if (currentData.leftX >= 0 && currentData.leftX <= 1 &&
            currentData.leftY >= 0 && currentData.leftY <= 1)
        {
            DrawCursor(currentData.leftX, currentData.leftY, Color.green, currentData.leftPress == 1);
        }

        // 绘制右手光标（红色）
        if (currentData.rightX >= 0 && currentData.rightX <= 1 &&
            currentData.rightY >= 0 && currentData.rightY <= 1)
        {
            DrawCursor(currentData.rightX, currentData.rightY, Color.red, currentData.rightPress == 1);
        }
    }

    /// <summary>
    /// 在指定屏幕相对位置绘制光标
    /// </summary>
    /// <param name="normalizedX">屏幕相对 X [0,1]</param>
    /// <param name="normalizedY">屏幕相对 Y [0,1]</param>
    /// <param name="color">光标颜色</param>
    /// <param name="isPressed">是否按下（按下时变大）</param>
    private void DrawCursor(float normalizedX, float normalizedY, Color color, bool isPressed)
    {
        // 屏幕坐标转换：Y轴翻转（Unity GUI 原点在左上角）
        float screenX = normalizedX * Screen.width;
        float screenY = normalizedY * Screen.height;

        // 根据按压状态调整大小
        float currentSize = isPressed ? cursorSize * 1.5f : cursorSize;
        float halfSize = currentSize / 2f;

        // 设置 GUI 颜色
        GUI.color = color;

        // 绘制光标纹理（居中显示）
        GUI.DrawTexture(
            new Rect(screenX - halfSize, screenY - halfSize, currentSize, currentSize),
            cursorTexture
        );

        // 恢复颜色
        GUI.color = Color.white;
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void OnDestroy()
    {
        if (cursorTexture != null)
        {
            Destroy(cursorTexture);
        }
    }
}
