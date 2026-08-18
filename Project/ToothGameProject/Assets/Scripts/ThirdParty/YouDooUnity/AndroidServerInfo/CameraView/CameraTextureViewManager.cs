using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CameraTextureViewManager
{
    // 内部结构：存储人物信息
    private struct PersonInfo
    {
        public int playerId;
        public Rect normalizedRect;
        public float score;
        public float area;
        public Vector2 center;
        public float distanceToCenter;      // 实际距离（用于显示）
        public float distanceToCenterSqr;   // 距离平方（用于排序比较，避免开方）
        public bool isValid;

        public int needFrame;

        private const float minValue = 0.001f;
        public PersonInfo(int playerId, float left, float top, float right, float bottom, float score)
        {
            this.playerId = playerId;

            // 确保坐标在[0,1]范围内
            left = Mathf.Clamp01(left);
            top = Mathf.Clamp01(top);
            right = Mathf.Clamp01(right);
            bottom = Mathf.Clamp01(bottom);

            // 确保right > left, bottom > top
            float width = Mathf.Max(right - left, minValue);
            float height = Mathf.Max(bottom - top, minValue);

            normalizedRect = new Rect(left, top, width, height);
            this.score = score;
            area = width * height;
            center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);

            // 计算到画面中心的距离 (0.5, 0.5)
            Vector2 screenCenter = new Vector2(0.5f, 0.5f);
            Vector2 diff = center - screenCenter;
            distanceToCenterSqr = diff.x * diff.x + diff.y * diff.y;
            distanceToCenter = Mathf.Sqrt(distanceToCenterSqr);
            isValid = true;
            needFrame = 30; // 丢失容忍帧数：如果没检测到，保持该框30帧
        }
    }


    private int _requiredPersonCount = 1;  // 需要优选的人数，默认为1  
    private Rect _lastBoundingRect; // 添加类字段保存上一帧的结果
    private Rect _targetBoundingRect; // 用于稳定输出的边界框
    private Rect _pendingBounds; // 正在等待确认的新边界框
    private int _framesSinceChange = 0; // 防抖动计数器

    private const int FramesToWaitBeforeZoomIn = 30;  // 缩小包围盒(画面放大)需要等待的帧数，过滤短暂的检测闪烁
    private const int FramesToWaitBeforeZoomOut = 10;  // 放大包围盒(画面缩小)的等待帧数，避免人物不动时偶尔被误检假人导致闪烁

    private const float BoundsChangeThreshold = 0.08f; // 阈值：与目标框差值超过8%才被认为发生显著变化
    private const float PendingChangeThreshold = 0.05f; // 阈值：新框与等待框的容差，如果相差不大则认为是同一种新状态

    private bool _isFirstFrame = true;

    private readonly Dictionary<int, PersonInfo> _personDict = new Dictionary<int, PersonInfo>();
    private readonly List<PersonInfo> _personList = new List<PersonInfo>(); // 用于排序
    private readonly List<int> _preferredPersonIds = new List<int>();
    private readonly List<Rect> _preferredRects = new List<Rect>();
    private readonly List<PersonInfo> _tempBestPersons = new List<PersonInfo>();

    private readonly float readyAreaLeftMargin = 0f;
    private readonly float readyAreaRightMargin = 0f;
    private readonly float readyAreaTopMargin = 0f;
    private readonly float readyAreaBottomMargin = 0f;

    public CameraTextureViewManager(float ready_area_left_margin, float ready_area_top_margin, float ready_area_right_margin, float ready_area_bottom_margin)
    {
        readyAreaLeftMargin = ready_area_left_margin;
        readyAreaTopMargin = ready_area_top_margin;
        readyAreaRightMargin = ready_area_right_margin;
        readyAreaBottomMargin = ready_area_bottom_margin;
    }

    public void SetCurPlayerCount(int count)
    {
        _requiredPersonCount = Mathf.Max(1, count);
    }


    // 准备新的一帧数据（被外部用作 ResetData 调用）
    public void ResetData()
    {
        // 我们不再粗暴清空字典，而是标记所有人的 isValid 为 false
        // 以便实现跨帧保留（防闪烁丢失）
        var keys = _personDict.Keys.ToList();
        foreach (var key in keys)
        {
            var p = _personDict[key];
            p.isValid = false;
            _personDict[key] = p;
        }

        _preferredPersonIds.Clear();
        _preferredRects.Clear();
        _tempBestPersons.Clear();
    }

    /// <summary>
    /// 添加人物信息
    /// </summary>
    public void AddPerson(int playerId, float left, float top, float right, float bottom, float score)
    {
        // 验证输入数据
        if (playerId < 0)
        {
            Debug.LogWarning($"无效的玩家ID abc : {playerId}");
            return;
        }

        // 验证分数有效性
        if (score < 0 || score > 1)
        {
            Debug.LogWarning($"无效的分数: {score}，应在[0,1]范围内");
            return;
        }

        // 验证矩形有效性
        if (!ValidateAndAdjustCoords(ref left, ref top, ref right, ref bottom))
        {
            // 如果坐标无效（越界等），我们不更新该玩家这一帧的信息，
            // 他的 isValid 依然是 false，会在 CalculationResult 中衰减寿命
            return;
        }

        PersonInfo newPerson = new PersonInfo(playerId, left, top, right, bottom, score);
        newPerson.isValid = true; // 标记本帧被成功更新

        // 更新或添加记录
        _personDict[playerId] = newPerson;
    }

    /// <summary>
    /// 验证和调整坐标，返回是否有效
    /// </summary>
    private bool ValidateAndAdjustCoords(ref float left, ref float top, ref float right, ref float bottom)
    {
        if (left < readyAreaLeftMargin || left > readyAreaRightMargin || top < readyAreaTopMargin || top > readyAreaBottomMargin ||
            right < readyAreaLeftMargin || right > readyAreaRightMargin || bottom < readyAreaTopMargin || bottom > readyAreaBottomMargin)
        {
            return false;
        }
        if (right <= left || bottom <= top)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 计算并返回包含所有优选人物的最大边界框
    /// </summary>
    public Rect CalculationResult()
    {
        _preferredPersonIds.Clear();
        _preferredRects.Clear();
        _personList.Clear();

        // 更新并清理人物生命周期，重建 _personList
        var keys = _personDict.Keys.ToList();
        foreach (var key in keys)
        {
            var p = _personDict[key];
            if (!p.isValid)
            {
                p.needFrame--;
                _personDict[key] = p; // 更新存活时间
                if (p.needFrame <= 0)
                {
                    _personDict.Remove(key);
                    continue; // 彻底死亡，不加入列表
                }
            }
            _personList.Add(p); // 将存活的人加入排序列表
        }

        // 如果没有找到任何优选人物，返回最大Rect
        if (_personList.Count == 0)
        {
            return new Rect(0, 0, 1, 1);
        }

        // 填充 _tempBestPersons
        FindBestPersonsInto(_requiredPersonCount, _tempBestPersons);

        if (_tempBestPersons.Count == 0)
        {
            return new Rect(0, 0, 1, 1);
        }

        for (int i = 0; i < _tempBestPersons.Count; i++)
        {
            var person = _tempBestPersons[i];
            _preferredPersonIds.Add(person.playerId);

            _preferredRects.Add(person.normalizedRect);
        }

        // 返回包含所有优选人物边界框的最大矩形
        return CalculateBoundingRect(_preferredRects);
    }

    private Rect CalculateSymmetricBoundingRect(List<Rect> rects)
    {
        if (rects == null || rects.Count == 0)
            return new Rect(0, 0, 1, 1);

        float maxDistX = 0f;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var rect in rects)
        {
            // X轴：计算相对于中心 (0.5) 的最大距离，实现左右对称
            float distL = Mathf.Abs(rect.x - 0.5f);
            float distR = Mathf.Abs(rect.x + rect.width - 0.5f);
            maxDistX = Mathf.Max(maxDistX, distL, distR);

            // Y轴：计算实际包围范围，不需要对称
            minY = Mathf.Min(minY, rect.y);
            maxY = Mathf.Max(maxY, rect.y + rect.height);
        }

        // X轴对称
        float width = maxDistX * 2f;

        // Y轴实际包围
        minY = Mathf.Clamp01(minY);
        maxY = Mathf.Clamp01(maxY);
        float height = Mathf.Max(maxY - minY, 0.001f);
        float y = minY;

        // Padding
        float padding = 0.01f;

        // X轴 Padding (保持对称)
        width += padding * 2;

        // Y轴 Padding (直接扩展)
        y -= padding;
        height += padding * 2;

        // 限制在 [0,1] 范围内

        // X轴：限制宽度为1，保持对称
        if (width > 1.0f)
        {
            width = 1.0f;
        }
        float x = 0.5f - width * 0.5f;

        // Y轴：限制在 [0,1] 内，优先保证位置有效
        if (height > 1.0f)
        {
            height = 1.0f;
            y = 0f;
        }
        else
        {
            // 尝试平移以适应边界
            if (y < 0f) y = 0f;
            if (y + height > 1.0f) y = 1.0f - height;
        }

        Rect bounds = new Rect(x, y, width, height);

        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            _lastBoundingRect = bounds;
            return bounds;
        }

        if (ShouldUseLastFrame(bounds, _lastBoundingRect, 0.1f))
        {
            return _lastBoundingRect;
        }

        _lastBoundingRect = bounds;
        return bounds;
    }

    /// <summary>
    /// 找出最优的N个人物并填充到指定列表
    /// </summary>
    private void FindBestPersonsInto(int count, List<PersonInfo> result)
    {
        result.Clear();

        if (_personList.Count == 0 || count <= 0)
            return;

        if (count >= _personList.Count)
        {
            // 如果需要的人数大于等于总人数，直接全部返回
            result.AddRange(_personList);
            return;
        }
        var sorted = new PersonInfo[_personList.Count];
        _personList.CopyTo(sorted);

        // 一次性排序，O(n log n)但更简单
        Array.Sort(sorted, (a, b) =>
        {
            // 主要按面积排序（降序）
            int distanceToCenterSqr = a.distanceToCenterSqr.CompareTo(b.distanceToCenterSqr);
            if (distanceToCenterSqr != 0) return distanceToCenterSqr;
            int areaCompare = b.area.CompareTo(a.area);
            if (areaCompare != 0) return areaCompare;
            // 最后按到中心距离排序（升序）
            return b.score.CompareTo(a.score);
        });

        // 取前count个
        for (int i = 0; i < count && i < sorted.Length; i++)
        {
            result.Add(sorted[i]);
        }
        // Debug.Log($"233 233 233 233  看一眼找到的人--- : {result.Count}---{result[0].playerId}");
    }



    /// <summary>
    /// 计算包含所有矩形的最小边界矩形
    /// </summary>
    private Rect CalculateBoundingRect(List<Rect> rects)
    {
        if (rects == null || rects.Count == 0)
            return new Rect(0, 0, 1, 1);

        // 计算边界框
        var bounds = CalculateBounds(rects);

        // 添加padding（可选）
        bounds = ApplyPadding(bounds, 0.05f);

        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            _lastBoundingRect = bounds;
            _targetBoundingRect = bounds;
            _pendingBounds = bounds;
            return bounds;
        }

        // 检查新的 bounds 与当前稳定的 _targetBoundingRect 是否存在显著差异
        bool isSignificantlyDifferent = !ShouldUseLastFrame(bounds, _targetBoundingRect, BoundsChangeThreshold);

        if (isSignificantlyDifferent)
        {
            // 发生了显著变化。检查这个变化是否和我们正在等待的 _pendingBounds 是一致的
            bool isSameAsPending = ShouldUseLastFrame(bounds, _pendingBounds, PendingChangeThreshold);

            if (isSameAsPending)
            {
                // 状态持续保持在这个新的异常区间
                _framesSinceChange++;

                // 判断相对于 _targetBoundingRect 是扩大还是缩小
                bool isExpandingBounds = _pendingBounds.width > _targetBoundingRect.width || _pendingBounds.height > _targetBoundingRect.height;
                int requiredFrames = isExpandingBounds ? FramesToWaitBeforeZoomOut : FramesToWaitBeforeZoomIn;

                if (_framesSinceChange >= requiredFrames)
                {
                    // 已经稳定在一个新状态足够长的时间，正式接受这个新的包围盒作为目标
                    _targetBoundingRect = _pendingBounds;
                    _framesSinceChange = 0;
                }
            }
            else
            {
                // 这是一个全新的跳变，或者是从一种异常跳到了另一种异常
                // 重置计数器，并将当前 bounds 作为新的待确认目标
                _pendingBounds = bounds;
                _framesSinceChange = 1;
            }
        }
        else
        {
            // 差异不大，或者又恢复了正常的稳定状态，彻底重置防抖计数器
            _framesSinceChange = 0;
            _pendingBounds = bounds; // 让 pending 保持跟随

            // 绝对死区(Deadzone)：如果差异极小（比如小于2%），彻底锁定包围盒，防止人物静止时的轻微抖动。
            bool isSlightlyDifferent = !ShouldUseLastFrame(bounds, _targetBoundingRect, 0.02f);

            if (isSlightlyDifferent)
            {
                // 如果差异在 [2%, 8%] 之间，说明不是纯随机噪点跳动，而可能是缓慢位移，我们用极小的平滑慢慢趋近，防止累积误差
                _targetBoundingRect = new Rect(
                    Mathf.Lerp(_targetBoundingRect.x, bounds.x, 0.01f),
                    Mathf.Lerp(_targetBoundingRect.y, bounds.y, 0.01f),
                    Mathf.Lerp(_targetBoundingRect.width, bounds.width, 0.01f),
                    Mathf.Lerp(_targetBoundingRect.height, bounds.height, 0.01f)
                );
            }
            // 如果差异小于 2%，_targetBoundingRect 不发生任何改变，画面彻底静止。
        }

        _lastBoundingRect = bounds;
        return _targetBoundingRect;
    }


    private Rect CalculateBounds(List<Rect> rects)
    {
        if (rects.Count == 1)
            return rects[0];

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var rect in rects)
        {
            float right = rect.x + rect.width;
            float bottom = rect.y + rect.height;

            minX = Mathf.Min(minX, rect.x);
            minY = Mathf.Min(minY, rect.y);
            maxX = Mathf.Max(maxX, right);
            maxY = Mathf.Max(maxY, bottom);
        }

        // 确保有效值
        minX = Mathf.Clamp01(minX);
        minY = Mathf.Clamp01(minY);
        maxX = Mathf.Clamp01(maxX);
        maxY = Mathf.Clamp01(maxY);

        float width = Mathf.Max(maxX - minX, 0.001f);
        float height = Mathf.Max(maxY - minY, 0.001f);

        return new Rect(minX, minY, width, height);
    }

    private Rect ApplyPadding(Rect bounds, float padding)
    {
        float minX = Mathf.Clamp01(bounds.x - padding);
        float minY = Mathf.Clamp01(bounds.y - padding);
        float maxX = Mathf.Clamp01(bounds.x + bounds.width + padding);
        float maxY = Mathf.Clamp01(bounds.y + bounds.height + padding);

        float width = Mathf.Max(maxX - minX, 0.001f);
        float height = Mathf.Max(maxY - minY, 0.001f);

        return new Rect(minX, minY, width, height);
    }


    private bool ShouldUseLastFrame(Rect current, Rect last, float threshold)
    {
        float deltaX = Mathf.Abs(current.x - last.x);
        float deltaY = Mathf.Abs(current.y - last.y);
        float deltaWidth = Mathf.Abs(current.width - last.width);
        float deltaHeight = Mathf.Abs(current.height - last.height);

        return deltaX < threshold && deltaY < threshold &&
               deltaWidth < threshold && deltaHeight < threshold;
    }


    /// <summary>
    /// 获取当前优选人物的ID列表（只读）
    /// </summary>
    public IReadOnlyList<int> GetPreferredPersonIds() => _preferredPersonIds;

    /// <summary>
    /// 获取当前优选人物的边界框列表（只读）
    /// </summary>
    public IReadOnlyList<Rect> GetPreferredPersonRects() => _preferredRects;

    /// <summary>
    /// 获取指定优选人物的边界框
    /// </summary>
    public Rect GetPreferredPersonRect(int index)
    {
        if (index >= 0 && index < _preferredRects.Count)
            return _preferredRects[index];

        return new Rect(0, 0, 1, 1); // 返回最大Rect作为默认值
    }



    /// <summary>
    /// 获取当前人物总数
    /// </summary>
    public int GetPersonCount() => _personList.Count;

    /// <summary>
    /// 获取指定ID的人物信息
    /// </summary>
    public bool TryGetPersonInfo(int playerId, out Rect rect, out float score)
    {
        if (_personDict.TryGetValue(playerId, out PersonInfo info))
        {
            rect = info.normalizedRect;
            score = info.score;
            return true;
        }

        rect = new Rect(0, 0, 1, 1);
        score = 0;
        return false;
    }

    /// <summary>
    /// 清除所有人物数据
    /// </summary>
    public void ClearAllPersons()
    {
        _personDict.Clear();
        _personList.Clear();
        _preferredPersonIds.Clear();  // 添加
        _preferredRects.Clear();      // 添加
        _tempBestPersons.Clear();     // 添加
    }

    /// <summary>
    /// 移除指定ID的人物
    /// </summary>
    public bool RemovePerson(int playerId)
    {
        if (!_personDict.ContainsKey(playerId))
            return false;

        _personList.RemoveAll(p => p.playerId == playerId);
        _personDict.Remove(playerId); // 添加这一行
        return true;
    }
}
