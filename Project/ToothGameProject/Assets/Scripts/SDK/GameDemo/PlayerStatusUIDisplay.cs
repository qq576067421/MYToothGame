using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusUIDisplay : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("如果不指定，会在 OnEnable 中自动查找场景中的 AndroidParseDataDemo")]
    public AndroidParseDataDemo parseDataDemo;

    [Header("UI展示组件 (可选)")]
    [Tooltip("UGUI Text 组件，用于展示状态")]
    public Text statusTextUI;
    
    [Tooltip("TextMeshPro UGUI 组件，用于展示状态")]
    public TextMeshProUGUI statusTMPUI;

    // 缓存每个玩家槽位（Index）的状态
    private Dictionary<int, string> _playerStates = new Dictionary<int, string>();
    private string _globalState = string.Empty;

    private void OnEnable()
    {
        _globalState = RenderAPI.GetTextByLanId("sdk_demo_global_status_waiting_update");

        // 自动获取数据源
        if (parseDataDemo == null)
        {
            parseDataDemo = FindObjectOfType<AndroidParseDataDemo>();
        }

        if (parseDataDemo != null)
        {
            // 订阅事件
            parseDataDemo.onPlayerNotInReadyArea += OnPlayerNotInReadyArea;
            parseDataDemo.onPlayerCancelReady += OnPlayerCancelReady;
            parseDataDemo.onPlayerIsReady += OnPlayerIsReady;
            parseDataDemo.onNoneIsArea += OnNoneIsArea;
            parseDataDemo.onCanGameStart += OnCanGameStart;
            parseDataDemo.onPlayerDisappeared += OnPlayerDisappeared;
            parseDataDemo.onPlayerReviced += OnPlayerReviced;
            parseDataDemo.onRestartGame += OnRestartGame;
            parseDataDemo.onPlayerNotGame += OnPlayerNotGame;
            
            // 每帧刷新 UI
            parseDataDemo.onFrameInfoRefresh += OnFrameInfoRefresh;
        }
        else
        {
            Debug.LogWarning("PlayerStatusUIDisplay: 场景中未找到 AndroidParseDataDemo 组件！");
        }
    }

    private void OnDisable()
    {
        if (parseDataDemo != null)
        {
            // 注销事件，防止内存泄漏
            parseDataDemo.onPlayerNotInReadyArea -= OnPlayerNotInReadyArea;
            parseDataDemo.onPlayerCancelReady -= OnPlayerCancelReady;
            parseDataDemo.onPlayerIsReady -= OnPlayerIsReady;
            parseDataDemo.onNoneIsArea -= OnNoneIsArea;
            parseDataDemo.onCanGameStart -= OnCanGameStart;
            parseDataDemo.onPlayerDisappeared -= OnPlayerDisappeared;
            parseDataDemo.onPlayerReviced -= OnPlayerReviced;
            parseDataDemo.onRestartGame -= OnRestartGame;
            parseDataDemo.onPlayerNotGame -= OnPlayerNotGame;
            
            parseDataDemo.onFrameInfoRefresh -= OnFrameInfoRefresh;
        }
    }

    #region 事件回调处理

    private void OnPlayerNotInReadyArea(int index)
    {
        _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_out_area");
    }

    private void OnPlayerCancelReady(int index)
    {
        _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_cancel");
    }

    private void OnPlayerIsReady(int index, int curFrame, int needFrame)
    {
        if (curFrame >= needFrame && needFrame > 0)
        {
            _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_done");
        }
        else if (curFrame > 0)
        {
            _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_progress", curFrame, needFrame);
        }
        else
        {
            // curFrame == 0 的情况，通常意味着准备动作中断或重置
            _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_no_pose");
        }
    }

    private void OnNoneIsArea(int index)
    {
        _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_ready_empty");
    }

    private void OnCanGameStart()
    {
        _globalState = RenderAPI.GetTextByLanId("sdk_demo_ready_all_done");
    }

    private void OnPlayerDisappeared(int[] indices)
    {
        if (indices == null) return;
        foreach (var index in indices)
        {
            _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_player_lost");
        }
    }

    private void OnPlayerReviced(int[] indices)
    {
        if (indices == null) return;
        foreach (var index in indices)
        {
            _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_player_recovered");
        }
    }

    private void OnRestartGame(bool arg1, int arg2, int arg3)
    {
        _globalState = RenderAPI.GetTextByLanId("sdk_demo_restart_game", arg1, arg2, arg3);
        _playerStates.Clear(); // 重新开始时清空玩家状态缓存
    }

    private void OnPlayerNotGame(int index, int arg2, int arg3)
    {
        _playerStates[index] = RenderAPI.GetTextByLanId("sdk_demo_player_not_game");
    }

    private void OnFrameInfoRefresh()
    {
        UpdateUI();
    }

    #endregion

    /// <summary>
    /// 将状态文本格式化并更新到绑定的 UI 组件上
    /// </summary>
    private void UpdateUI()
    {
        // 如果没有绑定任何 UI 组件，则直接返回，依赖 OnGUI 显示
        if (statusTextUI == null && statusTMPUI == null) return;

        string finalText = GenerateStatusText();

        if (statusTextUI != null)
        {
            // UGUI Text 不支持 <color=red> 等富文本的话需要开启 richText，默认是开启的
            statusTextUI.text = finalText;
        }
        
        if (statusTMPUI != null)
        {
            statusTMPUI.text = finalText;
        }
    }

    /// <summary>
    /// 生成格式化后的状态文本
    /// </summary>
    private string GenerateStatusText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_global_status_title"));
        sb.AppendLine(_globalState);
        sb.AppendLine();
        sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_player_status_title"));
        
        if (_playerStates.Count == 0)
        {
            sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_player_status_empty"));
        }
        else
        {
            // 对键进行排序输出，使 UI 展示更稳定
            List<int> keys = new List<int>(_playerStates.Keys);
            keys.Sort();
            foreach (var key in keys)
            {
                sb.AppendLine(RenderAPI.GetTextByLanId("sdk_demo_player_status_line", key, _playerStates[key]));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 提供一个备用的 OnGUI 显示。如果开发者没有挂载任何 UI 组件，直接在屏幕左上角打印状态，方便快速调试。
    /// </summary>
    private void OnGUI()
    {
        if (statusTextUI == null && statusTMPUI == null)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.richText = true;
            
            // 绘制半透明背景以便看清文字
            string text = GenerateStatusText();
            Vector2 size = style.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(10, 10, size.x + 20, size.y + 20), "");
            
            GUI.Label(new Rect(20, 20, size.x, size.y), text, style);
        }
    }
}
