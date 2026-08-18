// SimpleUILogger.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace YouDooSDK.Utils
{
/// <summary>
/// 超简化 UI 日志显示
/// </summary>
public class UILogger : MonoBehaviour
{
    public Text logText; // 拖拽一个 UI Text 组件到这里
    public int maxLogLines = 10; // 最大显示行数
    
    private Queue<string> _logs = new Queue<string>();
    
    /// <summary>
    /// 添加日志
    /// </summary>
    public void AddLog(string message)
    {
        if (logText == null) return;
        
        // 添加新日志
        _logs.Enqueue(message);
        
        // 限制行数
        while (_logs.Count > maxLogLines)
        {
            _logs.Dequeue();
        }
        
        // 更新显示
        UpdateText();
    }
    
    /// <summary>
    /// 清空日志
    /// </summary>
    public void Clear()
    {
        _logs.Clear();
        if (logText != null)
            logText.text = "";
    }
    
    private void UpdateText()
    {
        logText.text = string.Join("\n", _logs.ToArray());
    }
}
}
