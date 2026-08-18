using MonoBean;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDll
{
    [CustomEditor(typeof(HeroAttributePanel))]
    [CanEditMultipleObjects]
    public class HeroAttributePanelEditor : Editor
    {
        private GUIStyle m_TitleStyle;
        private GUIStyle m_LabelStyle;
        private GUIStyle m_ValueStyle;
        private GUIStyle m_SeparatorStyle;
        private GUIStyle m_BoxStyle;
        private bool m_StylesInitialized = false;

        private void InitStyles()
        {
            m_TitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            m_LabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12
            };

            m_ValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight
            };

            m_SeparatorStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(1, 1, new Color(0.4f, 0.4f, 0.5f, 0.5f)) },
                margin = new RectOffset(0, 0, 4, 4)
            };

            m_BoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(1, 1, new Color(0.15f, 0.15f, 0.18f, 0.3f)) },
                padding = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(0, 0, 2, 2)
            };

            m_StylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public override void OnInspectorGUI()
        {
            Repaint();

            if (!m_StylesInitialized)
                InitStyles();

            serializedObject.Update();

            DrawDefaultPropertyField();

            HeroAttributePanel panel = (HeroAttributePanel)target;
            PlayerHero hero = panel.GetTargetHero();

            if (hero == null)
            {
                EditorGUILayout.HelpBox("未指定英雄。", MessageType.Info);
                return;
            }

            if (hero.ReadIsDestroy() || hero.ReadIsDead())
            {
                EditorGUILayout.HelpBox("英雄已销毁或死亡。", MessageType.Warning);
                return;
            }

            DrawSeparator();
            DrawTitle("英雄参数");
            DrawSeparator();
            DrawBasicInfo(hero);
            DrawSeparator();
            DrawEditParameters(hero, panel);
            DrawSeparator();
            DrawHpBar(hero);
            DrawSeparator();
            DrawCombatAttributes(hero);
            DrawSeparator();
            DrawSkills(hero);
            DrawSeparator();
            DrawBuffs(hero);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefaultPropertyField()
        {
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }

        private void DrawSeparator()
        {
            GUILayout.Box(GUIContent.none, m_SeparatorStyle, GUILayout.Height(2), GUILayout.ExpandWidth(true));
        }

        private void DrawTitle(string title)
        {
            EditorGUILayout.LabelField(title, m_TitleStyle);
        }

        private void DrawBasicInfo(PlayerHero hero)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);
            DrawKeyValue("等级", hero.ReadLevel().ToString());
            DrawKeyValue("状态", hero.GetCurrentState()?.GetStateType().ToString() ?? "无");
            EditorGUILayout.EndVertical();
        }

        private void DrawEditParameters(PlayerHero hero, HeroAttributePanel panel)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);
            EditorGUILayout.LabelField("调试参数（仅供策划测试）", m_TitleStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("设置等级:", m_LabelStyle, GUILayout.Width(80));
            int currentLevel = hero.ReadLevel();
            int newLevel = EditorGUILayout.IntField(currentLevel, GUILayout.Width(60));
            if (newLevel != currentLevel && newLevel > 0)
            {
                ApplyHeroLevelStepwise(hero, newLevel);
                FinalizeDebugHeroLevelChange(hero, newLevel);
                EditorUtility.SetDirty(panel.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("设置血量:", m_LabelStyle, GUILayout.Width(80));
            float currentHp = hero.ReadHP();
            float newHp = EditorGUILayout.FloatField(currentHp, GUILayout.Width(60));
            if (!Mathf.Approximately(newHp, currentHp) && newHp >= 0)
            {
                hero.SetHpRuntime(newHp);
                hero.OnHpChanged();
                EditorUtility.SetDirty(panel.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Buff操作", m_TitleStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("添加Buff(ID):", m_LabelStyle, GUILayout.Width(90));
            long buffIdToAdd = EditorGUILayout.LongField(0, GUILayout.Width(80));
            if (buffIdToAdd > 0)
            {
                var buffMgr = hero.GetBuffManager();
                if (buffMgr != null && GUILayout.Button("添加", GUILayout.Width(50)))
                {
                    buffMgr.TryAddBuff(buffIdToAdd, hero, hero);
                    EditorUtility.SetDirty(panel.gameObject);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("移除Buff(ID):", m_LabelStyle, GUILayout.Width(90));
            long buffIdToRemove = EditorGUILayout.LongField(0, GUILayout.Width(80));
            if (buffIdToRemove > 0)
            {
                var buffMgr = hero.GetBuffManager();
                if (buffMgr != null && GUILayout.Button("移除", GUILayout.Width(50)))
                {
                    buffMgr.RemoveBuff(buffIdToRemove);
                    EditorUtility.SetDirty(panel.gameObject);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("清除所有Buff"))
            {
                var buffMgr = hero.GetBuffManager();
                if (buffMgr != null)
                {
                    buffMgr.ClearBuffs();
                    EditorUtility.SetDirty(panel.gameObject);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("技能操作", m_TitleStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("重置技能CD:", m_LabelStyle, GUILayout.Width(90));
            int skillSlot = EditorGUILayout.IntField(0, GUILayout.Width(60));
            if (GUILayout.Button("重置", GUILayout.Width(50)))
            {
                var skillMgr = hero.GetSkillManager();
                if (skillMgr != null)
                {
                    var skill = skillMgr.ReadSkillBySlot(skillSlot);
                    if (skill != null)
                    {
                        skill.SetCooldown(BattleManager.ReadBattleTime());
                        EditorUtility.SetDirty(panel.gameObject);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawHpBar(PlayerHero hero)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);

            float currentHp = hero.ReadHP();
            float maxHp = hero.GetMaxHP();
            float hpPercent = maxHp > 0 ? currentHp / maxHp : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("血量", m_LabelStyle, GUILayout.Width(50));

            Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.DrawRect(barRect, new Color(0.3f, 0.1f, 0.1f, 1f));

            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * hpPercent, barRect.height);
            EditorGUI.DrawRect(fillRect, hpPercent > 0.5f ? Color.green : (hpPercent > 0.25f ? Color.yellow : Color.red));

            string hpText = $"{currentHp:F0} / {maxHp:F0} ({hpPercent * 100:F1}%)";
            Rect textRect = new Rect(barRect.x, barRect.y, barRect.width, barRect.height);
            EditorGUI.LabelField(textRect, hpText, new GUIStyle(m_ValueStyle) { alignment = TextAnchor.MiddleCenter });

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCombatAttributes(PlayerHero hero)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);
            DrawKeyValue("攻击力", hero.GetAtk().ToString("F2"));
            DrawKeyValue("攻击速度", hero.GetNormalAtkSpeed().ToString("F2"));
            DrawKeyValue("攻击范围", hero.GetAttackRange().ToString("F2"));
            DrawKeyValue("移动速度", hero.GetConfigMoveSpeed().ToString("F2"));
            DrawKeyValue("暴击率", (hero.ReadCritRatePermille() * 100).ToString("F2") + "%");
            DrawKeyValue("暴击伤害", hero.ReadCritDamageScalePermille().ToString("F2"));
            DrawKeyValue("伤害加成", hero.ReadDamageAmpPercent().ToString("F2"));
            EditorGUILayout.EndVertical();
        }

        private void DrawSkills(PlayerHero hero)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);
            EditorGUILayout.LabelField("技能", m_TitleStyle);

            var skillManager = hero.GetSkillManager();
            if (skillManager == null)
            {
                EditorGUILayout.LabelField("  无技能管理器", m_LabelStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            var skills = skillManager.ReadSkills();
            if (skills == null || skills.Count == 0)
            {
                EditorGUILayout.LabelField("  无技能", m_LabelStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            var groupedSkills = new Dictionary<int, List<Skill>>();
            foreach (var skill in skills)
            {
                if (skill == null) continue;
                int slot = skill.ReadSlot();
                if (!groupedSkills.ContainsKey(slot))
                    groupedSkills[slot] = new List<Skill>();
                groupedSkills[slot].Add(skill);
            }

            int autoSkillSlot = hero.ReadTowerDefendAutoSkillSlot();

            foreach (var kvp in groupedSkills)
            {
                int slot = kvp.Key;
                var slotSkills = kvp.Value;
                string slotName = slot == 0
                    ? "普攻"
                    : (slot == autoSkillSlot ? "自动技能" : $"技能槽{slot}");
                string autoFlag = slot == autoSkillSlot ? " [自动]" : "";
                EditorGUILayout.LabelField($"  [{slotName}]{autoFlag} 数量:{slotSkills.Count}", m_ValueStyle);

                for (int i = 0; i < slotSkills.Count; i++)
                {
                    var skill = slotSkills[i];
                    var skillBean = skill.GetSkillBean();
                    if (skillBean == null) continue;

                    EditorGUILayout.LabelField($"    #{i + 1} ID:{skillBean.t_id} 等级:{skill.ReadLevel()}", m_LabelStyle);

                    float cdTotal = skill.ReadCooldownTime();
                    float cdLeft = skill.ReadCooldownLeftTime();
                    bool isReady = skill.ReadIsCooldown();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("      CD:", m_LabelStyle, GUILayout.Width(50));

                    Rect cdBarRect = EditorGUILayout.GetControlRect(GUILayout.Height(14));
                    EditorGUI.DrawRect(cdBarRect, new Color(0.2f, 0.2f, 0.2f, 1f));

                    if (!isReady && cdTotal > 0)
                    {
                        float cdPercent = 1 - (cdLeft / cdTotal);
                        Rect cdFillRect = new Rect(cdBarRect.x, cdBarRect.y, cdBarRect.width * cdPercent, cdBarRect.height);
                        EditorGUI.DrawRect(cdFillRect, new Color(1f, 0.6f, 0.2f, 1f));
                    }
                    else
                    {
                        EditorGUI.DrawRect(cdBarRect, new Color(0.3f, 1f, 0.3f, 1f));
                    }

                    string cdText = isReady ? "就绪" : $"{cdLeft:F2}s / {cdTotal:F2}s";
                    GUI.Label(cdBarRect, cdText, new GUIStyle(m_ValueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField($"      攻击间隔: {skillBean.t_Interval}ms", m_LabelStyle);

                    string precon = GetSkillPreconDesc(skillBean);
                    if (!string.IsNullOrEmpty(precon))
                        EditorGUILayout.LabelField($"      释放条件: {precon}", m_LabelStyle);

                    DrawSkillGestureConfig(skill);
                }

                GUILayout.Space(4);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillGestureConfig(Skill skill)
        {
            var skillDesc = skill != null ? skill.GetSkillDescBean() : null;
            if (skillDesc == null)
            {
                EditorGUILayout.LabelField("      手势配置: 技能描述表为空", m_LabelStyle);
                return;
            }

            if (BoneGestureRules.TryResolveActionBinding(
                    skillDesc.t_gesture,
                    skillDesc.t_gesture_phase,
                    out BoneGestureType gestureType,
                    out BoneGesturePhaseMask phaseMask,
                    out bool requiresConsumeResult,
                    out string error))
            {
                string consumeText = requiresConsumeResult ? "需要消费结果" : "不需要消费结果";
                EditorGUILayout.LabelField(
                    $"      手势配置: {gestureType}({skillDesc.t_gesture}) / {phaseMask}({skillDesc.t_gesture_phase}) / {consumeText}",
                    m_LabelStyle);
                return;
            }

            EditorGUILayout.LabelField(
                $"      手势配置: 无效 t_gesture={skillDesc.t_gesture} t_gesture_phase={skillDesc.t_gesture_phase} 原因:{error}",
                m_LabelStyle);
        }

        private void DrawBuffs(PlayerHero hero)
        {
            EditorGUILayout.BeginVertical(m_BoxStyle);
            EditorGUILayout.LabelField("增益效果", m_TitleStyle);

            var buffManager = hero.GetBuffManager();
            if (buffManager == null)
            {
                EditorGUILayout.LabelField("  无Buff管理器", m_LabelStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            var buffs = buffManager.ReadBuffs();
            if (buffs == null || buffs.Count == 0)
            {
                EditorGUILayout.LabelField("  无激活的Buff", m_LabelStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var buff in buffs)
            {
                if (buff == null) continue;

                var bean = buff.GetBean();
                if (bean == null) continue;

                int stackCount = buff.ReadStackCount();
                string stackText = stackCount > 1 ? $" x{stackCount}" : "";

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{bean.t_id}]{stackText}", m_LabelStyle, GUILayout.Width(100));

                float duringTime = bean.t_buff_during / 1000.0f;
                if (duringTime < float.MaxValue)
                    EditorGUILayout.LabelField($"{duringTime:F1}s", m_ValueStyle);
                else
                    EditorGUILayout.LabelField("永久", m_ValueStyle);

                EditorGUILayout.EndHorizontal();

                var descBean = buff.GetDescBean();
                if (descBean != null && !string.IsNullOrEmpty(descBean.t_desc))
                    EditorGUILayout.LabelField($"    {descBean.t_desc}", new GUIStyle(m_LabelStyle) { fontSize = 10, wordWrap = true });

                GUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        private string GetSkillPreconDesc(t_skillBean skillBean)
        {
            if (skillBean == null || skillBean.t_skill_precon == null || skillBean.t_skill_precon.Count < 1)
                return "无";

            if (skillBean.t_skill_precon.Count == 1 && skillBean.t_skill_precon[0] == 0)
                return "无";

            if (skillBean.t_skill_precon.Count < 3)
                return "无";

            long buffId = skillBean.t_skill_precon[0];
            int stackCount = skillBean.t_skill_precon[1];

            return $"Buff[{buffId}] x{stackCount}";
        }

        private void ApplyHeroLevelStepwise(PlayerHero hero, int targetLevel)
        {
            if (hero == null)
            {
                return;
            }

            int currentLevel = hero.ReadLevel();
            if (currentLevel == targetLevel)
            {
                return;
            }

            int step = targetLevel > currentLevel ? 1 : -1;
            while (currentLevel != targetLevel)
            {
                currentLevel += step;
                hero.SetLevel(currentLevel);
            }
        }

        private void FinalizeDebugHeroLevelChange(PlayerHero hero, int targetLevel)
        {
            if (hero == null)
            {
                return;
            }

            SyncBattlePlayerLevel(hero, targetLevel);
            if (hero.GetSkillManager() != null)
            {
                hero.RegisterSkills();
            }
        }

        private void SyncBattlePlayerLevel(PlayerHero hero, int targetLevel)
        {
            var battle = BattleManager.GetBattle() as TowerDefendBattle;
            if (battle == null)
            {
                return;
            }

            long playerId = hero.ReadBattlePlayerId();
            if (playerId <= 0)
            {
                return;
            }

            var player = battle.GetPlayer(playerId);
            if (player != null)
            {
                player.m_RoleLevel = Mathf.Max(1, targetLevel);
            }
        }

        private void DrawKeyValue(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {label}", m_LabelStyle, GUILayout.Width(130));
            EditorGUILayout.LabelField(value, m_ValueStyle);
            EditorGUILayout.EndHorizontal();
        }
    }
}
