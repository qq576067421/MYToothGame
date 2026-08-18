using UnityEngine;
using YouDooSDK.Utils;
using static YouDooSDKConstants;

public class PlayerMatchView : Singleton<PlayerMatchView>
{
    private PlayerMatchViewMode _playerMatchViewMode = PlayerMatchViewMode.FullView;
    public PlayerMatchViewMode PlayerMatchViewMode { get => _playerMatchViewMode; set => _playerMatchViewMode = value; }


    /// <summary>
    /// 根据业务需求,需要绘制出人物的图片
    /// </summary>
    private float[,] personPlayerDrawRectf;
    public float[,] PersonPlayerDrawRectf { get => personPlayerDrawRectf; set => personPlayerDrawRectf = value; }

    /// <summary>
    /// 根据业务需求,需要绘制出人物的图片
    /// </summary>
    private float[,] personPlayerDrawDefaultRectf;

    public float READY_AREA_LEFT_MARGIN = 0f;
    public float READY_AREA_RIGHT_MARGIN = 1f;
    public float READY_AREA_TOP_MARGIN = 0.05f;
    public float READY_AREA_BOTTOM_MARGIN = 0.95f;

    public const int RECT_ELEMENTS_PER_PLAYER = 4;

    CameraTextureViewManager cameraTextureViewManager;

    public void InitPlayerMatchView(int playerNumber, PlayerMatchViewMode mode)
    {
        if (cameraTextureViewManager != null)
        {
            cameraTextureViewManager.ClearAllPersons();
            cameraTextureViewManager = null;
        }
        personPlayerDrawRectf = null;
        personPlayerDrawDefaultRectf = null;

        READY_AREA_TOP_MARGIN = 0.05f;
        PlayerMatchViewMode = mode;
        cameraTextureViewManager ??= new CameraTextureViewManager(READY_AREA_LEFT_MARGIN, READY_AREA_TOP_MARGIN, READY_AREA_RIGHT_MARGIN, READY_AREA_BOTTOM_MARGIN);
        cameraTextureViewManager.SetCurPlayerCount(playerNumber);
        cameraTextureViewManager.ClearAllPersons();
        personPlayerDrawRectf = new float[playerNumber, RECT_ELEMENTS_PER_PLAYER];
        personPlayerDrawDefaultRectf = new float[playerNumber, RECT_ELEMENTS_PER_PLAYER];
    }

    public bool CheckCameraTextureViewManager()
    {
        return cameraTextureViewManager != null;
    }


    public void ResetData()
    {
        cameraTextureViewManager.ResetData();
    }


    public Rect CalculationResult()
    {
        return cameraTextureViewManager.CalculationResult();
    }



    public void SetPersonPlayerRectf(float[,] personPlayerRectf, int playerId, float score)
    {
        float leftShoulderX = personPlayerRectf[(int)KeyPointIndex.Leftshoulder, 0];
        float rightShoulderX = personPlayerRectf[(int)KeyPointIndex.Rightshoulder, 0];
        float shoulderWidth = Mathf.Abs(leftShoulderX - rightShoulderX);

        // 修正边界框扩展，使其更合理地包围人物
        float leftMargin = Mathf.Min(leftShoulderX, rightShoulderX);
        float rightMargin = Mathf.Max(leftShoulderX, rightShoulderX);
        float topMargin;
        if (PlayerMatchViewMode == PlayerMatchViewMode.HalfView)
        {
            topMargin = personPlayerRectf[(int)KeyPointIndex.Nose, 1] - 1.5f * shoulderWidth;
        }
        else if (PlayerMatchViewMode == PlayerMatchViewMode.FullView)
        {
            topMargin = personPlayerRectf[(int)KeyPointIndex.Nose, 1] - 2f * shoulderWidth;
        }
        else
        {
            topMargin = personPlayerRectf[(int)KeyPointIndex.Nose, 1] - 2f * shoulderWidth;
            READY_AREA_TOP_MARGIN = 0f;
        }
        float hipY;
        // 采用臀部高度作为基础截取半身
        if (PlayerMatchViewMode == PlayerMatchViewMode.HalfView)
        {
            hipY = (personPlayerRectf[(int)KeyPointIndex.Lefthip, 1] + personPlayerRectf[(int)KeyPointIndex.Righthip, 1]) / 2 - 1f * shoulderWidth;
        }
        else if (PlayerMatchViewMode == PlayerMatchViewMode.FullView)
        {
            hipY = (personPlayerRectf[(int)KeyPointIndex.Leftankle, 1] + personPlayerRectf[(int)KeyPointIndex.Rightankle, 1]) / 2 - 1.5f * shoulderWidth;
        }
        else
        {
            hipY = (personPlayerRectf[(int)KeyPointIndex.Leftankle, 1] + personPlayerRectf[(int)KeyPointIndex.Rightankle, 1]) / 2 - 1.5f * shoulderWidth;
        }
        float bottomMargin = hipY;
        leftMargin = Mathf.Max(leftMargin, READY_AREA_LEFT_MARGIN);
        rightMargin = Mathf.Min(rightMargin, READY_AREA_RIGHT_MARGIN);
        topMargin = Mathf.Max(topMargin, READY_AREA_TOP_MARGIN);
        bottomMargin = Mathf.Min(bottomMargin, READY_AREA_BOTTOM_MARGIN);
        cameraTextureViewManager.AddPerson(playerId, leftMargin, topMargin, rightMargin, bottomMargin, score);
    }

