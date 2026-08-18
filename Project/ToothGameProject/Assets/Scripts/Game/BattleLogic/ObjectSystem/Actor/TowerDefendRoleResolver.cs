using MonoBean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace GameDll
{
    public static class TowerDefendRoleResolver
    {
        public static TowerDefendRoleSnapshot Resolve(long roleCfgId, int level)
        {
            var cfg = t_heroBean.GetConfig(roleCfgId, false);
            if (cfg == null)
            {
                throw new InvalidOperationException($"找不到塔防角色配置，roleCfgId={roleCfgId}。");
            }

            var snapshot = new TowerDefendRoleSnapshot
            {
                m_NormalSkillCfgId = ResolveSkillCfgId(cfg.t_normal_skill_id, level, roleCfgId, "t_normal_skill_id"),
                m_AutoSkillCfgId = ResolveOptionalSkillCfgId(cfg.t_auto_skill_id, level, roleCfgId, "t_auto_skill_id"),
            };
            AppendSkillCfgIds(snapshot.m_ActiveSkillCfgIds, cfg.t_skill_id, roleCfgId, "t_skill_id");

            var growCfg = t_tdRoleGrowBean.GetConfig(roleCfgId, false);
            if (growCfg == null)
            {
                throw new InvalidOperationException($"找不到塔防角色成长配置，roleCfgId={roleCfgId}。");
            }

            if (growCfg.t_runtime_buff_ids != null && growCfg.t_runtime_buff_ids.Count > 0)
            {
                int runtimeBuffIndex = Mathf.Clamp(level - 1, 0, growCfg.t_runtime_buff_ids.Count - 1);
                var buffCfgIds = growCfg.t_runtime_buff_ids[runtimeBuffIndex];
                for (int i = 0; i < buffCfgIds.Count; i++)
                {
                    snapshot.m_RuntimeBuffCfgIds.Add(buffCfgIds[i]);
                }
            }

            return snapshot;
        }

        private static long ResolveSkillCfgId(ReadOnlyCollection<long> skillCfgIds, int level, long roleCfgId, string fieldName)
        {
            if (skillCfgIds == null || skillCfgIds.Count <= 0)
            {
                throw new InvalidOperationException($"塔防角色缺少技能配置，roleCfgId={roleCfgId}，field={fieldName}。");
            }

            int index;
            if (skillCfgIds.Count >= 5)
            {
                index = Mathf.Clamp(level - 1, 0, skillCfgIds.Count - 1);
            }
            else
            {
                // 当前技能表仍按 1/3/5 级里程碑压缩配置，
                // 2/4 级继续复用上一档技能，差异由 runtime buff 表达。
                index = Mathf.Clamp((Mathf.Max(1, level) - 1) / 2, 0, skillCfgIds.Count - 1);
            }

            long skillCfgId = skillCfgIds[index];
            //我们允许刚开始没有技能，所以有时间可以填0
            if (skillCfgId < 0)
            {
                throw new InvalidOperationException($"塔防角色技能配置无效，roleCfgId={roleCfgId}，field={fieldName}，level={level}。");
            }

            return skillCfgId;
        }

        private static long ResolveOptionalSkillCfgId(ReadOnlyCollection<long> skillCfgIds, int level, long roleCfgId, string fieldName)
        {
            if (skillCfgIds == null || skillCfgIds.Count <= 0)
            {
                return 0;
            }

            return ResolveSkillCfgId(skillCfgIds, level, roleCfgId, fieldName);
        }

        private static void AppendSkillCfgIds(List<long> output, ReadOnlyCollection<long> skillCfgIds, long roleCfgId, string fieldName)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (skillCfgIds == null || skillCfgIds.Count <= 0)
            {
                throw new InvalidOperationException($"塔防角色缺少技能配置，roleCfgId={roleCfgId}，field={fieldName}。");
            }

            int count = skillCfgIds.Count;
            for (int i = 0; i < count; i++)
            {
                long skillCfgId = skillCfgIds[i];
                if (skillCfgId <= 0)
                {
                    throw new InvalidOperationException($"塔防角色技能配置无效，roleCfgId={roleCfgId}，field={fieldName}，index={i}。");
                }

                output.Add(skillCfgId);
            }
        }
    }
}
