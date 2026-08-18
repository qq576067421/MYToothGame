using LCL;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public enum HudType
    {
        Player,
        Monster,
        Building,
        Item,
        Tower,
    }
    public enum HpTextType
    {
        SkillHurt = 0,
        Miss = 1,
        Gold = 2,
        NormalHurt = 3,
        OtherText = 4,
        AddGold = 5,
        Crit = 6,
    }
    public class HudManager
    {
        public static int m_HudIds = 0;
        private static readonly Vector3 m_HudHiddenLocalPosition = new Vector3(0, 1000000, 0);
        private static readonly Vector2 m_HudHiddenAnchoredPosition = Vector2.up * 3000f;

        private static Dictionary<int, GameObject> m_HudTemplates = new Dictionary<int, GameObject>();
        private static Dictionary<int, List<PlayerHud>> m_HudCaches = new Dictionary<int, List<PlayerHud>>();
        private static Dictionary<int, PlayerHud> m_Huds = new Dictionary<int, PlayerHud>();
        private static RectTransform m_HudParent = null;

        private static RectTransform m_EffectParent = null;
        private static Dictionary<HpTextType, GameObject> m_HurtHpTemplates = new Dictionary<HpTextType, GameObject>();
        private static Dictionary<HpTextType, List<HpText>> m_HpTextCaches = new Dictionary<HpTextType, List<HpText>>();
        private static Dictionary<int, HpText> m_HpTexts = new Dictionary<int, HpText>();
        private static int m_HpTextIds = 0;
        private static List<ABRequest> m_ABs = new List<ABRequest>();

        public static void Init()
        {
            if(m_HudCaches.Count  > 0)
            {
                UDebug.Log("已有Hud缓存，无需再次缓存, Hud count:" + m_HudCaches.Count);
                return;
            }
            ABRequest hudAbId = null;
            hudAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/player_hud_wnd.jpg", "player_hud_wnd", (rd, ud)=> 
            {
                var hudgo  = rd.m_Obj as GameObject;
                m_HudTemplates.Add((int)HudType.Player, hudgo);

                List<PlayerHud> cache = new List<PlayerHud>();
                for (int i = 0; i < 5; ++i)
                {
                    var hud = GetHud(HudType.Player);
                    cache.Add(hud);
                }
                foreach (var go in cache)
                {
                    PoolHud(go);
                }
            });
            m_ABs.Add(hudAbId);

            hudAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/building_hud_wnd.jpg", "building_hud_wnd", (rd, ud) =>
            {
                var hudgo = rd.m_Obj as GameObject;
                m_HudTemplates.Add((int)HudType.Building, hudgo);

                List<PlayerHud> cache = new List<PlayerHud>();
                for (int i = 0; i < 5; ++i)
                {
                    var hud = GetHud(HudType.Building);
                    cache.Add(hud);
                }
                foreach (var go in cache)
                {
                    PoolHud(go);
                }
            });
            m_ABs.Add(hudAbId);

            hudAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/tower_hud_wnd.jpg", "tower_hud_wnd", (rd, ud) =>
            {
                var hudgo = rd.m_Obj as GameObject;
                m_HudTemplates.Add((int)HudType.Tower, hudgo);

                List<PlayerHud> cache = new List<PlayerHud>();
                for (int i = 0; i < 5; ++i)
                {
                    var hud = GetHud(HudType.Tower);
                    cache.Add(hud);
                }
                foreach (var go in cache)
                {
                    PoolHud(go);
                }
            });
            m_ABs.Add(hudAbId);

            hudAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/monster_hud_wnd.jpg", "monster_hud_wnd", (rd, ud) =>
            {
                var hudgo = rd.m_Obj as GameObject;
                m_HudTemplates.Add((int)HudType.Monster, hudgo);

                List<PlayerHud> cache = new List<PlayerHud>();
                for (int i = 0; i < 20; ++i)
                {
                    var hud = GetHud(HudType.Monster);
                    cache.Add(hud);
                }
                foreach (var go in cache)
                {
                    PoolHud(go);
                }
            });
            m_ABs.Add(hudAbId);

            hudAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/item_hud_wnd.jpg", "item_hud_wnd", (rd, ud) =>
            {
                var hudgo = rd.m_Obj as GameObject;
                m_HudTemplates.Add((int)HudType.Item, hudgo);

                List<PlayerHud> cache = new List<PlayerHud>();
                for (int i = 0; i < 10; ++i)
                {
                    var hud = GetHud(HudType.Item);
                    cache.Add(hud);
                }
                foreach (var go in cache)
                {
                    PoolHud(go);
                }
            });
            m_ABs.Add(hudAbId);

            ABRequest hpAbId = null;
            hpAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/number_hurt/number_hurt_hp.jpg", "number_hurt_hp", (rd, ud) =>
            {
                var hpgo = rd.m_Obj as GameObject;
                m_HurtHpTemplates.Add(HpTextType.SkillHurt, hpgo);
                //常用语技能
                List<HpText> hp_cache = new List<HpText>();
                for (int i = 0; i < 10; ++i)
                {
                    var hud = GetHpText(HpTextType.SkillHurt);
                    hp_cache.Add(hud);
                }
                foreach (var go in hp_cache)
                {
                    PoolHpText(go);
                }
            });
            m_ABs.Add(hudAbId);

            hpAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/number_hurt/number_hurt_miss.jpg", "number_hurt_miss", (rd, ud) =>
            {
                var hpgo = rd.m_Obj as GameObject;
                m_HurtHpTemplates.Add(HpTextType.Miss, hpgo);
                //miss
                List<HpText> hp_cache = new List<HpText>();
                for (int i = 0; i < 10; ++i)
                {
                    var hud = GetHpText(HpTextType.Miss);
                    hp_cache.Add(hud);
                }
                foreach (var go in hp_cache)
                {
                    PoolHpText(go);
                }
            });
            m_ABs.Add(hudAbId);


            hpAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/number_hurt/number_hurt_crit.jpg", "number_hurt_crit", (rd, ud) =>
            {
                var hpgo = rd.m_Obj as GameObject;
                m_HurtHpTemplates.Add(HpTextType.Crit, hpgo);
                //miss
                List<HpText> hp_cache = new List<HpText>();
                for (int i = 0; i < 4; ++i)
                {
                    var hud = GetHpText(HpTextType.Crit);
                    hp_cache.Add(hud);
                }
                foreach (var go in hp_cache)
                {
                    PoolHpText(go);
                }
            });
            m_ABs.Add(hudAbId);


            hpAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/number_hurt/number_hurt_gold.jpg", "number_hurt_gold", (rd, ud) =>
            {
                var hpgo = rd.m_Obj as GameObject;
                m_HurtHpTemplates.Add(HpTextType.AddGold, hpgo);

                List<HpText> hp_cache = new List<HpText>();
                for (int i = 0; i < 10; ++i)
                {
                    var hud = GetHpText(HpTextType.AddGold);
                    hp_cache.Add(hud);
                }
                foreach (var go in hp_cache)
                {
                    PoolHpText(go);
                }
            });
            m_ABs.Add(hudAbId);

            hpAbId = UIRes.LoadPrefabAsync(typeof(GameObject), "ui/hud/number_hurt/number_hurt_normal.jpg", "number_hurt_normal", (rd, ud) =>
            {
                var hpgo = rd.m_Obj as GameObject;
                m_HurtHpTemplates.Add(HpTextType.NormalHurt, hpgo);
                //miss
                List<HpText> hp_cache = new List<HpText>();
                for (int i = 0; i < 10; ++i)
                {
                    var hud = GetHpText(HpTextType.NormalHurt);
                    hp_cache.Add(hud);
                }
                foreach (var go in hp_cache)
                {
                    PoolHpText(go);
                }
            });
            m_ABs.Add(hudAbId);
        }

        public static PlayerHud GetHud(HudType type)
        {
            List<PlayerHud> huds = null;
            if (m_HudCaches.ContainsKey((int)type))
            {
                huds = m_HudCaches[(int)type];
            }
            else
            {
                huds = new List<PlayerHud>();
                m_HudCaches.Add((int)type, huds);
            }

            if (huds != null)
            {
                var count = huds.Count;
                if (count > 0)
                {
                    var t = huds[count - 1];
                    huds.RemoveAt(count - 1);
                    m_Huds.Add(t.m_Id, t);
                    t.transform.SetAsLastSibling();
                    if (t.m_CanvasGroup != null)
                    {
                        t.m_CanvasGroup.alpha = 1;
                    }
                    ResetHudRuntimeState(t);
                    return t;
                }
                else
                {
                    if(!m_HudTemplates.ContainsKey((int)type))
                    {
                        return null;
                    }
                    var clone = GameObject.Instantiate(m_HudTemplates[(int)type]);
                    PlayerHud t = clone.GetComponent<PlayerHud>();
                    t.m_Id = m_HudIds++;
                    t.m_Type = type;
                    if (t.m_CanvasGroup != null)
                    {
                        t.m_CanvasGroup.alpha = 1;
                    }
                    m_Huds.Add(t.m_Id, t);


                    if (m_HudParent == null || m_HudParent.Equals(null))
                    {
                        var hudGo = GameObject.Find("GlobalUI/GlobalCanvas/HUD");
                        if (hudGo != null)
                        {
                            m_HudParent = hudGo.GetComponent<RectTransform>();
                        }
                    }

                    var trans = t.transform as RectTransform;
                    if (trans.parent != m_HudParent)
                    {
                        trans.SetParent(m_HudParent);
                        t.m_Parent = m_HudParent;
                        trans.localEulerAngles = Vector3.zero;
                        trans.localScale = Vector3.one;
                        trans.localPosition = m_HudHiddenLocalPosition;
                    }
                    ResetHudRuntimeState(t);
                    trans.SetAsLastSibling();
                    return t;
                }
            }
            else
            {
                return null;
            }


        }
        public static void PoolHud(PlayerHud hud)
        {
            if (!m_Huds.ContainsKey(hud.m_Id))
            {
                return;
            }
            m_Huds.Remove(hud.m_Id);
            ResetHudRuntimeState(hud);
            if (hud.m_CanvasGroup != null)
            {
                hud.m_CanvasGroup.alpha = 0;
            }
            List<PlayerHud> huds = null;
            var type = hud.m_Type;
            if (m_HudCaches.ContainsKey((int)type))
            {
                huds = m_HudCaches[(int)type];
            }
            else
            {
                huds = new List<PlayerHud>();
                m_HudCaches.Add((int)type, huds);
            }
            huds.Add(hud);
        }

        private static void ResetHudRuntimeState(PlayerHud hud)
        {
            if (hud == null)
            {
                return;
            }

            hud.transform.localPosition = m_HudHiddenLocalPosition;

            if (hud.m_HudRender != null)
            {
                hud.m_HudRender.localPosition = m_HudHiddenLocalPosition;
                hud.m_HudRender.anchoredPosition = m_HudHiddenAnchoredPosition;
            }

            var bar = hud.m_HpBar;
            if (bar != null)
            {
                bar.m_EnableUpdate = false;
                bar.target = null;
                bar.target_collider = null;
                bar.target_resource = null;
                bar.offsetPos = Vector3.zero;
                if (bar.rectTrans != null)
                {
                    bar.rectTrans.anchoredPosition = m_HudHiddenAnchoredPosition;
                }
            }
        }

        public static HpText GetHpText(HpTextType detail_type)
        {
            List<HpText> texts = null;

            HpTextType type = detail_type;
            //if(detail_type >= HpTextType.NormalHurt)
            //{
            //    type = HpTextType.NormalHurt;
            //}
            if(m_HpTextCaches.ContainsKey(type))
            {
                texts = m_HpTextCaches[type];
            }
            else
            {
                texts = new List<HpText>();
                m_HpTextCaches.Add(type, texts);
            }

            if(texts != null)
            {
                var count = texts.Count;
                if(count > 0)
                {
                    var t = texts[count - 1];
                    texts.RemoveAt(count - 1);

                    t.m_Id = m_HpTextIds++;
                    t.m_Type = type;
                    //if (t.m_CanvasGroup != null)
                    //{
                    //    t.m_CanvasGroup.alpha = 1;
                    //}
                    if (!t.m_Trans.gameObject.activeSelf)
                    {
                        t.m_Trans.gameObject.SetActive(true);
                    }
                    t.m_OnComplete = (hpt) =>
                    {
                        PoolHpText(hpt);
                    };
                    m_HpTexts.Add(t.m_Id, t);
                    return t;
                }
                else
                {
                    var clone = GameObject.Instantiate(m_HurtHpTemplates[type]);
                    HpText t = clone.GetComponent<HpText>();
                    t.m_Id = m_HpTextIds++;
                    t.m_Type = type;
                    //if (t.m_CanvasGroup != null)
                    //{
                    //    t.m_CanvasGroup.alpha = 1;
                    //}
                    if (!t.m_Trans.gameObject.activeSelf)
                    {
                        t.m_Trans.gameObject.SetActive(true);
                    }
                    t.m_OnComplete = (hpt) =>
                    {
                        PoolHpText(hpt);
                    };
                    m_HpTexts.Add(t.m_Id, t);


                    if (m_EffectParent == null || m_EffectParent.Equals(null))
                    {
                        var hudGo = GameObject.Find("GlobalUI/GlobalCanvas/Effect");
                        if (hudGo != null)
                        {
                            m_EffectParent = hudGo.GetComponent<RectTransform>();
                        }
                    }

                    var trans = t.transform as RectTransform;
                    if (trans.parent != m_EffectParent)
                    {
                        trans.SetParent(m_EffectParent);
                        t.m_Parent = m_EffectParent;
                        trans.localEulerAngles = Vector3.zero;
                        trans.localScale = Vector3.one;
                        trans.localPosition = Vector3.zero;
                    }
                    return t;
                }
            }
            else
            {
                return null;
            }


        }
        public static void PoolHpText(HpText hud)
        {
            if(!m_HpTexts.ContainsKey(hud.m_Id))
            {
                return;
            }
            m_HpTexts.Remove(hud.m_Id);

            hud.m_OwnerEntityId = -1;
            hud.m_Trans.localPosition = new Vector3(0, 1000000, 0);
            //if(hud.m_CanvasGroup != null)
            //{
            //    hud.m_CanvasGroup.alpha = 0;
            //}
            if (hud.m_Trans.gameObject.activeSelf)
            {
                hud.m_Trans.gameObject.SetActive(false);
            }
            List<HpText> texts = null;
            var type = hud.m_Type;
            if (m_HpTextCaches.ContainsKey(type))
            {
                texts = m_HpTextCaches[type];
            }
            else
            {
                texts = new List<HpText>();
                m_HpTextCaches.Add(type, texts);
            }
            texts.Add(hud);
        }

        public static void ResetOwnerHpTexts(int entityId)
        {
            var toRemove = new List<int>(m_HpTexts.Count);
            foreach (var kv in m_HpTexts)
            {
                if (kv.Value.m_OwnerEntityId == entityId && kv.Value.m_State == HpText.State.Playing)
                {
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var key in toRemove)
            {
                var hpt = m_HpTexts[key];
                hpt.m_State = HpText.State.Cache;
                PoolHpText(hpt);
            }
        }

        //回收血条和伤害数字  暂时不处理
        public static void OnBattleFinish()
        {

        }
    }
}