    /// <summary>
    /// 分割区域时区域的划分
    /// </summary>
    /// <param name="curPlayerCount"></param>
    /// <param name="personPlayerReadyRectf"></param>
    public void SetpersonPlayerReadyPartitionRectf(int curPlayerCount, float[,] personPlayerReadyRectf)
    {
        if (curPlayerCount < 1 || curPlayerCount > 4)
        {
            return;
        }
        switch (curPlayerCount)
        {
            case 1:
                personPlayerReadyRectf[0, 0] = 0.45f; personPlayerReadyRectf[0, 1] = 0.0f; personPlayerReadyRectf[0, 2] = 0.55f; personPlayerReadyRectf[0, 3] = 0.9f;
                break;
            case 2:
                personPlayerReadyRectf[0, 0] = 0.25f; personPlayerReadyRectf[0, 1] = 0.0f; personPlayerReadyRectf[0, 2] = 0.45f; personPlayerReadyRectf[0, 3] = 0.9f;
                personPlayerReadyRectf[1, 0] = 0.55f; personPlayerReadyRectf[1, 1] = 0.0f; personPlayerReadyRectf[1, 2] = 0.75f; personPlayerReadyRectf[1, 3] = 0.9f;

                break;
            case 3:
                personPlayerReadyRectf[0, 0] = 0.2f; personPlayerReadyRectf[0, 1] = 0.0f; personPlayerReadyRectf[0, 2] = 0.4f; personPlayerReadyRectf[0, 3] = 0.9f;
                personPlayerReadyRectf[1, 0] = 0.45f; personPlayerReadyRectf[1, 1] = 0.0f; personPlayerReadyRectf[1, 2] = 0.55f; personPlayerReadyRectf[1, 3] = 0.9f;
                personPlayerReadyRectf[2, 0] = 0.6f; personPlayerReadyRectf[2, 1] = 0.0f; personPlayerReadyRectf[2, 2] = 0.8f; personPlayerReadyRectf[2, 3] = 0.9f;
                break;
            case 4:
                personPlayerReadyRectf[0, 0] = 0.1f; personPlayerReadyRectf[0, 1] = 0.0f; personPlayerReadyRectf[0, 2] = 0.375f; personPlayerReadyRectf[0, 3] = 0.9f;
                personPlayerReadyRectf[1, 0] = 0.375f; personPlayerReadyRectf[1, 1] = 0.0f; personPlayerReadyRectf[1, 2] = 0.5f; personPlayerReadyRectf[1, 3] = 0.9f;
                personPlayerReadyRectf[2, 0] = 0.5f; personPlayerReadyRectf[2, 1] = 0.0f; personPlayerReadyRectf[2, 2] = 0.625f; personPlayerReadyRectf[2, 3] = 0.9f;
                personPlayerReadyRectf[3, 0] = 0.625f; personPlayerReadyRectf[3, 1] = 0.0f; personPlayerReadyRectf[3, 2] = 1.0f; personPlayerReadyRectf[3, 3] = 0.9f;
                break;
            default:
                Debug.LogError($"不支持 {curPlayerCount} 个玩家的准备区设置");
                break;
        }

        float ratio = 1.5f;

        for (int i = 0; i < curPlayerCount; i++)
        {
            float readyLeft = personPlayerReadyRectf[i, 0];
            float readyTop = personPlayerReadyRectf[i, 1];
            float readyRight = personPlayerReadyRectf[i, 2];
            float readyBottom = personPlayerReadyRectf[i, 3];
            if (i == 1 || i == 2)
            {
                ratio = 1.8f;
            }
            // 宽度的中心等比缩小
            float readyWidth = readyRight - readyLeft;
            float drawWidth = readyWidth / ratio;
            float offsetX = (readyWidth - drawWidth) / 2f;

            // 高度也必须进行相同的中心等比缩小，否则底层算法为了保持宽高比不失真，
            // 会以未缩小的高度为基准把宽度重新补满，导致视觉上没有放大！
            float readyHeight = readyBottom - readyTop;
            float drawHeight = readyHeight / ratio;
            float offsetY = (readyHeight - drawHeight) / 2f;

            personPlayerDrawDefaultRectf[i, 0] = readyLeft + offsetX;
            personPlayerDrawDefaultRectf[i, 1] = readyTop + offsetY;
            personPlayerDrawDefaultRectf[i, 2] = readyRight - offsetX;
            personPlayerDrawDefaultRectf[i, 3] = readyBottom - offsetY;

            // 初始状态下让绘制框等于默认框
            for (int j = 0; j < 4; j++)
            {
                personPlayerDrawRectf[i, j] = personPlayerDrawDefaultRectf[i, j];
            }
        }
    }

