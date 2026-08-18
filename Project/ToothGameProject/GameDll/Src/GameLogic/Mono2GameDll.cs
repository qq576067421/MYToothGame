
using MonoBean;
using GameDll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameHot
{
    class Mono2GameDll
    {
        public static object Call(string func, params object[] datas)
        {
            if (func == "GameDll_UIManager_WindowOpen")
            {
                WindowBase parent = (WindowBase)datas[1];
                object[] leftObjects = null;
                if (datas.Length > 2)
                {
                    leftObjects = (object[])datas[2];
                }

                return UIManager.OpenWindow((string)datas[0], parent, leftObjects);
            }
            else if (func == "GameDll_UIManager_WindowClose")
            {
                UIManager.CloseWindow((WindowBase)datas[0]);
            }
            else if (func == "GameDll_TowerDefend_NormalAttackBySeat")
            {
                var seatId = Convert.ToInt32(datas[0]);
                var faceForward = datas.Length > 1 ? (Vector3)datas[1] : Vector3.forward;
                var moveDir = datas.Length > 2 ? (Vector3)datas[2] : Vector3.zero;
                return CBattleLogic.GetInstance().TryPlayerNormalAttackBySeat(seatId, faceForward, moveDir);
            }
            else if (func == "GameDll_TowerDefend_SkillAttackBySeat")
            {
                var seatId = Convert.ToInt32(datas[0]);
                int argIndex = 1;
                var slot = 1;
                if (datas.Length > 1 && !(datas[1] is Vector3))
                {
                    slot = Convert.ToInt32(datas[1]);
                    argIndex = 2;
                }

                var faceForward = datas.Length > argIndex ? (Vector3)datas[argIndex] : Vector3.forward;
                var moveDir = datas.Length > argIndex + 1 ? (Vector3)datas[argIndex + 1] : Vector3.zero;
                return CBattleLogic.GetInstance().TryPlayerSkillBySeat(seatId, slot, faceForward, moveDir);
            }
            else if (func == "GameDll_TowerDefend_RequestPause")
            {
                return CBattleLogic.GetInstance().RequestTowerDefendPause();
            }
            else if (func == "GameDll_TowerDefend_SetBattlePause")
            {
                var pause = ReadBoolArg(datas, 0, true);
                return CBattleLogic.GetInstance().SetTowerDefendBattlePause(pause);
            }
            else if (func == "GameDll_TowerDefend_ResumeBattle")
            {
                return CBattleLogic.GetInstance().ResumeTowerDefendBattle();
            }
            else if (func == "GameDll_TowerDefend_IsBattlePaused")
            {
                return CBattleLogic.GetInstance().IsTowerDefendBattlePaused();
            }
            else if (func == "GameDll_TowerDefend_GM_SetPause")
            {
                var pause = ReadBoolArg(datas, 0, true);
                return CBattleLogic.GetInstance().GM_SetTowerDefendPause(pause);
            }
            else if (func == "GameDll_TowerDefend_GM_IsPaused")
            {
                return CBattleLogic.GetInstance().GM_IsTowerDefendPaused();
            }
            else if (func == "GameDll_TowerDefend_IsUpgradeChallengeActive")
            {
                return CBattleLogic.GetInstance().IsUpgradeChallengeActive();
            }
            else if (func == "GameDll_TowerDefend_CanAddLevelScoreBySeat")
            {
                var seatId = Convert.ToInt32(datas[0]);
                return CBattleLogic.GetInstance().CanAddLevelScoreBySeat(seatId);
            }
            else if (func == "GameDll_TowerDefend_AddLevelScoreBySeat")
            {
                var seatId = Convert.ToInt32(datas[0]);
                var score = Convert.ToInt32(datas[1]);
                CBattleLogic.GetInstance().AddLevelScoreBySeat(seatId, score);
            }

            
            return null;
        }

        private static bool ReadBoolArg(object[] datas, int index, bool defaultValue)
        {
            if (datas == null || datas.Length <= index || datas[index] == null)
            {
                return defaultValue;
            }

            var value = datas[index];
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string stringValue && bool.TryParse(stringValue, out bool parsedValue))
            {
                return parsedValue;
            }

            return Convert.ToInt32(value) != 0;
        }
    }
}
