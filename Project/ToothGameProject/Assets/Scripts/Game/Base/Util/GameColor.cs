using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public class GameColor
    {
        public static string AddHpColor = "#00FF7F";
        public static string SkillSubHpColor = "#FF6347";
        public static string OtherSubHpColor = "#FFFFFF";

        public static string AddResColor = "#EEE5DE";
        public static string AddGoldColor = "#FFEA25";

        public static string AddMagicColor = "#00EEEE";
        public static string AddExpColor = "#ea66a6";

        public static string FriendGroup = "#47FF01";
        public static string EnemyGroup = "#FF012F";
        public static string HudName = "#FFFFFF";

        private static readonly string[] m_PlayerDamageColors =
        {
            "#FFE221",
            "#20F7FF",
            "#FF2041",
            "#20FF58",
        };

        public static string ReadPlayerDamageColor(int seatId)
        {
            if (seatId < 0 || seatId >= m_PlayerDamageColors.Length)
            {
                return null;
            }

            return m_PlayerDamageColors[seatId];
        }
    }
}
