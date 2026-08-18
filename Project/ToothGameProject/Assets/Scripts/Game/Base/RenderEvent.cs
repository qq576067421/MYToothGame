
using LCL;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUI;
namespace GameDll
{
    public class CameraShakeParam
    {
        public float m_KeepTime;
        public float m_ShakeInterval;
        public float m_ShakeOffsetX;
        public float m_ShakeOffsetY;
        public float m_ShakeOffsetZ;
        public float m_SlowDown;
    }

    public delegate bool BattleStartupRequestHandler(BattleStartupRequest request, out string error);

    public class RenderEvent
    {
        private static RenderEvent m_Instance = new RenderEvent();
        //放弃实时new的原因是有时候有些地方会提前就注册了事件，不好评估，所以暂时把居于表现的事件弄成了持久的了
        //public static void CreateRenderEvent()
        //{
        //    m_Instance = new RenderEvent();
        //}
        public static RenderEvent Event
        {
            get
            {
                return m_Instance;
            }
        }



        public Func<string, string, string> OnCallFunction = (_func, _param) => string.Empty;
        public Action<BattleResultData> OnBattleResult = (result) => { };
        public Action<float> OnBattlePrepareTimeChanged = (time) => { };
        public Action OnPreStart = () => { };
        // 正式战斗暂停请求，由战斗界面使用，不用于 GM 调试冻结。
        public Func<bool> OnTowerDefendPauseRequest = () => false;
        // 正式战斗暂停状态设置和查询，供界面或外部输入直接调用。
        public Func<bool, bool> OnTowerDefendBattlePauseStateRequest = (pause) => false;
        public Func<bool> OnTowerDefendBattlePauseStateQuery = () => false;
        public Action<int, int> OnTowerDefendBaseHealthChanged = (current, max) => { };
        public Action<int, int, int, float> OnTowerDefendWaveStateChanged = (currentWave, maxWave, aliveMonsterCount, wait) => { };
        public Action OnTowerDefendBattleStateChanged = () => { };
        public Action<int> OnTowerDefendPlayerSkillEnergyFullStateChanged = (seatId) => { };
        public Action<int, BoneParserPlayerResult, int> OnTowerDefendBoneDebugInfoChanged = (seatId, playerResult, sdkSlotIndex) => { };
        public Action OnTowerDefendBoneDebugInfosCleared = () => { };
        public Action<PropertyEntity> OnTowerDefendMonsterDeathStarsEffect = (defender) => { };
        public Action OnTowerDefendBattleHudOpenRequest = () => { };
        public Action OnTowerDefendBattleHudCloseRequest = () => { };
        public Func<Action, Action, Action, object> OnTowerDefendPauseOpenRequest = (onResume, onRestart, onReturn) => null;
        public Action<object> OnTowerDefendPauseCloseRequest = (pauseWindow) => { };
        public Action OnTowerDefendRestartBattleRequest = () => { };
        public Action OnTowerDefendReturnLobbyRequest = () => { };
        public Action OnRenderPrepareConfirmRequest = () => { };
        public Action<InputAction.CallbackContext> OnRenderEscapePressedRequest = (context) => { };
        public Func<bool> OnRenderShouldIgnoreInputDuringLoading = () => false;
        public BattleStartupRequestHandler OnGmStartBattleRequest = DefaultGmStartBattleRequest;

        private static bool DefaultGmStartBattleRequest(BattleStartupRequest request, out string error)
        {
            error = "大厅流程未初始化。";
            return false;
        }


        public Action<int> OnNetStateChanged = (state) => { };
        public Action<int> OnFightNetStateChanged = (state) => { };

        public Action OnCameraDirty = () => { };

        public Action<int, int> OnChangeSceneResult = (rst, sceneId) => { };

        public Action<GameObject> OnQualityCamera = (cameraGo) => { };
        public Action OnGameResult = () => { };
        public Action<int, int> OnWellDead = (wellId, attackId) => { };
        public Action<int, int> OnTriggerJiGuan = (jiguanId, attackId) => { };
        public Action<int, int> OnKillMonster = (entityId, killerId) => { };
        public Action<int> OnTraitorDead = (entityId) => { };
        public Action<int, int> OnHeroDead = (deadUnitId, attackerId) => { };
        public Action<int> OnLevelUp = (entityId) => { };
        public Action<int> OnEntityAliveable = (entityId) => { };
        public Action<CameraShakeParam> OnCameraShake = (param) => { };
        public Action<int, bool> KeepSkill = (entityId, show) => { };
        public Action<int> AddMiniMap = (entityId) => { };
        public Action<int> RemoveMiniMap = (entityId) => { };

        public Action<int> TellBattleInfoKillMonster = (count) => { };



        public Action<int, int> OnLowFPSWarning = (fps, limit) => { };
        public Action<float, string> OnLoadingProChanged = (pro, info) => { };

        public Action<bool> OnSkillIndicatorRed = (bool red) => { };

        public Action<int> OnSkillCastFinish = (int entId) => { };




        public Action<int, int> OnClickMiniMapNode = (int entityType, int entId) => { };

        public Action<string> OnShowTipLanId = (string lanId) =>{};

        public Action<long> OnFixedUpdate = (dt) => { };
        public Action<int> OnBattleBagUpdateAllItem = (entityId) => { };
        public Action<int, int, long> OnBattleBagUpdateItem = (entityId, slot, itemCfgId) => { };
        public Action<int, int, long> OnHeroEquipChangeItem = (entityId, slot, itemCfgId) => { };
        public Action<int, bool> OnThreeOneChange = (entityId, isGroup) => { };
        public Action<int, int> OnBattleAddExpLevelChanged = (entityId, level) => { };
        public Action<int, int> OnBattleAddExpSkillPointChanged = (entityId, point) => { };
        public Action<int, int> OnBattleAddExpTalentPointChanged = (entityId, point) => { };
        public Action<Entity> OnBossHealthChanged = (entity) => { };
        public Action<int, int,bool> OnLollipopHealthChanged = (maxHealth, currentHealth,isShowBlood) => { };
        public Action<int> OnSaveTowerPosition = (entId) => { };




        public Action OnGameFightAgain = () => { };
        public Action OnStartUpgradeChallenge = () => { };
        public Action OnFinishUpgradeChallenge = () => { };
        public Action OnAddTeamExp = () => { };
        public Action OnUpdateSelectionVisuals = () => { };
        public Action<PropertyEntity,int> ShowRewardCoin = (propertyEntity,rewardCoin) => { };
        public Action<long,long> OnChangeFrameTest = (send_recevie_time, run_cmd_time) => { };
        public Action<string,string, bool> OnLoginBySDK = (string user_id, string pass_wd, bool hr) => { };

        public Action<string> OnProductInfoFail = (error_info) => { };
        public Action<string> OnProductBuyComplete = (productId) => { };
        public Action<string> OnBuyProductCancled = (productId) => { };
        public Action<string> OnRestoreCompleted = (productId) => { };
        public Action<bool> OnBillingSetupFinished = (isOk) => { };
        public Action<string,string> OnBuyProductFail = (productId, error) => { };
        public Action<string,string> OnPlatformMessageReceived = (cmd, value) => { };
        public Action<int> OnLoadedTowerBuilding = (building_id) => { };
        public Action<string> OnClickUIHelper = (lanId) => { };
        public Action<ICoroutineHandler> OnAddCoroutinesGameObject = (mono) => { };
    }
}
