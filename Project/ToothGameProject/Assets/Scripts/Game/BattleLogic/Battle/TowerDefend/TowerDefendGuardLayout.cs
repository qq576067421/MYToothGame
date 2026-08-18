using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    // 塔防守卫台子与英雄站位的居中布局工具。
    // 原始 4 个 td_guard 点在场景里等距铺开，少人局直接取"居中的索引"会产生偏移
    // （例如 1 人取 td_guard_2 会落在中心偏左半个间距处），这里改成基于几何中心
    // 重新计算位置，保证任意人数都真正居中。
    public static class TowerDefendGuardLayout
    {
        // 根据玩家数量，把守卫点重新居中分布，返回每个玩家应处的世界坐标。
        // guardPoints: 场景原始 td_guard 点位（顺序即 td_guard_1..N）。
        // playerCount: 本局玩家数。
        // playerIndex: 当前要算的玩家序号（0 起）。
        public static Vector3 ResolveCenteredPosition(IReadOnlyList<Vector3> guardPoints, int playerCount, int playerIndex)
        {
            if (guardPoints == null || guardPoints.Count <= 0)
            {
                return Vector3.zero;
            }

            int count = guardPoints.Count;
            if (count == 1 || playerCount <= 1)
            {
                // 单点或单人：统一取几何中心，避免台子非正中时角色偏台。
                return ResolveCentroid(guardPoints);
            }

            int effectiveCount = Mathf.Min(Mathf.Max(playerCount, 1), count);

            // 用首尾两点推算整体走向与单位间距，等距场景下等价于相邻点位差。
            var first = guardPoints[0];
            var last = guardPoints[count - 1];
            Vector3 span = last - first;
            float step = count > 1 ? 1.0f / (count - 1) : 0f;

            // 几何中心（首尾中点）。
            Vector3 center = first + span * 0.5f;

            // 把玩家均匀放在中心两侧：偏移 = (i - (N-1)/2) 个原始间距单位。
            float offsetUnits = playerIndex - (effectiveCount - 1) * 0.5f;
            return center + span * step * offsetUnits;
        }

        // 计算点位集合的几何中心（各分量平均）。
        public static Vector3 ResolveCentroid(IReadOnlyList<Vector3> guardPoints)
        {
            if (guardPoints == null || guardPoints.Count <= 0)
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < guardPoints.Count; i++)
            {
                sum += guardPoints[i];
            }
            return sum / guardPoints.Count;
        }
    }
}