    public void SetPersonPlayerReadyDrawPartitionRectf(float[,] keyPointListPose, int playerIdx)
    {
        if (keyPointListPose != null)
        {
            // Debug.Log($"[PlayerMatchView] SetPersonPlayerReadyDrawPartitionRectf 设置有效数据 playerIdx={playerIdx}");
            float leftShoulderX = keyPointListPose[(int)KeyPointIndex.Leftshoulder, 0];
            float rightShoulderX = keyPointListPose[(int)KeyPointIndex.Rightshoulder, 0];
            float shoulderWidth = Mathf.Abs(leftShoulderX - rightShoulderX);

            // 修正边界框扩展，使其更合理地包围人物
            float leftMargin = Mathf.Min(leftShoulderX, rightShoulderX);
            float rightMargin = Mathf.Max(leftShoulderX, rightShoulderX);
            float topMargin = keyPointListPose[(int)KeyPointIndex.Nose, 1] - 2f * shoulderWidth;
            float bottomMargin = (keyPointListPose[(int)KeyPointIndex.Leftankle, 1] + keyPointListPose[(int)KeyPointIndex.Rightankle, 1]) / 2 - 0.5f * shoulderWidth; 
            leftMargin = Mathf.Max(leftMargin, READY_AREA_LEFT_MARGIN);
            rightMargin = Mathf.Min(rightMargin, READY_AREA_RIGHT_MARGIN);
            topMargin = Mathf.Max(topMargin, READY_AREA_TOP_MARGIN);
            bottomMargin = Mathf.Min(bottomMargin, READY_AREA_BOTTOM_MARGIN);

            float rectWidth = rightMargin - leftMargin;
            float rectHeight = bottomMargin - topMargin;
            if (rectWidth < 0.01f || rectHeight < 0.01f)
            {
                for (int j = 0; j < 4; j++)
                    personPlayerDrawRectf[playerIdx, j] = personPlayerDrawDefaultRectf[playerIdx, j];
            }
            else
            {
                personPlayerDrawRectf[playerIdx, 0] = leftMargin;
                personPlayerDrawRectf[playerIdx, 1] = topMargin;
                personPlayerDrawRectf[playerIdx, 2] = rightMargin;
                personPlayerDrawRectf[playerIdx, 3] = bottomMargin;
            }
        }
        else
        {
            // 重置矩形数据
            // Debug.Log($"[PlayerMatchView] SetPersonPlayerReadyDrawPartitionRectf 收到空数据，使用默认框 playerIdx={playerIdx}");
            for (int j = 0; j < 4; j++)
                personPlayerDrawRectf[playerIdx, j] = personPlayerDrawDefaultRectf[playerIdx, j];
        }
    }
}

/// <summary>
/// 有三种匹配模式
/// </summary>
public enum PlayerMatchViewMode
{
    FullView,//全身
    HalfView,//半身
    PartitionView,//人物切割
    Length
}
