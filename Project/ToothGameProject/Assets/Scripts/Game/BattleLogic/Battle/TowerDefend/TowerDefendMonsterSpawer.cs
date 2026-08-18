using GameDll;
using DG.Tweening;
using LCL;
using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Rendering;
using UnityEngine;

namespace GameDll
{
    public enum MonsterType
    {
        Normal = 0,
        Elite = 1,
        Boss = 2,
    }

    public class SpawData
    {
        public long m_MonsterCfgId;
        public int m_Level;
        public long m_Num;
        public int m_BaseHpValue = 1;
        public MonsterType m_MonsterKind = MonsterType.Normal;
    }
    public class TowerDefendSpawGroup
    {
        public List<SpawData> m_SpawData;
        public MonsterType m_MonsterKind = MonsterType.Normal;
        public List<int> m_RuntimeLaneIndices;
        public int m_RuntimeLaneCursor = 0;
        public bool m_UseBatchedSpawn = false;
        //种类序号
        public int m_RuntimeSpawDataIndex = 0;
        //某种的序号
        public int m_RuntimeSpawIndex = 0;
    }

    // TowerDefendMonsterSpawer: 负责塔防模式中的刷怪器逻辑。
    // 职责包括：解析关卡怪物池、推进波次、管理场上怪物实体、记录怪物种类/阵营/主题信息，
    // 并向上层战斗暴露运行时查询接口（例如当前存活怪物数、是否为 Boss 波等）。
    public class TowerDefendMonsterSpawer
    {
        private const float m_MonsterPresentationFadeDuration = 1.0f;
        private const int m_MonsterReachBaseDisappearEffectId = 81;
        private const int m_MonsterReachBaseDisappearSoundId = 103;
        private const float m_MonsterReachBaseDisappearEffectDuration = 2.0f;
        private static readonly int m_MonsterPresentationColorPropertyId = Shader.PropertyToID("_BaseColor");

        private sealed class MonsterPresentationFadeRuntime
        {
            public Tween m_Tween;
            public float m_CurrentAlpha = 1.0f;
        }

        private sealed class MonsterLanePathRuntime
        {
            public Vector3 m_SpawnPoint = Vector3.zero;
            public Vector3 m_EndPoint = Vector3.zero;
            public bool m_HasEndPoint = false;
        }

        private struct MonsterRecycleKey : IEquatable<MonsterRecycleKey>
        {
            public long m_MonsterCfgId;
            public MonsterType m_MonsterKind;

            public MonsterRecycleKey(long monsterCfgId, MonsterType monsterKind)
            {
                m_MonsterCfgId = monsterCfgId;
                m_MonsterKind = monsterKind;
            }

            public bool Equals(MonsterRecycleKey other)
            {
                return m_MonsterCfgId == other.m_MonsterCfgId &&
                    m_MonsterKind == other.m_MonsterKind;
            }

            public override bool Equals(object obj)
            {
                return obj is MonsterRecycleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (m_MonsterCfgId.GetHashCode() * 397) ^ (int)m_MonsterKind;
                }
            }
        }

        private const int m_DefaultEndlessDisplayWaveCount = 999;
        private const int m_DefaultChapterBaseMonsterCount = 10;
        private const float m_DefaultWaveIntervalSec = 20.0f;
        private const int m_EndlessEliteWaveInterval = 5;
        private const int m_EndlessBossWaveInterval = 10;
        private const float m_EndlessMoveSpeedGrowthInterval = 180.0f;
        private const int m_EndlessMoveSpeedGrowthPermillePerStep = 100;
        private const int m_EndlessMoveSpeedGrowthMaxStep = 10;
        private const int m_WaveBatchSpawnMinCount = 2;
        private const int m_WaveBatchSpawnMaxCount = 3;
        private const int m_WaveBatchLaneGap = 2;
        private const int m_PreferredNormalLaneOccupancy = 3;
        private const float m_MinMonsterReachRadius = 0.8f;
        private const float m_MonsterPreloadMaxWaitSeconds = 8.0f;
        private float m_WaitSpawWaveTime = 0;
        private int m_CurWave = 0;
        private int m_NextWave = 1;

        private int m_MaxVave = 100;
        private int m_CurrentWaveMonsterPoolIndex = -1;
        private List<Vector3> m_MonsterSpawers = new List<Vector3>();
        private readonly List<MonsterLanePathRuntime> m_MonsterLanePaths = new List<MonsterLanePathRuntime>();
        private Vector3 m_BasePoint = Vector3.zero;
        private float m_BaseReachRadius = 1.5f;

        private int m_MaxMonsterCount = 50;
        public void SetMaxMonsterCount(int count)
        {
            m_MaxMonsterCount = count;
        }
        public int ReadMaxMonsterCount()
        {
            return m_MaxMonsterCount;
        }

        private List<PropertyEntity> m_Monsters = new List<PropertyEntity>();
        private readonly Dictionary<int, MonsterType> m_MonsterKinds = new Dictionary<int, MonsterType>();
        private readonly Dictionary<int, int> m_MonsterLaneIndices = new Dictionary<int, int>();
        private readonly Dictionary<int, MonsterPresentationFadeRuntime> m_MonsterPresentationFadeRuntimes = new Dictionary<int, MonsterPresentationFadeRuntime>();
        private readonly Dictionary<MonsterRecycleKey, Queue<PropertyEntity>> m_RecycledMonsters = new Dictionary<MonsterRecycleKey, Queue<PropertyEntity>>();
        private readonly HashSet<int> m_ReadyRecycleMonsterIds = new HashSet<int>();
        private int m_MonsterPreloadSessionId = 0;
        private int m_MonsterPreloadTotalCount = 0;
        private int m_MonsterPreloadLoadedCount = 0;
        private float m_MonsterPreloadStartRealtime = 0.0f;
        private bool m_IsMonsterPreloadFinished = true;
        private int m_LastSpecialMonsterLaneIndex = -1;
        private bool m_MonsterVisible = true;
        private bool m_MonsterCanBeTarget = true;
        private bool m_IsMonsterSuspended = false;
        private bool m_LastWaveSpawed = false;
        private bool m_SendFinish = false;
        private bool m_CurrentWaveHasElite = false;
        private bool m_CurrentWaveHasBoss = false;

        private List<TowerDefendSpawGroup> m_Groups = new List<TowerDefendSpawGroup>();
        private int m_WaveIndex = 0;
        private int m_RuntimeQueuedBatchSpawnRemain = 0;
        private float m_RuntimeQueuedNextSpawnDelay = 0f;
        private readonly List<int> m_RuntimeQueuedBatchLaneIndices = new List<int>();

        private TowerDefendStageConfigAdapter m_EntryStageCfg;
        private TowerDefendStageConfigAdapter m_ActiveStageCfg;
        private TowerDefendBattle m_Battle;
        private BattleGameMode m_GameMode = BattleGameMode.Chapter;
        private int m_StageId = 1;
        // 返回当前场上活跃的怪物数量（不含已销毁或死亡的实体）。
        public int ReadMonsterCount()
        {
            return m_Monsters.Count;
        }
        public bool ReadIsMonsterPreloadFinished()
        {
            TryFinishMonsterPreloadByTimeout();
            return m_IsMonsterPreloadFinished;
        }
        // 返回配置或运行期计算出的最大波次。
        public int ReadMaxWave()
        {
            return m_MaxVave;
        }
        // 当前新关卡模型下不再单独区分“Boss 波”状态。
        public bool ReadIsBossWave()
        {
            return m_CurrentWaveHasBoss;
        }
        // 当前新关卡模型下不再单独区分“精英波”状态。
        public bool ReadIsEliteWave()
        {
            return m_CurrentWaveHasElite;
        }
        // 返回当前激活怪物池的槽位 ID。
        public int ReadCurrentMonsterPoolId()
        {
            return m_ActiveStageCfg != null ? m_ActiveStageCfg.MonsterPoolId : 0;
        }
        // 返回当前激活怪物池来源的 stageId。
        public int ReadCurrentMonsterPoolStageId()
        {
            return m_ActiveStageCfg != null ? m_ActiveStageCfg.MonsterPoolSourceStageId : m_StageId;
        }
        public int ReadMonsterKillRewardExp()
        {
            return m_ActiveStageCfg != null ? Mathf.Max(0, m_ActiveStageCfg.RewardExp) : 0;
        }
        public int ReadMonsterKillRewardCoin()
        {
            return m_ActiveStageCfg != null ? Mathf.Max(0, m_ActiveStageCfg.RewardCoin) : 0;
        }
        // 新关卡模型下不再运行时切换怪物池，这里固定返回 0。
        public float ReadMonsterPoolSwitchLeft()
        {
            return 0;
        }
        // 返回指定实体对应的运行时怪物种类（Normal/Elite/Boss）。
        // 优先从运行期记录中读取，若缺失则通过实体属性回退判定。
        public MonsterType ReadMonsterKind(int entityId)
        {
            MonsterType k;
            if (m_MonsterKinds.TryGetValue(entityId, out k))
            {
                return k;
            }

            // 回退为普通，避免外部在实体尚未登记时抛出异常
            return MonsterType.Normal;
        }
        // 返回怪物当前分配到的车道索引（用于 AI 选择与表现分区）。
        public int ReadMonsterLaneIndex(int entityId)
        {
            int idx;
            if (m_MonsterLaneIndices.TryGetValue(entityId, out idx))
            {
                return idx;
            }
            return -1;
        }
        // 根据实体当前位置返回最近的锁敌区域索引（不依赖朝向），
        // 用于自动攻击场景下的“本轨道优先、邻轨道次之”目标选择。
        public int ResolveNearestRegionIndex(Vector3 attackerPos)
        {
            if (m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return -1;
            }

            attackerPos.y = 0f;
            int regionCount = ResolveRegionCount();
            int bestRegionIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < regionCount; i++)
            {
                var referencePoint = ResolveRegionReferencePoint(i);
                referencePoint.y = 0f;
                var distance = Vector3.Distance(attackerPos, referencePoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRegionIndex = i;
                }
            }

