using UnityEngine;
using UnityEngine.UI;

namespace UnityUI
{
    /// <summary>
    /// RichTextEx 演示脚本
    /// 挂载到场景中任意 GameObject 上即可运行演示。
    /// 会自动创建一个 Canvas，并展示多种图文混排效果。
    /// </summary>
    public class RichTextExDemo : MonoBehaviour
    {
        private RichTextEx m_ChatText;
        private RichTextEx m_SkillText;
        private RichTextEx m_QuestText;
        private RichTextEx m_StyleText;

        private void Start()
        {
            // 创建Canvas
            var canvasGo = new GameObject("DemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 背景
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            float yOffset = 400;

            // 1. 聊天示例
            m_ChatText = CreateRichText(canvasGo.transform, "ChatDemo", new Vector2(0, yOffset), new Vector2(800, 60));
            string chatText = "玩家：<color=#FF0000>[世界]</color> 获得了<img=texture_set/item:gold_icon,24,24>x100";
            m_ChatText.SetText(chatText);
            CreateLabel(canvasGo.transform, "聊天示例", new Vector2(-420, yOffset + 15));
            yOffset -= 100;

            // 2. 技能描述
            m_SkillText = CreateRichText(canvasGo.transform, "SkillDemo", new Vector2(0, yOffset), new Vector2(800, 100));
            string skillDesc = "<size=28><color=#FFFF00>火球术</color></size>\n" +
                "向目标发射一枚火球，造成200点火焰伤害，" +
                "并有<color=#FF0000>50%</color>几率造成<img=texture_set/effect:burn,16,16>灼烧效果";
            m_SkillText.SetText(skillDesc);
            CreateLabel(canvasGo.transform, "技能描述", new Vector2(-420, yOffset + 35));
            yOffset -= 140;

            // 3. 任务说明
            m_QuestText = CreateRichText(canvasGo.transform, "QuestDemo", new Vector2(0, yOffset), new Vector2(800, 100));
            string questDesc = "任务目标：\n" +
                "1. 击败<img=texture_set/monster:goblin,20,20>哥布林 x10\n" +
                "2. 收集<img=texture_set/item:herb,20,20>草药 x5";
            m_QuestText.SetText(questDesc);
            CreateLabel(canvasGo.transform, "任务说明", new Vector2(-420, yOffset + 35));
            yOffset -= 140;

            // 4. 富文本样式展示
            m_StyleText = CreateRichText(canvasGo.transform, "StyleDemo", new Vector2(0, yOffset), new Vector2(800, 120));
            string styleText = "普通文本 <b>粗体</b> <i>斜体</i> <color=#00FF00>绿色</color> " +
                "<size=32>大号</size> <u>下划线</u> <s>删除线</s>\n" +
                "<a=https://example.com>点击这里</a> 超链接测试\n" +
                "中文标点不断行测试：这里是一段很长的文字，用来测试中文标点符号的换行规则。";
            m_StyleText.SetText(styleText);
            m_StyleText.OnHyperlinkClick += url => Debug.Log("超链接点击: " + url);
            m_StyleText.OnImageClick += img => Debug.Log("图片点击: " + img.abName + ":" + img.assetName);
            CreateLabel(canvasGo.transform, "样式展示", new Vector2(-420, yOffset + 45));
        }

        private RichTextEx CreateRichText(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RichTextEx));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var richText = go.GetComponent<RichTextEx>();
            richText.color = Color.white;
            return richText;
        }

        private void CreateLabel(Transform parent, string text, Vector2 pos)
        {
            var go = new GameObject("Label_" + text, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 30);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = 20;
            t.color = Color.yellow;
            t.alignment = TextAnchor.MiddleRight;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
