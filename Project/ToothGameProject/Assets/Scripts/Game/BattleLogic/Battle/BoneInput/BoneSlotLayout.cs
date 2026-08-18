using UnityEngine;

namespace GameDll
{
    // 统一维护四个骨骼槽位的源分区，确保消费端分槽和调试显示使用同一套数据范围。
    public static class BoneSlotLayout
    {
        public const int m_SlotCount = 4;

        private static readonly Rect[] m_SourceUvRects =
        {
            new Rect(0.10f, 0.00f, 0.275f, 0.90f),
            new Rect(0.375f, 0.00f, 0.125f, 0.90f),
            new Rect(0.50f, 0.00f, 0.125f, 0.90f),
            new Rect(0.625f, 0.00f, 0.375f, 0.90f),
        };

        public static Rect ReadSourceUvRect(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_SourceUvRects.Length)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            return m_SourceUvRects[slotIndex];
        }

        // 战斗输入必须按准备界面选择的人数读取区域，不能始终使用四人固定分区。
        // 这里的数值需要和 PlayerMatchView.SetpersonPlayerReadyPartitionRectf 的准备区保持一致。
        public static bool TryReadPrepareUvRect(int playerCount, int slotIndex, out Rect rect)
        {
            rect = Rect.zero;
            if (slotIndex < 0 || slotIndex >= m_SlotCount)
            {
                return false;
            }

            playerCount = Mathf.Clamp(playerCount, 1, m_SlotCount);
            switch (playerCount)
            {
                case 1:
                    if (slotIndex == 0)
                    {
                        rect = new Rect(0.45f, 0.00f, 0.10f, 0.90f);
                        return true;
                    }
                    break;
                case 2:
                    if (slotIndex == 0)
                    {
                        rect = new Rect(0.55f, 0.00f, 0.20f, 0.90f);
                        return true;
                    }
                    if (slotIndex == 1)
                    {
                        rect = new Rect(0.25f, 0.00f, 0.20f, 0.90f);
                        return true;
                    }
                    break;
                case 3:
                    if (slotIndex == 0)
                    {
                        rect = new Rect(0.60f, 0.00f, 0.20f, 0.90f);
                        return true;
                    }
                    if (slotIndex == 1)
                    {
                        rect = new Rect(0.45f, 0.00f, 0.10f, 0.90f);
                        return true;
                    }
                    if (slotIndex == 2)
                    {
                        rect = new Rect(0.20f, 0.00f, 0.20f, 0.90f);
                        return true;
                    }
                    break;
                case 4:
                    return TryReadFourPlayerPrepareUvRect(slotIndex, out rect);
            }

            return false;
        }

        private static bool TryReadFourPlayerPrepareUvRect(int slotIndex, out Rect rect)
        {
            rect = Rect.zero;
            switch (slotIndex)
            {
                case 0:
                    rect = new Rect(0.625f, 0.00f, 0.375f, 0.90f);
                    return true;
                case 1:
                    rect = new Rect(0.50f, 0.00f, 0.125f, 0.90f);
                    return true;
                case 2:
                    rect = new Rect(0.375f, 0.00f, 0.125f, 0.90f);
                    return true;
                case 3:
                    rect = new Rect(0.10f, 0.00f, 0.275f, 0.90f);
                    return true;
                default:
                    return false;
            }
        }
    }
}