            return bestRegionIndex;
        }

        // 将当前还活着的怪物实体复制到输出列表（用于统计与 UI 表示）。
        public void ReadAliveMonsters(List<PropertyEntity> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            int monsterCount = m_Monsters.Count;
            for (int i = 0; i < monsterCount; i++)
            {
                var monster = m_Monsters[i];
                if (BattleManager.ReadIsEntityValide(monster))
                {
                    output.Add(monster);
                }
            }
        }

        // 控制场上怪物的可见性、是否可选中以及是否暂停（用于升级挑战期间冻结怪物与刷怪节奏）。
        public void SetMonsterPresentation(bool visible, bool canBeTarget, bool suspend)
        {
            bool needReseedAnimationPhase = m_IsMonsterSuspended && !suspend && !m_MonsterVisible && visible;
            m_MonsterVisible = visible;
            m_MonsterCanBeTarget = canBeTarget;
            m_IsMonsterSuspended = suspend;
            int monsterCount = m_Monsters.Count;
            for (int i = monsterCount - 1; i >= 0; i--)
            {
                if (TryCleanupMonsterPresentationState(i, false))
                {
                    continue;
                }

                ApplyMonsterPresentation(m_Monsters[i]);
            }

            if (needReseedAnimationPhase)
            {
                RefreshMonsterAnimationPhaseAfterRestore();
            }
        }

        // 注入场景点位：出生点列表、基地位置与基地检测半径。
        // 调用时机：场景加载完成后由 Level 层传入。
        public void ConfigureScenePoints(List<TowerDefendMonsterPathPointData> monsterPathPoints, Vector3 basePoint, float baseReachRadius)
        {
            m_MonsterSpawers.Clear();
            m_MonsterLanePaths.Clear();
            if (monsterPathPoints != null && monsterPathPoints.Count > 0)
            {
                for (int i = 0; i < monsterPathPoints.Count; i++)
                {
                    var pointData = monsterPathPoints[i];
                    if (pointData == null)
                    {
                        continue;
                    }

                    m_MonsterSpawers.Add(pointData.m_SpawnPoint);
                    var runtime = new MonsterLanePathRuntime();
                    runtime.m_SpawnPoint = pointData.m_SpawnPoint;
                    runtime.m_EndPoint = pointData.m_EndPoint;
                    runtime.m_HasEndPoint = pointData.m_HasEndPoint;
                    m_MonsterLanePaths.Add(runtime);
                }
            }

            m_BasePoint = basePoint;
            m_BaseReachRadius = Mathf.Max(m_MinMonsterReachRadius, baseReachRadius);
        }

        // 初始化刷怪器运行时状态并解析入口关卡配置。
        // 参数 stage 为入口关卡 ID（章节/无尽入口），用于选择初始主题池与规则。
        public void Init(int stage)
        {
            m_Battle = BattleManager.GetBattle() as TowerDefendBattle;
            m_GameMode = m_Battle != null ? m_Battle.ReadGameMode() : BattleGameMode.Chapter;
            m_StageId = Math.Max(1, stage);
            m_EntryStageCfg = TowerDefendStageConfigResolver.Resolve(stage, m_GameMode);
            Debug.Log(string.Format(
                "[塔防刷怪配置] 模式={0}({1})，入口关卡={2}，配置表={3}，怪物池来源关卡={4}",
                TowerDefendStageConfigResolver.GetModeDebugName(m_GameMode),
                (int)m_GameMode,
                m_StageId,
                TowerDefendStageConfigResolver.GetConfigTableName(m_GameMode),
                m_EntryStageCfg != null ? m_EntryStageCfg.MonsterPoolSourceStageId : 0));
            m_SendFinish = false;
            m_LastWaveSpawed = false;
            m_CurWave = 0;
            m_NextWave = 1;
            m_CurrentWaveMonsterPoolIndex = -1;
            m_WaveIndex = 0;
            m_Groups.Clear();
            m_RuntimeQueuedBatchSpawnRemain = 0;
            m_RuntimeQueuedNextSpawnDelay = 0f;
            m_RuntimeQueuedBatchLaneIndices.Clear();
            DestroyTrackedRecycleCandidates();
            ClearRecycledMonsterPool();
            ClearMonsterPresentationFadeRuntimes();
            m_Monsters.Clear();
            m_MonsterKinds.Clear();
            m_MonsterLaneIndices.Clear();
            m_CurrentWaveHasElite = false;
            m_CurrentWaveHasBoss = false;
            m_LastSpecialMonsterLaneIndex = -1;
            m_ActiveStageCfg = m_EntryStageCfg;
            m_MaxVave = ResolveMaxWaveCount();
            // 首波等待由喊话阶段独立消费，这里不再重复读取关卡首波延时。
            m_WaitSpawWaveTime = 0f;
            m_MonsterVisible = true;
            m_MonsterCanBeTarget = true;
            m_IsMonsterSuspended = false;
            PreloadCurrentStageMonsters();
        }

        public void OnRelease()
        {
            CancelMonsterPreloadTracking();
            DestroyTrackedRecycleCandidates();
            ClearRecycledMonsterPool();
            ClearMonsterPresentationFadeRuntimes();
            m_Monsters.Clear();
            m_MonsterKinds.Clear();
            m_MonsterLaneIndices.Clear();
            m_LastSpecialMonsterLaneIndex = -1;
        }

        public void Update(float dt)
        {
            if (m_SendFinish)
            {
                return;
            }

            var battle = BattleManager.GetBattle();
            bool gmPause = battle != null && battle.GM_IsPause();

            if (!m_IsMonsterSuspended && !gmPause)
            {
                UpdateWaveTimer(dt);
            }

            int monster_count = m_Monsters.Count;
            for (int i = monster_count - 1; i >= 0; --i)
            {
                if (TryCleanupMonsterPresentationState(i, !m_IsMonsterSuspended))
                {
                    continue;
                }

                if (!m_IsMonsterSuspended)
                {
                    UpdateMonsterAdvanceByLane(m_Monsters[i], dt);
                }
            }

            if (m_IsMonsterSuspended)
            {
                return;
            }

            if (!gmPause)
            {
                TrySpawnScheduledWave();
                UpdateQueuedSpawnGroups(dt);
            }

            TryFinishAfterAllMonstersCleared();
        }

        private bool TryCleanupMonsterPresentationState(int monsterIndex, bool allowReachBase)
        {
            if (monsterIndex < 0 || monsterIndex >= m_Monsters.Count)
            {
                return true;
            }

            var monster = m_Monsters[monsterIndex];
            if (monster == null || monster.ReadIsDestroy())
            {
                RemoveTrackedMonsterAt(monsterIndex, monster != null ? monster.ReadId() : 0);
                return true;
            }

            if (monster.ReadIsDead())
            {
                int entityId = monster.ReadId();
                if (!m_ReadyRecycleMonsterIds.Contains(entityId))
                {
                    RestoreMonsterPresentationForDead(monster);
                    return false;
                }

                RecycleDeadMonster(monster, entityId);
                RemoveTrackedMonsterAt(monsterIndex, entityId);
                return true;
            }

            if (allowReachBase && TryHandleMonsterReachBase(monster))
            {
                RemoveTrackedMonsterAt(monsterIndex, monster.ReadId());
                return true;
            }

            return false;
        }

        private void RemoveTrackedMonsterAt(int monsterIndex, int entityId)
        {
            RemoveMonsterPresentationFadeRuntime(entityId, false);
            m_ReadyRecycleMonsterIds.Remove(entityId);
            m_MonsterKinds.Remove(entityId);
            m_MonsterLaneIndices.Remove(entityId);
            m_Monsters.RemoveAt(monsterIndex);
        }

        private void OnMonsterDeadAnimationFinished(PropertyEntity monster)
        {
            if (monster == null || monster.ReadIsDestroy())
            {
                return;
            }

            PrepareMonsterForRecycle(monster, true);
            m_ReadyRecycleMonsterIds.Add(monster.ReadId());
        }

        private void RecycleDeadMonster(PropertyEntity monster, int entityId)
        {
            if (monster == null || monster.ReadIsDestroy())
            {
                return;
            }

            PrepareMonsterForRecycle(monster, false);
            EnqueueRecycledMonster(monster, entityId);
        }

        private void PrepareMonsterForRecycle(PropertyEntity monster, bool removeFromObjectManager)
        {
            if (monster == null || monster.ReadIsDestroy())
            {
                return;
            }

            if (removeFromObjectManager)
            {
                var objMgr = BattleManager.GetObjectManager();
                if (objMgr != null)
                {
                    objMgr.RemovePropertyEntity(monster, false);
                }
            }

            var render = monster.GetRender();
            if (render != null)
            {
                render.SetShowHud(false);
                render.DisableHudRender();
                render.ClearColorAlphaProperty(m_MonsterPresentationColorPropertyId);
            }

            monster.SetCanBeTarget(false);
            monster.SetCanBeHurt(false);
            monster.SetFreeze(false);
            monster.SetVisiable(false);
        }

        private void EnqueueRecycledMonster(PropertyEntity monster, int entityId)
        {
            EnqueueRecycledMonster(monster, ReadMonsterKind(entityId));
        }

        private void EnqueueRecycledMonster(PropertyEntity monster, MonsterType monsterKind)
        {
            if (monster == null || monster.ReadIsDestroy())
            {
                return;
            }

            var key = new MonsterRecycleKey(monster.ReadBeanId(), monsterKind);
            Queue<PropertyEntity> queue;
            if (!m_RecycledMonsters.TryGetValue(key, out queue))
            {
                queue = new Queue<PropertyEntity>();
                m_RecycledMonsters.Add(key, queue);
            }

            queue.Enqueue(monster);
        }

        private bool TryTakeRecycledMonster(long monsterCfgId, MonsterType monsterKind, out PropertyEntity monster)
        {
            monster = null;
            var key = new MonsterRecycleKey(monsterCfgId, monsterKind);
            Queue<PropertyEntity> queue;
            if (!m_RecycledMonsters.TryGetValue(key, out queue))
            {
                return false;
            }

            while (queue.Count > 0)
            {
                var candidate = queue.Dequeue();
                if (candidate == null || candidate.ReadIsDestroy())
                {
                    continue;
                }

                if (candidate.ReadBeanId() != monsterCfgId)
                {
                    candidate.Destroy();
                    continue;
                }

                if (monsterKind == MonsterType.Boss && !candidate.ReadIsBoss())
                {
                    candidate.Destroy();
                    continue;
                }

                if (monsterKind != MonsterType.Boss && candidate.ReadIsBoss())
                {
                    candidate.Destroy();
                    continue;
                }

                monster = candidate;
                if (queue.Count <= 0)
                {
                    m_RecycledMonsters.Remove(key);
                }

                return true;
            }

            m_RecycledMonsters.Remove(key);
            return false;
        }

        private void PreloadCurrentStageMonsters()
        {
            BeginMonsterPreloadTracking();
            var stageCfg = ResolveActiveStageCfg();
            if (stageCfg == null)
            {
                FinishMonsterPreloadTracking();
                return;
            }

            var preloadSpawnDatas = ResolveFirstWavePreloadSpawnData(stageCfg);
            int preloadCount = CountPreloadMonsterCount(preloadSpawnDatas);
            if (preloadCount <= 0)
            {
                FinishMonsterPreloadTracking();
                return;
            }

            m_MonsterPreloadTotalCount = preloadCount;
            m_IsMonsterPreloadFinished = false;
            int sessionId = m_MonsterPreloadSessionId;
            int pooledCount = 0;
            for (int i = 0; i < preloadSpawnDatas.Count; i++)
            {
                var data = preloadSpawnDatas[i];
                if (data == null || data.m_MonsterCfgId <= 0)
                {
                    continue;
                }

                var key = new MonsterRecycleKey(data.m_MonsterCfgId, data.m_MonsterKind);
                int count = Mathf.Max(1, (int)data.m_Num);
                for (int j = 0; j < count; j++)
                {
                    if (PreloadMonsterForRecycle(key, sessionId))
                    {
                        pooledCount++;
                    }
                    else
                    {
                        MarkMonsterPreloadLoaded(sessionId);
                    }
                }
            }

            Debug.Log(string.Format(
                "[塔防怪物预热] 关卡={0} 模式={1} 第一波数量={2} 发起={3}",
                m_StageId,
                TowerDefendStageConfigResolver.GetModeDebugName(m_GameMode),
                preloadCount,
                pooledCount));
            TryFinishMonsterPreloadTracking();
        }

        private List<SpawData> ResolveFirstWavePreloadSpawnData(TowerDefendStageConfigAdapter stageCfg)
        {
            if (stageCfg == null)
            {
                return new List<SpawData>();
            }

            if (m_GameMode == BattleGameMode.Chapter)
            {
                if (stageCfg.ChapterWaveMonsterPools.Count <= 0)
                {
                    return new List<SpawData>();
                }

                // 预热只模拟第一波的怪物数量，不修改 m_CurrentWaveMonsterPoolIndex，避免影响正式刷怪前的运行状态。
                return ResolveConfiguredSpawnData(stageCfg.ChapterWaveMonsterPools[0], -1);
            }

            return ResolveConfiguredSpawnData(stageCfg.EndlessMonsterPool, -1);
        }

        private int CountPreloadMonsterCount(List<SpawData> preloadSpawnDatas)
        {
            if (preloadSpawnDatas == null || preloadSpawnDatas.Count <= 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < preloadSpawnDatas.Count; i++)
            {
                var data = preloadSpawnDatas[i];
                if (data == null || data.m_MonsterCfgId <= 0)
                {
                    continue;
                }

                count += Mathf.Max(1, (int)data.m_Num);
            }

            return count;
        }

        private bool PreloadMonsterForRecycle(MonsterRecycleKey key, int sessionId)
        {
            var cfg = t_monsterBean.GetConfig(key.m_MonsterCfgId, false);
            if (cfg == null)
            {
                Debug.LogWarning(string.Format("塔防怪物预热失败，找不到怪物配置，monsterCfgId={0}。", key.m_MonsterCfgId));
                return false;
            }

            var monster = CreateMonsterEntity(cfg, key.m_MonsterKind);
            if (monster == null)
            {
                return false;
            }

            // 预热实体只负责把模型和运行时对象提前创建好，不进入战斗列表，也不允许被锁定。
            // 后续真正刷出时仍会经过 ActivateMonster，重新设置位置、血量、技能和出生状态。
            PrepareMonsterForRecycle(monster, false);
            RegisterMonsterPreloadLoadedCallback(monster, sessionId);
            EnqueueRecycledMonster(monster, key.m_MonsterKind);
            return true;
        }

        private void BeginMonsterPreloadTracking()
        {
            m_MonsterPreloadSessionId++;
            m_MonsterPreloadTotalCount = 0;
            m_MonsterPreloadLoadedCount = 0;
            m_MonsterPreloadStartRealtime = Time.realtimeSinceStartup;
            m_IsMonsterPreloadFinished = true;
        }

        private void CancelMonsterPreloadTracking()
        {
            m_MonsterPreloadSessionId++;
            m_MonsterPreloadTotalCount = 0;
            m_MonsterPreloadLoadedCount = 0;
            m_MonsterPreloadStartRealtime = 0.0f;
            m_IsMonsterPreloadFinished = true;
        }

        private void FinishMonsterPreloadTracking()
        {
            m_MonsterPreloadLoadedCount = m_MonsterPreloadTotalCount;
            m_IsMonsterPreloadFinished = true;
        }

        private void RegisterMonsterPreloadLoadedCallback(PropertyEntity monster, int sessionId)
        {
            var render = monster != null ? monster.GetRender() : null;
            if (render == null)
            {
                MarkMonsterPreloadLoaded(sessionId);
                return;
            }

            monster.AddLoadedCall(() => MarkMonsterPreloadLoaded(sessionId));
            if (render.IsObjectLoaded())
            {
                MarkMonsterPreloadLoaded(sessionId);
            }
        }

        private void MarkMonsterPreloadLoaded(int sessionId)
        {
            if (sessionId != m_MonsterPreloadSessionId || m_IsMonsterPreloadFinished)
            {
                return;
            }

            m_MonsterPreloadLoadedCount = Mathf.Min(m_MonsterPreloadTotalCount, m_MonsterPreloadLoadedCount + 1);
            TryFinishMonsterPreloadTracking();
        }

        private void TryFinishMonsterPreloadTracking()
        {
            if (m_IsMonsterPreloadFinished)
            {
                return;
            }

            if (m_MonsterPreloadLoadedCount < m_MonsterPreloadTotalCount)
            {
                return;
            }

            m_IsMonsterPreloadFinished = true;
            Debug.Log(string.Format(
                "[塔防怪物预热] 关卡={0} 资源加载完成 {1}/{2}",
                m_StageId,
                m_MonsterPreloadLoadedCount,
                m_MonsterPreloadTotalCount));
        }

        private void TryFinishMonsterPreloadByTimeout()
        {
            if (m_IsMonsterPreloadFinished || m_MonsterPreloadTotalCount <= 0)
            {
                return;
            }

            if (Time.realtimeSinceStartup - m_MonsterPreloadStartRealtime < m_MonsterPreloadMaxWaitSeconds)
            {
                return;
            }

            Debug.LogWarning(string.Format(
                "[塔防怪物预热] 等待资源超时，关卡={0} 已加载={1}/{2}，继续进入战斗。",
                m_StageId,
                m_MonsterPreloadLoadedCount,
                m_MonsterPreloadTotalCount));
            FinishMonsterPreloadTracking();
        }

        private void ClearRecycledMonsterPool()
        {
            foreach (var pair in m_RecycledMonsters)
            {
                var queue = pair.Value;
                if (queue == null)
                {
                    continue;
                }

                while (queue.Count > 0)
                {
                    var monster = queue.Dequeue();
                    if (monster != null && !monster.ReadIsDestroy())
                    {
                        monster.Destroy();
                    }
                }
            }

            m_RecycledMonsters.Clear();
            m_ReadyRecycleMonsterIds.Clear();
        }

        private void DestroyTrackedRecycleCandidates()
        {
            int monsterCount = m_Monsters.Count;
            for (int i = 0; i < monsterCount; i++)
            {
                var monster = m_Monsters[i];
                if (monster == null || monster.ReadIsDestroy())
                {
                    continue;
                }

                if (!m_ReadyRecycleMonsterIds.Contains(monster.ReadId()) &&
                    (!monster.ReadIsDead() || BattleManager.ReadIsEntityValide(monster)))
                {
                    continue;
                }

                monster.Destroy();
            }
        }

        private bool OnSpawGroup(float dt, out bool deferToNextBatch)
        {
            deferToNextBatch = false;
            if (m_Groups == null || m_Groups.Count <= 0)
            {
                return false;
            }

            for (int groupIndex = 0; groupIndex < m_Groups.Count;)
            {
                var group = m_Groups[groupIndex];
                if (group == null || group.m_SpawData == null || group.m_RuntimeSpawDataIndex >= group.m_SpawData.Count)
                {
                    m_Groups.RemoveAt(groupIndex);
                    continue;
                }

                var data = group.m_SpawData[group.m_RuntimeSpawDataIndex];
                if (data == null || data.m_Num <= 0)
                {
                    group.m_RuntimeSpawIndex = 0;
                    group.m_RuntimeSpawDataIndex++;
                    m_Groups.RemoveAt(groupIndex);
                    if (group.m_RuntimeSpawDataIndex < group.m_SpawData.Count)
                    {
                        m_Groups.Add(group);
                    }
                    continue;
                }

                int laneIndex;
                if (!TryResolveSpawnLaneIndex(group, out laneIndex))
                {
                    groupIndex++;
                    continue;
                }

                var pos = m_MonsterSpawers[laneIndex];
                SpawMonster(pos, laneIndex, data, group.m_MonsterKind);
                m_RuntimeQueuedBatchLaneIndices.Add(laneIndex);

                group.m_RuntimeSpawIndex++;

                if (group.m_RuntimeSpawIndex >= data.m_Num)
                {
                    group.m_RuntimeSpawIndex = 0;
                    group.m_RuntimeSpawDataIndex++;
                }

                if (group.m_RuntimeSpawDataIndex >= group.m_SpawData.Count)
                {
                    m_Groups.RemoveAt(groupIndex);
                    return true;
                }

                // 同一波的待刷配置仍然保持轮转，避免某一种怪连续刷完。
                m_Groups.RemoveAt(groupIndex);
                m_Groups.Add(group);
                return true;
            }

            // 本批里只要已经找不到任何满足“至少隔一个轨道”的目标，
            // 就提前结束当前批次，把剩余怪保留到下一批再刷。
            deferToNextBatch = m_Groups.Count > 0;
            return false;
        }

        private void UpdateQueuedSpawnGroups(float dt)
        {
            if (m_Groups == null || m_Groups.Count <= 0)
            {
                return;
            }

            var group = m_Groups[0];
            if (group == null || !group.m_UseBatchedSpawn)
            {
                return;
            }

            m_RuntimeQueuedNextSpawnDelay = Mathf.Max(0f, m_RuntimeQueuedNextSpawnDelay - dt);
            if (m_RuntimeQueuedNextSpawnDelay > 0f)
            {
                return;
            }

            if (m_RuntimeQueuedBatchSpawnRemain <= 0)
            {
                m_RuntimeQueuedBatchSpawnRemain = ResolveRandomWaveBatchSpawnCount();
                m_RuntimeQueuedBatchLaneIndices.Clear();
            }

            while (m_RuntimeQueuedBatchSpawnRemain > 0 && m_Groups.Count > 0)
            {
                var runtimeGroup = m_Groups[0];
                if (runtimeGroup == null || !runtimeGroup.m_UseBatchedSpawn)
                {
                    break;
                }

                bool deferToNextBatch;
                if (OnSpawGroup(dt, out deferToNextBatch))
                {
                    m_RuntimeQueuedBatchSpawnRemain--;
                    continue;
                }

                if (deferToNextBatch)
                {
                    m_RuntimeQueuedBatchSpawnRemain = 0;
                }

                break;
            }

            if (m_Groups.Count > 0 && m_Groups[0] != null && m_Groups[0].m_UseBatchedSpawn)
            {
                m_RuntimeQueuedNextSpawnDelay = ReadWaveBatchSpawnIntervalSec();
                return;
            }

            m_RuntimeQueuedBatchSpawnRemain = 0;
            m_RuntimeQueuedNextSpawnDelay = 0f;
            m_RuntimeQueuedBatchLaneIndices.Clear();
        }



        public bool ReadLastWaveSpawed()
        {
            return m_LastWaveSpawed;
        }
        public float ReadWaitSpawWaveTime()
        {
            return Mathf.Max(0, m_WaitSpawWaveTime);
        }
        public int ReadWaveIndex()
        {
            return m_CurWave > 0 ? m_CurWave : Math.Max(1, m_NextWave);
        }
        public bool GM_TrySpawnNextWaveNow()
        {
            if (m_SendFinish ||
                m_IsMonsterSuspended ||
                m_LastWaveSpawed ||
                m_NextWave <= 0 ||
                m_Groups == null ||
                m_Groups.Count > 0)
            {
                return false;
            }

            m_WaitSpawWaveTime = 0f;
            TrySpawnScheduledWave();
            UpdateQueuedSpawnGroups(0f);
            return true;
        }

        public bool GM_TrySpawnBossNow()
        {
            if (!TryResolveGMBossSpawnData(out var bossData, out var sourceLabel))
            {
                return false;
            }

            SpawnConfiguredMonstersImmediately(new List<SpawData> { bossData }, false);
            Debug.Log(string.Format(
                "[GMTools] 直接刷 Boss 成功：关卡={0} 怪物池来源关卡={1} 来源={2} BossCfg={3} 基础血量={4}",
                m_StageId,
                m_ActiveStageCfg != null ? m_ActiveStageCfg.MonsterPoolSourceStageId : m_StageId,
                sourceLabel,
                bossData.m_MonsterCfgId,
                bossData.m_BaseHpValue));
            return true;
        }

        private void SpawWave()
        {
            if (m_EntryStageCfg == null || m_NextWave <= 0)
            {
                return;
            }

            var wave = m_NextWave;
            m_CurWave = wave;
            m_WaveIndex = wave;
            var waveSpawnDatas = ResolveRegularWaveSpawnData(wave);
            //LogWaveSpawnSummary(wave, waveSpawnDatas);
            EnqueueWaveSpawnGroups(waveSpawnDatas, true);

            if (HasNextWave(wave))
            {
                m_NextWave = wave + 1;
                m_WaitSpawWaveTime = ResolveWaveInterval();
            }
            else
            {
                m_NextWave = 0;
                m_WaitSpawWaveTime = 0;
                m_LastWaveSpawed = true;
            }
        }

        //private void LogWaveSpawnSummary(int wave, List<SpawData> spawDatas)
        //{
        //    var builder = new StringBuilder(512);
        //    builder.Append("[TDWaveDebug] 波次").Append(wave);
        //    builder.Append(" 模式=").Append(TowerDefendStageConfigResolver.GetModeDebugName(m_GameMode));
        //    builder.Append("(").Append((int)m_GameMode).Append(")");
        //    builder.Append(" 入口关卡=").Append(m_StageId);
        //    builder.Append(" 配置表=").Append(TowerDefendStageConfigResolver.GetConfigTableName(m_GameMode));
        //    builder.Append(" 怪物池来源关卡=").Append(m_ActiveStageCfg != null ? m_ActiveStageCfg.MonsterPoolSourceStageId : 0);
        //    builder.Append(" 波次怪物血量：");
        //    AppendWaveMonsterSummary(builder, spawDatas);
        //    AppendGuardHeroSummary(builder);
        //    Debug.Log(builder.ToString());
        //}

        //private void AppendWaveMonsterSummary(StringBuilder builder, List<SpawData> spawDatas)
        //{
        //    if (builder == null)
        //    {
        //        return;
        //    }

        //    if (spawDatas == null || spawDatas.Count == 0)
        //    {
        //        builder.Append("无");
        //        return;
        //    }

        //    for (int i = 0; i < spawDatas.Count; i++)
        //    {
        //        var data = spawDatas[i];
        //        if (data == null)
        //        {
        //            continue;
        //        }

        //        if (i > 0)
        //        {
        //            builder.Append("；");
        //        }

        //        var baseHpValue = Mathf.Max(1, data.m_BaseHpValue);
        //        var maxHpScalePermille = ResolveMonsterMaxHpScalePermille();
        //        builder
        //            .Append("怪物")
        //            .Append(i + 1)
        //            .Append("(cfg:")
        //            .Append(data.m_MonsterCfgId)
        //            .Append(", 数量:")
        //            .Append(Mathf.Max(1, (int)data.m_Num))
        //            .Append(", 种类:")
        //            .Append(data.m_MonsterKind)
        //            .Append(", 基础血量:")
        //            .Append(FormatWaveLogNumber(baseHpValue))
        //            .Append(", 最终血量:")
        //            .Append(FormatWaveLogNumber(ResolveWaveMonsterFinalHp(baseHpValue, maxHpScalePermille)))
        //            .Append(" = ")
        //            .Append(BuildWaveMonsterFinalHpFormula(baseHpValue, maxHpScalePermille))
        //            .Append(")");
        //    }
        //}

        //private void AppendGuardHeroSummary(StringBuilder builder)
        //{
        //    if (builder == null)
        //    {
        //        return;
        //    }

        //    var heroes = ReadGuardHeroesForWaveLog();
        //    if (heroes == null || heroes.Count == 0)
        //    {
        //        return;
        //    }

        //    int heroDisplayIndex = 0;
        //    for (int i = 0; i < heroes.Count; i++)
        //    {
        //        var hero = heroes[i];
        //        if (!BattleManager.ReadIsEntityValide(hero))
        //        {
        //            continue;
        //        }

        //        heroDisplayIndex++;
        //        builder
        //            .Append(" | 角色")
        //            .Append(heroDisplayIndex)
        //            .Append("：攻击")
        //            .Append(FormatWaveLogNumber(hero.GetAtk()))
        //            .Append(" 暴击")
        //            .Append(FormatWaveLogPercent(hero.ReadCritRatePermille()))
        //            .Append(" 暴击伤害")
        //            .Append(FormatWaveLogPercent(hero.ReadCritDamageScalePermille()))
        //            .Append(" 攻击速度")
        //            .Append(FormatWaveLogNumber(hero.GetNormalAtkSpeed()))
        //            .Append(" 伤害加深")
        //            .Append(FormatWaveLogPercent(hero.ReadDamageAmpPercent()));
        //    }
        //}

        private List<PropertyEntity> ReadGuardHeroesForWaveLog()
        {
            var battleSpawer = m_Battle != null ? m_Battle.ReadBattleSpawer() as TowerDefendBattleSpawer : null;
            return battleSpawer != null ? battleSpawer.ReadGuardHeroes() : null;
        }

        private float ResolveWaveMonsterFinalHp(SpawData data)
        {
            if (data == null)
            {
                return 0f;
            }

            return ResolveWaveMonsterFinalHp(Mathf.Max(1, data.m_BaseHpValue), ResolveMonsterMaxHpScalePermille());
        }

        private float ResolveWaveMonsterFinalHp(int baseHpValue, int maxHpScalePermille)
        {
            return Mathf.Max(1f, Mathf.Max(1, baseHpValue) * maxHpScalePermille / 1000f);
        }

        private int ResolveMonsterMaxHpScalePermille()
        {
            var activeStageCfg = ResolveActiveStageCfg();
            if (activeStageCfg == null)
            {
                return 1000;
            }

            int playerCount = ResolveBattlePlayerCount();
            int scalePermille;
            switch (playerCount)
            {
                case 1:
                    scalePermille = activeStageCfg.MonsterHpUp1;
                    break;
                case 2:
                    scalePermille = activeStageCfg.MonsterHpUp2;
                    break;
                case 3:
                    scalePermille = activeStageCfg.MonsterHpUp3;
                    break;
                default:
                    scalePermille = activeStageCfg.MonsterHpUp4;
                    break;
            }

            return scalePermille > 0 ? scalePermille : 1000;
        }

        private int ResolveBattlePlayerCount()
        {
            if (m_Battle == null)
            {
                return 1;
            }

            int playerCount = 0;
            for (int seatId = 0; seatId < TowerDefendSeatLayout.MaxSupportedPlayerCount; seatId++)
            {
                if (m_Battle.ReadBattlePlayerIdBySeat(seatId) > 0)
                {
                    playerCount++;
                }
            }

            return Mathf.Clamp(playerCount, 1, TowerDefendSeatLayout.MaxSupportedPlayerCount);
        }

        private string BuildWaveMonsterFinalHpFormula(int baseHpValue, int maxHpScalePermille)
        {
            return string.Format(
                "基础血量 {0} * 血量倍率 {1} / 1000",
                FormatWaveLogNumber(Mathf.Max(1, baseHpValue)),
                maxHpScalePermille);
        }

        private string FormatWaveLogNumber(float value)
        {
            return value.ToString("0.##");
        }

        private string FormatWaveLogPercent(float value)
        {
            return (value * 100f).ToString("0.##") + "%";
        }

        private bool TryResolveSpawnLaneIndex(TowerDefendSpawGroup group, out int laneIndex)
        {
            laneIndex = 0;
            var monsterKind = group != null ? group.m_MonsterKind : MonsterType.Normal;
            if (m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return false;
            }

            if (group != null &&
                group.m_RuntimeLaneIndices != null &&
                group.m_RuntimeLaneCursor < group.m_RuntimeLaneIndices.Count)
            {
                int plannedLaneIndex = group.m_RuntimeLaneIndices[group.m_RuntimeLaneCursor];
                if (!CanUseLaneInCurrentBatch(plannedLaneIndex))
                {
                    return false;
                }

                group.m_RuntimeLaneCursor++;
                laneIndex = plannedLaneIndex;
                return true;
            }

            // 按策划案约定，精英和 Boss 只从中路出生；当没有预计算分路时，回退到中路即时分配。
            if (monsterKind == MonsterType.Elite || monsterKind == MonsterType.Boss)
            {
                return TryResolveQueuedMiddleLaneSpawnIndex(out laneIndex);
            }

            return TryResolveQueuedLeastOccupiedLaneIndex(out laneIndex);
        }

        private bool TryResolveQueuedMiddleLaneSpawnIndex(out int laneIndex)
        {
            return TryResolveMiddleLaneSpawnIndexCore(true, out laneIndex);
        }

        private bool TryResolveImmediateMiddleLaneSpawnIndex(out int laneIndex)
        {
            return TryResolveMiddleLaneSpawnIndexCore(false, out laneIndex);
        }

        private bool TryResolveMiddleLaneSpawnIndexCore(bool respectCurrentBatchConstraint, out int laneIndex)
        {
            laneIndex = 0;
            int leftMiddleIndex;
            int rightMiddleIndex;
            ResolveMiddleLaneRange(out leftMiddleIndex, out rightMiddleIndex);
            if (leftMiddleIndex == rightMiddleIndex)
            {
                if (respectCurrentBatchConstraint && !CanUseLaneInCurrentBatch(leftMiddleIndex))
                {
                    return false;
                }

                laneIndex = leftMiddleIndex;
                return true;
            }

            var laneRuntimeCounts = new int[m_MonsterSpawers.Count];
            for (int i = 0; i < laneRuntimeCounts.Length; i++)
            {
                laneRuntimeCounts[i] = CountAliveMonstersOnLane(i);
            }

            bool canUseLeft = !respectCurrentBatchConstraint || CanUseLaneInCurrentBatch(leftMiddleIndex);
            bool canUseRight = !respectCurrentBatchConstraint || CanUseLaneInCurrentBatch(rightMiddleIndex);
            if (!canUseLeft && !canUseRight)
            {
                return false;
            }

            if (canUseLeft && !canUseRight)
            {
                laneIndex = leftMiddleIndex;
                return true;
            }

            if (canUseRight && !canUseLeft)
            {
                laneIndex = rightMiddleIndex;
                return true;
            }

            // 精英怪和 Boss 需要在跨批次、跨波次时也避免连续落到同一条中路轨道。
            // 因此这里在“当前批次约束”之外，再额外参考上一只特殊怪的实际轨道；
            // 只要另一条中路仍然可用，就优先切到另一条，避免前后两只连续重叠到同一路。
            if (m_LastSpecialMonsterLaneIndex == leftMiddleIndex)
            {
                laneIndex = rightMiddleIndex;
                return true;
            }

            if (m_LastSpecialMonsterLaneIndex == rightMiddleIndex)
            {
                laneIndex = leftMiddleIndex;
                return true;
            }

            if (laneRuntimeCounts[leftMiddleIndex] < laneRuntimeCounts[rightMiddleIndex])
            {
                laneIndex = leftMiddleIndex;
                return true;
            }

            if (laneRuntimeCounts[rightMiddleIndex] < laneRuntimeCounts[leftMiddleIndex])
            {
                laneIndex = rightMiddleIndex;
                return true;
            }

            laneIndex = UnityEngine.Random.Range(0, 2) == 1 ? rightMiddleIndex : leftMiddleIndex;
            return true;
        }

        private void ResolveMiddleLaneRange(out int leftMiddleIndex, out int rightMiddleIndex)
        {
            var spawnCount = m_MonsterSpawers != null ? m_MonsterSpawers.Count : 0;
            if (spawnCount <= 1)
            {
                leftMiddleIndex = 0;
                rightMiddleIndex = 0;
                return;
            }

            if (spawnCount >= 6)
            {
                leftMiddleIndex = spawnCount / 2 - 1;
                rightMiddleIndex = spawnCount / 2;
                return;
            }

            leftMiddleIndex = spawnCount / 2;
            rightMiddleIndex = leftMiddleIndex;
        }

        private bool TryResolveQueuedLeastOccupiedLaneIndex(out int laneIndex)
        {
            return TryResolveLeastOccupiedLaneIndexCore(true, out laneIndex);
        }

        private bool TryResolveImmediateLeastOccupiedLaneIndex(out int laneIndex)
        {
            return TryResolveLeastOccupiedLaneIndexCore(false, out laneIndex);
        }

        private bool TryResolveLeastOccupiedLaneIndexCore(bool respectCurrentBatchConstraint, out int laneIndex)
        {
            laneIndex = 0;
            var laneCount = m_MonsterSpawers.Count;
            if (laneCount <= 0)
            {
                return false;
            }

            if (laneCount == 1)
            {
                if (respectCurrentBatchConstraint && !CanUseLaneInCurrentBatch(0))
                {
                    return false;
                }

                laneIndex = 0;
                return true;
            }

            var laneRuntimeCounts = new int[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                laneRuntimeCounts[i] = CountAliveMonstersOnLane(i);
            }

            int bestRegionOccupancy = int.MaxValue;
            bool hasCandidate = false;
            int regionCount = ResolveRegionCount();
            for (int i = 0; i < regionCount; i++)
            {
                var regionOccupancy = ReadRegionOccupancy(i, laneRuntimeCounts);
                int regionLaneIndex;
                if (!TryResolvePreferredLaneIndexInRegion(
                    i,
                    laneRuntimeCounts,
                    regionOccupancy,
                    respectCurrentBatchConstraint,
                    out regionLaneIndex))
                {
                    continue;
                }

                if (!hasCandidate || regionOccupancy < bestRegionOccupancy)
                {
                    bestRegionOccupancy = regionOccupancy;
                    laneIndex = regionLaneIndex;
                    hasCandidate = true;
                }
            }

            return hasCandidate;
        }

        private int CountAliveMonstersOnLane(int laneIndex)
        {
            if (laneIndex < 0)
            {
                return 0;
            }

            int aliveCount = 0;
            int monsterCount = m_Monsters.Count;
            for (int i = 0; i < monsterCount; i++)
            {
                var monster = m_Monsters[i];
                if (!BattleManager.ReadIsEntityValide(monster))
                {
                    continue;
                }

                int currentLaneIndex;
                if (m_MonsterLaneIndices.TryGetValue(monster.ReadId(), out currentLaneIndex) && currentLaneIndex == laneIndex)
                {
                    aliveCount++;
                }
            }

            return aliveCount;
        }

        private int ResolveRegionCount()
        {
            return Mathf.Max(1, (m_MonsterSpawers.Count + 1) / 2);
        }

        private void ResolveRegionLaneRange(int regionIndex, out int leftLaneIndex, out int rightLaneIndex)
        {
            var maxLaneIndex = Mathf.Max(0, m_MonsterSpawers.Count - 1);
            leftLaneIndex = Mathf.Clamp(regionIndex * 2, 0, maxLaneIndex);
            rightLaneIndex = Mathf.Clamp(leftLaneIndex + 1, 0, maxLaneIndex);
        }

        private int ReadRegionOccupancy(int regionIndex, int[] laneRuntimeCounts)
        {
            int leftLaneIndex;
            int rightLaneIndex;
            ResolveRegionLaneRange(regionIndex, out leftLaneIndex, out rightLaneIndex);

            var leftCount = laneRuntimeCounts != null && leftLaneIndex < laneRuntimeCounts.Length
                ? laneRuntimeCounts[leftLaneIndex]
                : CountAliveMonstersOnLane(leftLaneIndex);
            if (leftLaneIndex == rightLaneIndex)
            {
                return leftCount;
            }

            var rightCount = laneRuntimeCounts != null && rightLaneIndex < laneRuntimeCounts.Length
                ? laneRuntimeCounts[rightLaneIndex]
                : CountAliveMonstersOnLane(rightLaneIndex);
            return leftCount + rightCount;
        }

        private bool TryResolvePreferredLaneIndexInRegion(
            int regionIndex,
            int[] laneRuntimeCounts,
            int regionOccupancy,
            bool respectCurrentBatchConstraint,
            out int laneIndex)
        {
            laneIndex = 0;
            int leftLaneIndex;
            int rightLaneIndex;
            ResolveRegionLaneRange(regionIndex, out leftLaneIndex, out rightLaneIndex);
            if (leftLaneIndex == rightLaneIndex)
            {
                if (respectCurrentBatchConstraint && !CanUseLaneInCurrentBatch(leftLaneIndex))
                {
                    return false;
                }

                laneIndex = leftLaneIndex;
                return true;
            }

            var leftCount = laneRuntimeCounts != null && leftLaneIndex < laneRuntimeCounts.Length
                ? laneRuntimeCounts[leftLaneIndex]
                : CountAliveMonstersOnLane(leftLaneIndex);
            var rightCount = laneRuntimeCounts != null && rightLaneIndex < laneRuntimeCounts.Length
                ? laneRuntimeCounts[rightLaneIndex]
                : CountAliveMonstersOnLane(rightLaneIndex);

            int firstLaneIndex;
            int secondLaneIndex;
            if (leftCount < rightCount)
            {
                firstLaneIndex = leftLaneIndex;
                secondLaneIndex = rightLaneIndex;
            }
            else if (rightCount < leftCount)
            {
                firstLaneIndex = rightLaneIndex;
                secondLaneIndex = leftLaneIndex;
            }
            else if ((regionOccupancy & 1) == 0)
            {
                firstLaneIndex = leftLaneIndex;
                secondLaneIndex = rightLaneIndex;
            }
            else
            {
                firstLaneIndex = rightLaneIndex;
                secondLaneIndex = leftLaneIndex;
            }

            if (!respectCurrentBatchConstraint || CanUseLaneInCurrentBatch(firstLaneIndex))
            {
                laneIndex = firstLaneIndex;
                return true;
            }

            if (!respectCurrentBatchConstraint || CanUseLaneInCurrentBatch(secondLaneIndex))
            {
                laneIndex = secondLaneIndex;
                return true;
            }

            return false;
        }

        private bool CanUseLaneInCurrentBatch(int laneIndex)
        {
            int batchLaneCount = m_RuntimeQueuedBatchLaneIndices.Count;
            for (int i = 0; i < batchLaneCount; i++)
            {
                if (Mathf.Abs(m_RuntimeQueuedBatchLaneIndices[i] - laneIndex) < m_WaveBatchLaneGap)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 ResolveRegionReferencePoint(int regionIndex)
        {
            if (m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return m_BasePoint;
            }

            int leftLaneIndex;
            int rightLaneIndex;
            ResolveRegionLaneRange(regionIndex, out leftLaneIndex, out rightLaneIndex);
            return (m_MonsterSpawers[leftLaneIndex] + m_MonsterSpawers[rightLaneIndex]) * 0.5f;
        }
        public void CreateMonsters(List<SpawData> spawDatas)
        {
            SpawnConfiguredMonstersImmediately(spawDatas, false);
        }

        private List<int> BuildNormalMonsterLanePlan(List<SpawData> spawDatas)
        {
            var lanePlan = new List<int>();
            if (spawDatas == null || spawDatas.Count == 0 || m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return lanePlan;
            }

            int totalSpawnCount = 0;
            int spawDataCount = spawDatas.Count;
            for (int i = 0; i < spawDataCount; i++)
            {
                totalSpawnCount += Mathf.Max(0, (int)spawDatas[i].m_Num);
            }

            var laneCount = m_MonsterSpawers.Count;
            var regionCount = ResolveRegionCount();
            var laneRuntimeCounts = new int[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                laneRuntimeCounts[i] = CountAliveMonstersOnLane(i);
            }

            var regionRuntimeCounts = new int[regionCount];
            for (int i = 0; i < regionCount; i++)
            {
                regionRuntimeCounts[i] = ReadRegionOccupancy(i, laneRuntimeCounts);
            }

            // 先按逻辑道路分散，再在道路内部选择左右子点，避免同一道两侧被误判为已分路。
            while (totalSpawnCount > 0)
            {
                bool assignedToPreferredOccupancy = false;
                for (int i = 0; i < regionCount && totalSpawnCount > 0; i++)
                {
                    if (regionRuntimeCounts[i] >= m_PreferredNormalLaneOccupancy)
                    {
                        continue;
                    }

                    int laneIndex;
                    if (!TryResolvePreferredLaneIndexInRegion(i, laneRuntimeCounts, regionRuntimeCounts[i], false, out laneIndex))
                    {
                        continue;
                    }
                    lanePlan.Add(laneIndex);
                    laneRuntimeCounts[laneIndex]++;
                    regionRuntimeCounts[i]++;
                    totalSpawnCount--;
                    assignedToPreferredOccupancy = true;
                }

                if (assignedToPreferredOccupancy)
                {
                    continue;
                }

                for (int i = 0; i < regionCount && totalSpawnCount > 0; i++)
                {
                    int laneIndex;
                    if (!TryResolvePreferredLaneIndexInRegion(i, laneRuntimeCounts, regionRuntimeCounts[i], false, out laneIndex))
                    {
                        continue;
                    }
                    lanePlan.Add(laneIndex);
                    laneRuntimeCounts[laneIndex]++;
                    regionRuntimeCounts[i]++;
                    totalSpawnCount--;
                }
            }

            return lanePlan;
        }

        private List<int> BuildMiddleMonsterLanePlan(List<SpawData> spawDatas)
        {
            var lanePlan = new List<int>();
            if (spawDatas == null || spawDatas.Count == 0 || m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return lanePlan;
            }

            int totalSpawnCount = 0;
            int spawDataCount = spawDatas.Count;
            for (int i = 0; i < spawDataCount; i++)
            {
                totalSpawnCount += Mathf.Max(0, (int)spawDatas[i].m_Num);
            }

            if (totalSpawnCount <= 0)
            {
                return lanePlan;
            }

            int leftMiddleIndex;
            int rightMiddleIndex;
            ResolveMiddleLaneRange(out leftMiddleIndex, out rightMiddleIndex);
            if (leftMiddleIndex == rightMiddleIndex)
            {
                for (int i = 0; i < totalSpawnCount; i++)
                {
                    lanePlan.Add(leftMiddleIndex);
                }

                return lanePlan;
            }

            var laneRuntimeCounts = new int[m_MonsterSpawers.Count];
            for (int i = 0; i < laneRuntimeCounts.Length; i++)
            {
                laneRuntimeCounts[i] = CountAliveMonstersOnLane(i);
            }

            int previousLaneIndex = m_LastSpecialMonsterLaneIndex;
            for (int i = 0; i < totalSpawnCount; i++)
            {
                int laneIndex;
                if (previousLaneIndex == leftMiddleIndex)
                {
                    laneIndex = rightMiddleIndex;
                }
                else if (previousLaneIndex == rightMiddleIndex)
                {
                    laneIndex = leftMiddleIndex;
                }
                else
                {
                    laneIndex = laneRuntimeCounts[leftMiddleIndex] <= laneRuntimeCounts[rightMiddleIndex]
                        ? leftMiddleIndex
                        : rightMiddleIndex;
                }

                lanePlan.Add(laneIndex);
                laneRuntimeCounts[laneIndex]++;
                previousLaneIndex = laneIndex;
            }

            return lanePlan;
        }

        private void SpawnConfiguredMonstersImmediately(List<SpawData> spawDatas, bool updateWaveKindFlags)
        {
            if (updateWaveKindFlags)
            {
                UpdateCurrentWaveKindFlags(spawDatas);
            }

            if (spawDatas == null || spawDatas.Count == 0)
            {
                return;
            }

            var groupedDatas = new Dictionary<MonsterType, List<SpawData>>();
            int dataCount = spawDatas.Count;
            for (int i = 0; i < dataCount; i++)
            {
                var data = spawDatas[i];
                if (data == null)
                {
                    continue;
                }

                List<SpawData> bucket;
                if (!groupedDatas.TryGetValue(data.m_MonsterKind, out bucket))
                {
                    bucket = new List<SpawData>();
                    groupedDatas.Add(data.m_MonsterKind, bucket);
                }

                bucket.Add(data);
            }

            SpawnMonsterKindImmediately(groupedDatas, MonsterType.Normal);
            SpawnMonsterKindImmediately(groupedDatas, MonsterType.Elite);
            SpawnMonsterKindImmediately(groupedDatas, MonsterType.Boss);
        }

        private void EnqueueWaveSpawnGroups(List<SpawData> spawDatas, bool updateWaveKindFlags)
        {
            if (updateWaveKindFlags)
            {
                UpdateCurrentWaveKindFlags(spawDatas);
            }

            if (spawDatas == null || spawDatas.Count == 0)
            {
                return;
            }

            if (m_Groups.Count == 0)
            {
                m_RuntimeQueuedBatchSpawnRemain = 0;
                m_RuntimeQueuedNextSpawnDelay = 0f;
            }

            int dataCount = spawDatas.Count;
            for (int i = 0; i < dataCount; i++)
            {
                AppendSpawnGroup(spawDatas[i], true);
            }
        }

        private void UpdateCurrentWaveKindFlags(List<SpawData> spawDatas)
        {
            m_CurrentWaveHasElite = false;
            m_CurrentWaveHasBoss = false;
            if (spawDatas == null)
            {
                return;
            }

            int dataCount = spawDatas.Count;
            for (int i = 0; i < dataCount; i++)
            {
                var data = spawDatas[i];
                if (data == null)
                {
                    continue;
                }

                if (data.m_MonsterKind == MonsterType.Boss)
                {
                    m_CurrentWaveHasBoss = true;
                }
                else if (data.m_MonsterKind == MonsterType.Elite)
                {
                    m_CurrentWaveHasElite = true;
                }
            }
        }

        private void SpawnMonsterKindImmediately(
            Dictionary<MonsterType, List<SpawData>> groupedDatas,
            MonsterType monsterKind)
        {
            List<SpawData> datas;
            if (groupedDatas == null ||
                !groupedDatas.TryGetValue(monsterKind, out datas) ||
                datas == null ||
                datas.Count == 0)
            {
                return;
            }

            List<int> lanePlan = null;
            if (monsterKind == MonsterType.Normal)
            {
                lanePlan = BuildNormalMonsterLanePlan(datas);
            }
            else if (monsterKind == MonsterType.Elite || monsterKind == MonsterType.Boss)
            {
                lanePlan = BuildMiddleMonsterLanePlan(datas);
            }
            int laneCursor = 0;
            int dataCount = datas.Count;
            for (int i = 0; i < dataCount; i++)
            {
                var data = datas[i];
                if (data == null)
                {
                    continue;
                }

                int spawnCount = Mathf.Max(0, (int)data.m_Num);
                for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
                {
                    int laneIndex = ResolveImmediateSpawnLaneIndex(monsterKind, lanePlan, ref laneCursor);
                    laneIndex = Mathf.Clamp(laneIndex, 0, Mathf.Max(0, m_MonsterSpawers.Count - 1));
                    var pos = m_MonsterSpawers.Count > 0 ? m_MonsterSpawers[laneIndex] : m_BasePoint;
                    SpawMonster(pos, laneIndex, data, monsterKind);
                }
            }
        }

        private int ResolveImmediateSpawnLaneIndex(
            MonsterType monsterKind,
            List<int> lanePlan,
            ref int laneCursor)
        {
            if (m_MonsterSpawers == null || m_MonsterSpawers.Count == 0)
            {
                return 0;
            }

            if (lanePlan != null && laneCursor < lanePlan.Count)
            {
                return lanePlan[laneCursor++];
            }

            int laneIndex;
            if (monsterKind == MonsterType.Elite || monsterKind == MonsterType.Boss)
            {
                if (TryResolveImmediateMiddleLaneSpawnIndex(out laneIndex))
                {
                    return laneIndex;
                }
            }
            else if (TryResolveImmediateLeastOccupiedLaneIndex(out laneIndex))
            {
                return laneIndex;
            }

            return 0;
        }

        private void UpdateWaveTimer(float dt)
        {
            if (m_LastWaveSpawed || m_WaitSpawWaveTime <= 0)
            {
                return;
            }

            m_WaitSpawWaveTime = Mathf.Max(0, m_WaitSpawWaveTime - dt);
        }

        private void TrySpawnScheduledWave()
        {
            if (m_LastWaveSpawed || m_Groups.Count > 0)
            {
                return;
            }

            if (m_CurWave > 0 && m_Monsters.Count == 0 && m_WaitSpawWaveTime > 0)
            {
                m_WaitSpawWaveTime = 0;
            }

            if (m_WaitSpawWaveTime <= 0)
            {
                SpawWave();
            }
        }

        private void TryFinishAfterAllMonstersCleared()
        {
            if (!m_LastWaveSpawed || m_Groups.Count > 0 || m_Monsters.Count > 0)
            {
                return;
            }

            m_SendFinish = true;
            BattleManager.GetBattle().GetBattleProgress().OnFinishGame(FinishReason.DefenseSucceeded, null);
        }

        private int ResolveMaxWaveCount()
        {
            if (m_GameMode == BattleGameMode.Endless)
            {
                return m_DefaultEndlessDisplayWaveCount;
            }

            return m_EntryStageCfg != null
                ? Mathf.Max(0, m_EntryStageCfg.WaveCount)
                : 0;
        }

        private float ResolveWaveInterval()
        {
            if (m_EntryStageCfg != null && m_EntryStageCfg.WaveIntervalMs > 0)
            {
                return m_EntryStageCfg.WaveIntervalMs / 1000.0f;
            }

            return m_DefaultWaveIntervalSec;
        }

        private static float ReadWaveBatchSpawnIntervalSec()
        {
            BattleConst.ClampTowerDefendSpawnValues();
            return BattleConst.TowerDefendWaveBatchSpawnIntervalSec;
        }

        private bool HasNextWave(int wave)
        {
            if (m_GameMode == BattleGameMode.Endless)
            {
                return true;
            }

            return wave < m_MaxVave;
        }

        private List<SpawData> ResolveRegularWaveSpawnData(int wave)
        {
            var monsterPoolCfg = ResolveActiveStageCfg();
            if (monsterPoolCfg == null)
            {
                return new List<SpawData>();
            }

            if (m_GameMode == BattleGameMode.Chapter)
            {
                int waveIndex = wave - 1;
                if (waveIndex < 0 || waveIndex >= monsterPoolCfg.ChapterWaveMonsterPools.Count)
                {
                    m_CurrentWaveMonsterPoolIndex = -1;
                    return new List<SpawData>();
                }

                m_CurrentWaveMonsterPoolIndex = waveIndex;
                return ResolveConfiguredSpawnData(monsterPoolCfg.ChapterWaveMonsterPools[waveIndex], -1);
            }

            m_CurrentWaveMonsterPoolIndex = -1;
            return ResolveConfiguredSpawnData(monsterPoolCfg.EndlessMonsterPool, -1);
        }

        private List<SpawData> ResolveConfiguredSpawnData(
            ReadOnlyCollection<ReadOnlyCollection<long>> monsterIds,
            int targetTotalCount)
        {
            var spawDatas = new List<SpawData>();
            if (monsterIds == null || monsterIds.Count == 0)
            {
                return spawDatas;
            }

            int totalWeight = 0;
            int configCount = monsterIds.Count;
            for (int i = 0; i < configCount; i++)
            {
                var oneCfg = monsterIds[i];
                if (oneCfg == null || oneCfg.Count < 3)
                {
                    Debug.LogWarning($"塔防关卡怪物配置项格式错误，期待 id+数量+生命值，但实际长度={oneCfg?.Count ?? 0}。将以默认权重1继续。");
                    totalWeight += 1;
                }
                else
                {
                    totalWeight += ReadConfiguredSpawnWeight(oneCfg);
                }
            }

            if (targetTotalCount < 0)
            {
                for (int i = 0; i < configCount; i++)
                {
                    var oneCfg = monsterIds[i];
                    spawDatas.Add(CreateSpawnData(oneCfg, ReadConfiguredSpawnCount(oneCfg)));
                }

                return spawDatas;
            }

            var validConfigCount = configCount;
            targetTotalCount = Math.Max(validConfigCount, targetTotalCount);
            int assignedCount = 0;
            int remainingWeight = Math.Max(1, totalWeight);
            int processedConfigCount = 0;
            for (int i = 0; i < configCount; i++)
            {
                var oneCfg = monsterIds[i];
                processedConfigCount++;
                var weight = 1;
                if (oneCfg == null || oneCfg.Count < 3)
                {
                    Debug.LogWarning($"塔防关卡怪物配置项格式错误，期待 id+数量+生命值，但实际长度={oneCfg?.Count ?? 0}。将以权重1继续分配。");
                    weight = 1;
                }
                else
                {
                    weight = ReadConfiguredSpawnWeight(oneCfg);
                }
                var remainingConfigCount = validConfigCount - processedConfigCount;
                var remainingTargetCount = targetTotalCount - assignedCount;
                int spawnCount;
                if (remainingConfigCount <= 0)
                {
                    spawnCount = remainingTargetCount;
                }
                else
                {
                    var ratio = (float)weight / Math.Max(1, remainingWeight);
                    spawnCount = Mathf.RoundToInt(targetTotalCount * ratio);
                    spawnCount = Math.Max(1, spawnCount);
                    var maxAllowed = Math.Max(1, remainingTargetCount - remainingConfigCount);
                    spawnCount = Math.Min(spawnCount, maxAllowed);
                }

                assignedCount += spawnCount;
                remainingWeight -= weight;
                spawDatas.Add(CreateSpawnData(oneCfg, spawnCount));
            }

            return spawDatas;
        }

        private bool TryResolveGMBossSpawnData(out SpawData bossData, out string sourceLabel)
        {
            bossData = null;
            sourceLabel = string.Empty;
            var activeStageCfg = ResolveActiveStageCfg();
            if (activeStageCfg == null)
            {
                return false;
            }

            if (TryResolveCurrentWaveBossSpawnData(activeStageCfg, out bossData))
            {
                sourceLabel = "当前波次";
                return true;
            }

            if (TryResolveStageBossSpawnData(activeStageCfg, out bossData))
            {
                sourceLabel = "当前关卡";
                return true;
            }

            return false;
        }

        private bool TryResolveCurrentWaveBossSpawnData(
            TowerDefendStageConfigAdapter activeStageCfg,
            out SpawData bossData)
        {
            bossData = null;
            if (activeStageCfg == null)
            {
                return false;
            }

            if (m_GameMode == BattleGameMode.Chapter)
            {
                if (m_CurrentWaveMonsterPoolIndex < 0 ||
                    m_CurrentWaveMonsterPoolIndex >= activeStageCfg.ChapterWaveMonsterPools.Count)
                {
                    return false;
                }

                return TryResolveBossSpawnDataFromPool(
                    activeStageCfg.ChapterWaveMonsterPools[m_CurrentWaveMonsterPoolIndex],
                    out bossData);
            }

            return TryResolveBossSpawnDataFromPool(activeStageCfg.EndlessMonsterPool, out bossData);
        }

        private bool TryResolveStageBossSpawnData(
            TowerDefendStageConfigAdapter activeStageCfg,
            out SpawData bossData)
        {
            bossData = null;
            if (activeStageCfg == null)
            {
                return false;
            }

            if (m_GameMode == BattleGameMode.Chapter)
            {
                int poolCount = activeStageCfg.ChapterWaveMonsterPools.Count;
                for (int i = 0; i < poolCount; i++)
                {
                    if (TryResolveBossSpawnDataFromPool(activeStageCfg.ChapterWaveMonsterPools[i], out bossData))
                    {
                        return true;
                    }
                }

                return false;
            }

            return TryResolveBossSpawnDataFromPool(activeStageCfg.EndlessMonsterPool, out bossData);
        }

        private bool TryResolveBossSpawnDataFromPool(
            ReadOnlyCollection<ReadOnlyCollection<long>> monsterPool,
            out SpawData bossData)
        {
            bossData = null;
            if (monsterPool == null || monsterPool.Count <= 0)
            {
                return false;
            }

            int configCount = monsterPool.Count;
            for (int i = 0; i < configCount; i++)
            {
                var oneCfg = monsterPool[i];
                if (oneCfg == null || oneCfg.Count <= 0)
                {
                    continue;
                }

                long monsterCfgId = oneCfg[0];
                if (ResolveMonsterKind(monsterCfgId) != MonsterType.Boss)
                {
                    continue;
                }

                bossData = CreateSpawnData(oneCfg, 1);
                bossData.m_Num = 1;
                bossData.m_MonsterKind = MonsterType.Boss;
                return true;
            }

            return false;
        }

        private SpawData CreateSpawnData(ReadOnlyCollection<long> oneCfg, int spawnCount)
        {
            var data = new SpawData();
            data.m_MonsterCfgId = oneCfg[0];
            data.m_Level = 1;
            data.m_Num = Math.Max(1, spawnCount);
            data.m_BaseHpValue = ReadConfiguredHpValue(oneCfg);
            data.m_MonsterKind = ResolveMonsterKind(data.m_MonsterCfgId);
            return data;
        }

        private int ReadConfiguredSpawnCount(ReadOnlyCollection<long> oneCfg)
        {
            return oneCfg != null && oneCfg.Count > 1 ? Math.Max(1, (int)oneCfg[1]) : 1;
        }

        private int ReadConfiguredSpawnWeight(ReadOnlyCollection<long> oneCfg)
        {
            return ReadConfiguredSpawnCount(oneCfg);
        }

        private int ReadConfiguredHpValue(ReadOnlyCollection<long> oneCfg)
        {
            return oneCfg != null && oneCfg.Count > 2 ? Math.Max(1, (int)oneCfg[2]) : 1;
        }

        private TowerDefendStageConfigAdapter ResolveActiveStageCfg()
        {
            return m_ActiveStageCfg;
        }

        public List<SpawData> BuildSummonedMonsterSpawnData(int sourceEntityId, int targetTotalCount)
        {
            var activeStageCfg = ResolveActiveStageCfg();
            if (activeStageCfg == null)
            {
                return new List<SpawData>();
            }

            if (m_GameMode == BattleGameMode.Chapter &&
                m_CurrentWaveMonsterPoolIndex >= 0 &&
                m_CurrentWaveMonsterPoolIndex < activeStageCfg.ChapterWaveMonsterPools.Count)
            {
                return ResolveConfiguredSpawnData(activeStageCfg.ChapterWaveMonsterPools[m_CurrentWaveMonsterPoolIndex], targetTotalCount);
            }

            return ResolveConfiguredSpawnData(activeStageCfg.EndlessMonsterPool, targetTotalCount);
        }
        private void SpawMonster(Vector3 pos, int laneIndex, SpawData data, MonsterType monsterKind)
        {
            if (data == null)
            {
                return;
            }

            long cfgId = data.m_MonsterCfgId;
            var cfg = t_monsterBean.GetConfig(cfgId, false);
            if (cfg == null)
            {
                Debug.LogWarning($"塔防刷怪失败，找不到怪物配置，monsterCfgId:{cfgId}。");
                return;
            }

            var initialForward = ResolveMonsterAdvanceDirectionByLaneIndex(laneIndex, pos);
            if (initialForward == Vector3.zero)
            {
                initialForward = Vector3.back;
            }

            var maxHpScalePermille = ResolveMonsterMaxHpScalePermille();
            var baseHpValue = Mathf.Max(1, data.m_BaseHpValue);
            PropertyEntity monster;
            bool reused = TryTakeRecycledMonster(cfgId, monsterKind, out monster);
            if (!reused)
            {
                monster = CreateMonsterEntity(cfg, monsterKind);
            }
            else
            {
                monster.SetBean(cfg);
            }

            if (monster == null)
            {
                return;
            }

            ActivateMonster(
                monster,
                reused,
                pos,
                laneIndex,
                data,
                monsterKind,
                initialForward,
                maxHpScalePermille,
                baseHpValue);

            if (monsterKind == MonsterType.Elite || monsterKind == MonsterType.Boss)
            {
                m_LastSpecialMonsterLaneIndex = laneIndex;
            }
        }

        private PropertyEntity CreateMonsterEntity(t_monsterBean cfg, MonsterType monsterKind)
        {
            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null || cfg == null)
            {
                return null;
            }

            PropertyEntity monster;
            if (monsterKind == MonsterType.Boss)
            {
                monster = (PropertyEntity)objMgr.NewCreature(emEntityType.em_EntityType_MasterHero);
            }
            else
            {
                monster = (PropertyEntity)objMgr.NewCreature(emEntityType.em_EntityType_SmallMonster);
            }

            if (monster == null)
            {
                return null;
            }

            monster.SetBean(cfg);
            monster.SetGroup(GroupId.PushGroupId);
            monster.SetId(objMgr.AssignClientId());
            monster.SetDeadAnimationFinishedHandler(OnMonsterDeadAnimationFinished);
            monster.CreateRender(null, ResourceType.Actor);
            monster.InitInstance();
            return monster;
        }

        private void ActivateMonster(
            PropertyEntity monster,
            bool reused,
            Vector3 pos,
            int laneIndex,
            SpawData data,
            MonsterType monsterKind,
            Vector3 initialForward,
            int maxHpScalePermille,
            float baseHpValue)
        {
            if (monster == null || data == null)
            {
                return;
            }

            var objMgr = BattleManager.GetObjectManager();
            if (objMgr == null)
            {
                return;
            }

            if (reused)
            {
                monster.SetId(objMgr.AssignClientId());
            }

            monster.SetDeadAnimationFinishedHandler(OnMonsterDeadAnimationFinished);
            monster.ResetRuntimeForReuse();
            monster.SetGroup(GroupId.PushGroupId);
            monster.SetTDMove(true);
            monster.SetDropRedArea(true);
            monster.SetBornPosition(pos);
            monster.SetVisiable(true);
            monster.SetForward(initialForward);
            monster.SetPosition(pos);
            if (!monster.ReadIsBoss())
            {
                SetMonsterRenderCommon(monster);
            }

            monster.InitLevel(data.m_Level);
            monster.SetBaseHppublic(baseHpValue);
            monster.SetMaxHpScalePermille(maxHpScalePermille);
            monster.InitSkills();
            // 先登记运行时怪物种类，再初始化血量。
            // InitHp 内部会触发 OnHpChanged，而顶部特殊怪血条会在事件里按种类过滤；
            // 如果这里登记过晚，Boss/精英第一次满血刷新会被误判成普通怪而直接丢掉。
            m_MonsterKinds[monster.ReadId()] = monsterKind;
            m_MonsterLaneIndices[monster.ReadId()] = laneIndex;
            monster.InitHp();
            monster.SetAngularSpeed(720);
            monster.TryChangeState(emEntityState.em_EntityState_Born);
            objMgr.AddPropertyEntity(monster);

            m_Monsters.Add(monster);
            ApplyMonsterPresentation(monster);
        }

        private void ApplyMonsterPresentation(PropertyEntity monster)
        {
            if (monster == null)
            {
                return;
            }

            if (ShouldYieldMonsterPresentationToDeath(monster))
            {
                RestoreMonsterPresentationForDead(monster);
                return;
            }

            monster.SetCanBeTarget(m_MonsterCanBeTarget);
            monster.SetCanBeHurt(m_MonsterCanBeTarget);
            monster.SetFreeze(m_IsMonsterSuspended);
            var render = monster.GetRender();
            if (render != null)
            {
                render.SetShowHud(m_MonsterVisible);
            }
            ApplyMonsterVisibilityTransition(monster, render);
        }

        private void RestoreMonsterPresentationForDead(PropertyEntity monster)
        {
            if (monster == null)
            {
                return;
            }

            // 死亡表现必须压过棒棒糖表现：一旦进入死亡态，先撤掉冻结，避免死亡动画与溶解被暂停。
            monster.SetFreeze(false);

            // 棒棒糖阶段使用 `_BaseColor.a` 做场景显隐，怪物进入死亡态后需要撤掉这条覆盖，
            // 否则死亡溶解虽然仍在跑，但会被残留的颜色透明度压住，看起来像死亡材质动画失效。
            var render = monster.GetRender();
            if (render != null)
            {
                render.ClearColorAlphaProperty(m_MonsterPresentationColorPropertyId);
            }

            if (!monster.ReadVisiable())
            {
                monster.SetVisiable(true);
            }
        }

        // 怪物显隐属于塔防流程语义，淡变时序放在刷怪器里，渲染层只接收颜色属性 Alpha。
        private void ApplyMonsterVisibilityTransition(PropertyEntity monster, UResource render)
        {
            if (monster == null)
            {
                return;
            }

            if (ShouldYieldMonsterPresentationToDeath(monster))
            {
                RestoreMonsterPresentationForDead(monster);
                return;
            }

            var runtime = GetOrCreateMonsterPresentationFadeRuntime(monster);
            KillMonsterPresentationFadeRuntime(runtime, false);

            if (render == null || !render.IsObjectLoaded() || !render.SupportsColorAlphaProperty(m_MonsterPresentationColorPropertyId))
            {
                monster.SetVisiable(m_MonsterVisible);
                if (runtime != null)
                {
                    runtime.m_CurrentAlpha = m_MonsterVisible ? 1.0f : 0.0f;
                }
                if (render != null)
                {
                    render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, m_MonsterVisible ? 1.0f : 0.0f);
                }
                return;
            }

            if (m_MonsterVisible)
            {
                if (!monster.ReadVisiable())
                {
                    monster.SetVisiable(true);
                    runtime.m_CurrentAlpha = 0.0f;
                    render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, 0.0f);
                }

                StartMonsterPresentationFade(runtime, render, monster, 1.0f, m_MonsterPresentationFadeDuration, null);
                return;
            }

            if (!monster.ReadVisiable())
            {
                runtime.m_CurrentAlpha = 0.0f;
                render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, 0.0f);
                return;
            }

            StartMonsterPresentationFade(
                runtime,
                render,
                monster,
                0.0f,
                m_MonsterPresentationFadeDuration,
                () =>
                {
                    if (m_MonsterVisible ||
                        ShouldYieldMonsterPresentationToDeath(monster) ||
                        !BattleManager.ReadIsEntityValide(monster))
                    {
                        return;
                    }

                    monster.SetVisiable(false);
                });
        }

        private MonsterPresentationFadeRuntime GetOrCreateMonsterPresentationFadeRuntime(PropertyEntity monster)
        {
            if (monster == null)
            {
                return null;
            }

            MonsterPresentationFadeRuntime runtime;
            if (!m_MonsterPresentationFadeRuntimes.TryGetValue(monster.ReadId(), out runtime))
            {
                runtime = new MonsterPresentationFadeRuntime();
                m_MonsterPresentationFadeRuntimes.Add(monster.ReadId(), runtime);
            }

            return runtime;
        }

        private void StartMonsterPresentationFade(
            MonsterPresentationFadeRuntime runtime,
            UResource render,
            PropertyEntity monster,
            float targetAlpha,
            float duration,
            Action onComplete)
        {
            if (runtime == null)
            {
                if (render != null && !ShouldYieldMonsterPresentationToDeath(monster))
                {
                    render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, targetAlpha);
                }
                if (!ShouldYieldMonsterPresentationToDeath(monster))
                {
                    onComplete?.Invoke();
                }
                return;
            }

            targetAlpha = Mathf.Clamp01(targetAlpha);
            if (render != null && !ShouldYieldMonsterPresentationToDeath(monster))
            {
                render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, runtime.m_CurrentAlpha);
            }

            if (duration <= 0f || Mathf.Abs(runtime.m_CurrentAlpha - targetAlpha) <= 0.0001f)
            {
                runtime.m_CurrentAlpha = targetAlpha;
                if (render != null && !ShouldYieldMonsterPresentationToDeath(monster))
                {
                    render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, targetAlpha);
                }
                if (!ShouldYieldMonsterPresentationToDeath(monster))
                {
                    onComplete?.Invoke();
                }
                return;
            }

            runtime.m_Tween = DOTween
                .To(
                    () => runtime.m_CurrentAlpha,
                    value =>
                    {
                        runtime.m_CurrentAlpha = value;
                        if (render != null && !ShouldYieldMonsterPresentationToDeath(monster))
                        {
                            render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, value);
                        }
                    },
                    targetAlpha,
                    duration)
                .SetEase(Ease.Linear)
                .OnKill(() => runtime.m_Tween = null)
                .OnComplete(() =>
                {
                    runtime.m_CurrentAlpha = targetAlpha;
                    if (render != null && !ShouldYieldMonsterPresentationToDeath(monster))
                    {
                        render.SetColorAlphaProperty(m_MonsterPresentationColorPropertyId, targetAlpha);
                    }
                    if (!ShouldYieldMonsterPresentationToDeath(monster))
                    {
                        onComplete?.Invoke();
                    }
                });
        }

        private static bool ShouldYieldMonsterPresentationToDeath(PropertyEntity monster)
        {
            return monster == null || monster.ReadIsDestroy() || monster.ReadIsDead();
        }

        private void KillMonsterPresentationFadeRuntime(MonsterPresentationFadeRuntime runtime, bool complete)
        {
            if (runtime == null || runtime.m_Tween == null)
            {
                return;
            }

            runtime.m_Tween.Kill(complete);
            runtime.m_Tween = null;
        }

        private void RemoveMonsterPresentationFadeRuntime(int entityId, bool complete)
        {
            MonsterPresentationFadeRuntime runtime;
            if (!m_MonsterPresentationFadeRuntimes.TryGetValue(entityId, out runtime))
            {
                return;
            }

            KillMonsterPresentationFadeRuntime(runtime, complete);
            m_MonsterPresentationFadeRuntimes.Remove(entityId);
        }

        private void ClearMonsterPresentationFadeRuntimes()
        {
            foreach (var pair in m_MonsterPresentationFadeRuntimes)
            {
                KillMonsterPresentationFadeRuntime(pair.Value, false);
            }

            m_MonsterPresentationFadeRuntimes.Clear();
        }

        // 升级挑战结束后，怪物会重新显示并继续普通战斗。
        // 这里把怪物当前动画按随机进度重新采样一次，避免因为统一恢复显示而显得动作完全同步。
        private void RefreshMonsterAnimationPhaseAfterRestore()
        {
            int monsterCount = m_Monsters.Count;
            for (int i = 0; i < monsterCount; i++)
            {
                var monster = m_Monsters[i];
                if (!BattleManager.ReadIsEntityValide(monster))
                {
                    continue;
                }

                ReseedMonsterAnimationPhase(monster);
            }
        }

        private static void ReseedMonsterAnimationPhase(PropertyEntity monster)
        {
            if (monster == null || !monster.ReadVisiable())
            {
                return;
            }

            var render = monster.GetRender();
            if (render == null)
            {
                return;
            }

            float normalizedTime = UnityEngine.Random.Range(0.05f, 0.95f);
            render.ReplayCurrentAnimation(normalizedTime);
        }

        public  void SetMonsterRenderCommon(Entity ent)
        {
            var render = ent.GetRender();
            //策划说的暂时不需要血条
            //render.EnableHudRender();
            render.ShowWeaponEff(false);
            if (ent.ReadIsBoss())
            {
                var cfg = t_monsterBean.GetConfig(ent.ReadBeanId());
                render.SetName(RenderAPI.GetTextByLanId(cfg.t_name));
            }
            else
            {
                var cfg = t_monsterBean.GetConfig(ent.ReadBeanId());
                render.SetName(RenderAPI.GetTextByLanId(cfg.t_name));
            }


            render.SetShowExp(false);
            render.SetShowLevel(true);




            render.SetShowHud(true);
            render.SetShowHudName(true);

            SetCampColor(ent);
        }
        public  void SetCampColor(Entity ent)
        {
            var render = ent.GetRender();
            if (ent.ReadGroup() == GroupId.AnyGroupId)
            {
                render.SetCampColor(GameColor.FriendGroup);
                return;
            }
            var player_group = GroupId.GuardGroupId;
            if (player_group == ent.ReadGroup())
            {
                render.SetCampColor(GameColor.FriendGroup);
            }
            else
            {
                render.SetCampColor(GameColor.EnemyGroup);
            }
        }
        private InvalidOperationException CreateStageConfigException(string message)
        {
            return new InvalidOperationException("塔防关卡表配置错误: " + message);
        }

        private void AppendSpawnGroup(
            SpawData data,
            bool useBatchedSpawn = false)
        {
            if (data == null || data.m_Num <= 0)
            {
                return;
            }

            var datas = new List<SpawData>(1);
            datas.Add(data);
            var group = new TowerDefendSpawGroup();
            group.m_SpawData = datas;
            group.m_MonsterKind = data.m_MonsterKind;
            group.m_UseBatchedSpawn = useBatchedSpawn;

            if (!useBatchedSpawn)
            {
                if (group.m_MonsterKind == MonsterType.Normal)
                {
                    group.m_RuntimeLaneIndices = BuildNormalMonsterLanePlan(datas);
                }
                else if (group.m_MonsterKind == MonsterType.Elite || group.m_MonsterKind == MonsterType.Boss)
                {
                    group.m_RuntimeLaneIndices = BuildMiddleMonsterLanePlan(datas);
                }
            }

            m_Groups.Add(group);
        }

        private int ResolveRandomWaveBatchSpawnCount()
        {
            int remainCount = CountRemainingQueuedSpawnCount();
            if (remainCount <= 0)
            {
                return 0;
            }

            if (remainCount <= m_WaveBatchSpawnMaxCount)
            {
                return remainCount;
            }

            int batchCount = UnityEngine.Random.Range(m_WaveBatchSpawnMinCount, m_WaveBatchSpawnMaxCount + 1);
            if (remainCount - batchCount == 1)
            {
                batchCount = m_WaveBatchSpawnMinCount;
            }

            return batchCount;
        }

        private int CountRemainingQueuedSpawnCount()
        {
            if (m_Groups == null || m_Groups.Count == 0)
            {
                return 0;
            }

            int remainCount = 0;
            int groupCount = m_Groups.Count;
            for (int i = 0; i < groupCount; i++)
            {
                remainCount += CountRemainingSpawnCount(m_Groups[i]);
            }

            return remainCount;
        }

        // 本波 Boss/精英怪尚未刷出的数量（用于 HUD 判断是否已刷完、能否显示血条）。
        public int CountRemainingQueuedSpecialSpawnCount()
        {
            if (m_Groups == null || m_Groups.Count == 0) return 0;
            int remain = 0;
            foreach (var g in m_Groups)
            {
                if (g != null && (g.m_MonsterKind == MonsterType.Elite || g.m_MonsterKind == MonsterType.Boss))
                    remain += CountRemainingSpawnCount(g);
            }
            return remain;
        }

        private int CountRemainingSpawnCount(TowerDefendSpawGroup group)
        {
            if (group == null || group.m_SpawData == null)
            {
                return 0;
            }

            int remainCount = 0;
            int dataCount = group.m_SpawData.Count;
            for (int i = group.m_RuntimeSpawDataIndex; i < dataCount; i++)
            {
                var data = group.m_SpawData[i];
                if (data == null)
                {
                    continue;
                }

                int spawnCount = Mathf.Max(0, (int)data.m_Num);
                if (i == group.m_RuntimeSpawDataIndex)
                {
                    spawnCount -= group.m_RuntimeSpawIndex;
                }

                remainCount += Mathf.Max(0, spawnCount);
            }

            return remainCount;
        }

        private void ShuffleLanePlan(List<int> lanePlan)
        {
            if (lanePlan == null || lanePlan.Count <= 1)
            {
                return;
            }

            for (int i = lanePlan.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                int tmp = lanePlan[i];
                lanePlan[i] = lanePlan[swapIndex];
                lanePlan[swapIndex] = tmp;
            }
        }

        private MonsterType ResolveMonsterKind(long monsterCfgId)
        {
            var monsterCfg = t_monsterBean.GetConfig(monsterCfgId, false);
            if (monsterCfg == null)
            {
                Debug.LogWarning($"塔防怪物类型读取失败，找不到怪物配置，monsterCfgId:{monsterCfgId}。将回退为普通怪。");
                return MonsterType.Normal;
            }

            switch (monsterCfg.t_type)
            {
                case (int)MonsterType.Normal:
                    return MonsterType.Normal;
                case (int)MonsterType.Elite:
                    return MonsterType.Elite;
                case (int)MonsterType.Boss:
                    return MonsterType.Boss;
                default:
                    Debug.LogWarning(
                        $"塔防怪物类型配置无效，monsterCfgId:{monsterCfgId} t_type:{monsterCfg.t_type}。将回退为普通怪。");
                    return MonsterType.Normal;
            }
        }

        private void UpdateMonsterAdvance(PropertyEntity monster, float dt)
        {
            if (monster == null || monster.ReadIsDead() || monster.ReadIsInBorn())
            {
                return;
            }

            if (!monster.ReadIsMoveableCreature())
            {
                return;
            }

            var currentPos = monster.GetPosition();
            var dir = ResolveMonsterAdvanceDirection(monster, currentPos);
            if (dir == Vector3.zero)
            {
                return;
            }

            monster.TryChangeState(emEntityState.em_EntityState_Move);
            var state = monster.GetCurrentState();
            if(state != null && state.m_StateType == emEntityState.em_EntityState_Move)
            {
                var moveState = state as State_Move;
                moveState.SetForward(dir);
            }
            else
            {
                monster.SetForward(dir);
            }
        }

        private void UpdateMonsterAdvanceByLane(PropertyEntity monster, float dt)
        {
            if (monster == null || monster.ReadIsDead() || monster.ReadIsInBorn())
            {
                return;
            }

            UpdateMonsterAdvance(monster, dt);
        }

        private Vector3 ResolveMonsterAdvanceDirection(PropertyEntity monster, Vector3 currentPos)
        {
            var laneIndex = ReadMonsterLaneIndex(monster != null ? monster.ReadId() : -1);
            return ResolveMonsterAdvanceDirectionByLaneIndex(laneIndex, currentPos);
        }

        private Vector3 ResolveMonsterAdvanceDirectionByLaneIndex(int laneIndex, Vector3 currentPos)
        {
            var lanePath = ReadMonsterLanePath(laneIndex);
            if (lanePath != null && lanePath.m_HasEndPoint)
            {
                var dir = lanePath.m_EndPoint - currentPos;
                dir.y = 0f;
                if (dir.sqrMagnitude <= 0.0001f)
                {
                    return Vector3.zero;
                }

                return dir.normalized;
            }

            var zDirection = ResolveMonsterAdvanceZDirection(laneIndex, currentPos.z);
            if (Mathf.Approximately(zDirection, 0f))
            {
                return Vector3.zero;
            }

            return zDirection < 0f ? Vector3.back : Vector3.forward;
        }

        private float ResolveMonsterAdvanceZDirection(int laneIndex, float currentZ)
        {
            if (laneIndex >= 0 && laneIndex < m_MonsterSpawers.Count)
            {
                var laneSpawnZ = m_MonsterSpawers[laneIndex].z;
                if (!Mathf.Approximately(laneSpawnZ, m_BasePoint.z))
                {
                    return laneSpawnZ > m_BasePoint.z ? -1f : 1f;
                }
            }

            if (Mathf.Approximately(currentZ, m_BasePoint.z))
            {
                return 0f;
            }

            return currentZ > m_BasePoint.z ? -1f : 1f;
        }

        private MonsterLanePathRuntime ReadMonsterLanePath(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= m_MonsterLanePaths.Count)
            {
                return null;
            }

            return m_MonsterLanePaths[laneIndex];
        }

        private int ResolveMonsterMoveSpeedScalePermille()
        {
            if (m_GameMode != BattleGameMode.Endless || m_Battle == null)
            {
                return 1000;
            }

            var battleProgress = m_Battle.GetBattleProgress();
            var battleTime = battleProgress != null ? Mathf.Max(0, battleProgress.ReadStageTime()) : 0;
            var growthStep = Mathf.Min(m_EndlessMoveSpeedGrowthMaxStep, Mathf.FloorToInt(battleTime / m_EndlessMoveSpeedGrowthInterval));
            return 1000 + growthStep * m_EndlessMoveSpeedGrowthPermillePerStep;
        }

        private bool TryHandleMonsterReachBase(PropertyEntity monster)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null || monster == null)
            {
                return false;
            }

            var reachRadius = m_BaseReachRadius + monster.ReadRadius();
            if (!HasMonsterReachedBaseLine(monster, monster.GetPosition(), reachRadius))
            {
                return false;
            }

            battle.ApplyMonsterReachBase(monster);
            PlayMonsterReachBaseDisappearFeedback(monster.GetPosition());
            PrepareMonsterForRecycle(monster, true);
            EnqueueRecycledMonster(monster, monster.ReadId());
            return true;
        }

        private void PlayMonsterReachBaseDisappearFeedback(Vector3 position)
        {
            var eff = RenderEffManager.GetInstance().CreateRenderEff(m_MonsterReachBaseDisappearEffectId);
            if (eff != null)
            {
                eff.ShowEff(false, position, Vector3.zero, Vector3.one);
                eff.SetDuringTime(m_MonsterReachBaseDisappearEffectDuration);
                RenderEffManager.GetInstance().SetAutoPool(eff);
            }

            AudioManager.GetInstance().Play3D(m_MonsterReachBaseDisappearSoundId, position);
        }

        private bool HasMonsterReachedBaseLine(PropertyEntity monster, Vector3 currentPos, float reachRadius)
        {
            var laneIndex = ReadMonsterLaneIndex(monster != null ? monster.ReadId() : -1);
            var lanePath = ReadMonsterLanePath(laneIndex);
            if (lanePath != null && lanePath.m_HasEndPoint)
            {
                var toEnd = lanePath.m_EndPoint - currentPos;
                toEnd.y = 0f;
                var dist = Mathf.Sqrt(toEnd.sqrMagnitude);
                var endPointReachRadius = monster.ReadRadius() + 0.1f;
                return dist <= endPointReachRadius;
            }

            var currentZ = currentPos.z;
            var zDirection = ResolveMonsterAdvanceZDirection(laneIndex, currentZ);
            if (Mathf.Approximately(zDirection, 0f) || Mathf.Approximately(currentZ, m_BasePoint.z))
            {
                return true;
            }

            if (zDirection < 0f)
            {
                return currentZ <= m_BasePoint.z + reachRadius;
            }

            return currentZ >= m_BasePoint.z - reachRadius;
        }


    }
}
