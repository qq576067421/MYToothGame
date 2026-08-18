using UnityEngine;
using System.Collections;
using GameDll;
using System;
using GameDll;

namespace GameDll
{

    public class CBattleLogic
    {
        private long m_LoginPlayerId;
        public void SetLoginPlayerId(long id)
        {
            m_LoginPlayerId = id;
        }
        public long GetLoginPlayerId()
        {
            return m_LoginPlayerId;
        }
        private IBattleScene m_Battle = null;
        public IBattleScene GetScene()
        {
            return m_Battle;
        }
        public void SetScene(IBattleScene scene)
        {
            m_Battle = scene;
        }
        private static CBattleLogic Instance;


        public static CBattleLogic GetInstance()
        {
            if (Instance == null)
            {
                Instance = new CBattleLogic();
            }
            return Instance;
        }


        public void CreateBattle(LevelInputData data)
        {
            IBattleScene scene = null;
            switch (data.m_BattleType)
            {
                case BattleType.TowerDefend:
                    {
                        scene = new TowerDefendBattleScene();
                        break;
                    }
            }
            if (scene != null)
            {
                scene.Init(data);
                SetScene(scene);
            }
            else
            {

            }
        }


        public bool IsOpenRecord()
        {
            if(m_Battle == null)
            {
                return false;
            }
            return m_Battle.IsOpenRecord();
        }
        public bool GetIsReplay()
        {
            if (m_Battle == null)
            {
                return false;
            }
            return m_Battle.IsReplay();
        }

        public void StartBattleLogic(long fightId)
        {
            //GameDll.BattleAPIBridge.Send_CS_BattleData(fightId, m_LoginPlayerId);
            //GameDll.BattleInMessage.GetInstance().Send_CS_BattleData(fightId, m_LoginPlayerId);
        }

        public void SetLight(float obj)
        {
            if(m_Battle != null)
            {
                m_Battle.SetLight(obj);
            }
        }

        public void StartLocalBattleLogic(long battleId)
        {

        }

        public void AddLevelScore(long playerId, int score)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            // 体感侧现在通过 Mono2GameDll 的 seat 接口接入，这里保留 playerId 直连入口。
            battle.AddUpgradeChallengeScore(playerId, score);
        }

        public void AddLevelScoreBySeat(int seatId, int score)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            var playerId = battle.ReadBattlePlayerIdBySeat(seatId);
            if (playerId <= 0)
            {
                return;
            }

            battle.AddUpgradeChallengeScore(playerId, score);
        }

        public bool CanAddLevelScore(long playerId)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return false;
            }

            return battle.CanAddUpgradeChallengeScore(playerId);
        }

        public bool CanAddLevelScoreBySeat(int seatId)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return false;
            }

            return battle.CanAddUpgradeChallengeScoreBySeat(seatId);
        }

        public bool IsUpgradeChallengeActive()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            return battle != null && battle.ReadIsUpgradeChallengeActive();
        }

        public bool RequestTowerDefendPause()
        {
            return RenderEvent.Event.OnTowerDefendPauseRequest();
        }

        public bool SetTowerDefendBattlePause(bool pause)
        {
            return RenderEvent.Event.OnTowerDefendBattlePauseStateRequest(pause);
        }

        public bool ResumeTowerDefendBattle()
        {
            return SetTowerDefendBattlePause(false);
        }

        public bool IsTowerDefendBattlePaused()
        {
            return RenderEvent.Event.OnTowerDefendBattlePauseStateQuery();
        }

        public bool GM_SetTowerDefendPause(bool pause)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return false;
            }

            battle.GM_SetPause(pause);
            return true;
        }

        public bool GM_IsTowerDefendPaused()
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            return battle != null && battle.GM_IsPause();
        }

        public int GetLevelScore(long playerId)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return 0;
            }

            return battle.GetUpgradeChallengeScore(playerId);
        }

        public bool TryPlayerNormalAttackBySeat(int seatId, Vector3 faceForward, Vector3 moveDir)
        {
            return TryPlayerActionBySeat(seatId, 0, faceForward, moveDir);
        }

        public bool TryPlayerSkillBySeat(int seatId, Vector3 faceForward, Vector3 moveDir)
        {
            return TryPlayerSkillBySeat(seatId, 1, faceForward, moveDir);
        }

        public bool TryPlayerSkillBySeat(int seatId, int slot, Vector3 faceForward, Vector3 moveDir)
        {
            return TryPlayerActionBySeat(seatId, slot, faceForward, moveDir);
        }

        public bool TryPlayerActionBySeat(int seatId, int slot, Vector3 faceForward, Vector3 moveDir)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return false;
            }

            var spawer = battle.ReadBattleSpawer() as TowerDefendBattleSpawer;
            if (spawer == null)
            {
                return false;
            }

            return spawer.TryGuardHeroActionBySeat(seatId, slot, faceForward, moveDir);
        }
        public void StopBattleLogic()
        {
            if(m_Battle != null)
            {
                m_Battle.Destroy();
                m_Battle = null;
            }
        }

        public void Destroy()
        {
            UDebug.Log("start CBattleLogic Destroy");
            StopBattleLogic();
            UDebug.Log("end CBattleLogic Destroy");
        }

        // Update is called once per frame
        public void Update()
        {
            if (m_Battle != null)
            {
                float deltaTime = Time.deltaTime;
                m_Battle.Update(deltaTime);
                //CGameInput.Update();
            }
        }
        public void LateUpdate()
        {
            if (m_Battle != null)
            {
                m_Battle.LateUpdate();
            }
        }
        public void OnDrawGizmos()
        {
            if (m_Battle != null)
            {
                m_Battle.OnDrawGizmos();
            }
        }
    }
}
