namespace UnityUI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using System;

    public class UITypeWords : BaseMeshEffect
    {
        [SerializeField]
        private int m_TextIndex;

        private UIVertex m_Vertex = new UIVertex();

        //记录刷新时间
        private float m_TimeCount = 0;

        private float m_CharTime = 0;

        //是否开始显示打字
        private int m_State = 0;

        //每个字的出现间隔
        private float m_TimeSpace = 0;

        //要显示的Text组件
        private Text m_Text;

        private int m_AlphaIndex = 0;

        private float[] m_ArrVertex;

        private int m_AniComplete = 0;
        private int m_CharLen = 0;
        private int m_HasInit = 0;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }
            if (m_State == 0)
            {
                return;
            }
            m_CharLen = vh.currentVertCount / 4;
            //文本长度
            if (m_State == 1)
            {
                for (var i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(ref m_Vertex, i);
                    Color c = m_Vertex.color;
                    c.a = 0;
                    m_Vertex.color = c;
                    vh.SetUIVertex(m_Vertex, i);
                }
                m_State = 2;
                return;
            }
            int vCount = 4;
            int vertexIndex = 0;
            int charLen = m_CharLen;
            for (int i = 0; i < charLen; i++)
            {
                float alpha = m_ArrVertex[i];
                for (int j = 0; j < vCount; j++)
                {
                    if (vertexIndex < vh.currentVertCount)
                    {
                        vh.PopulateUIVertex(ref m_Vertex, vertexIndex);
                        Color c = m_Vertex.color;
                        c.a = alpha;
                        m_Vertex.color = c;
                        vh.SetUIVertex(m_Vertex, vertexIndex);
                    }
                    vertexIndex++;
                }
            }
            if (m_AlphaIndex >= charLen && m_State > 0)
            {
                m_State = 0;
                m_AniComplete = 1;
            }

        }
        protected override void Awake()
        {
            m_Text = gameObject.GetComponent<Text>();
            m_TextIndex = 0;
            m_Text.text = "";
        }
        protected override void Start()
        {
        }
        void Update()
        {
            if (m_State == 2)
            {
                if (m_HasInit == 0)
                {
                    m_HasInit = 1;
                    m_ArrVertex = new float[m_CharLen];
                    for (int i = 0; i < m_CharLen; i++)
                    {
                        m_ArrVertex[i] = 0;
                    }
                }
                doText();
                doAlphaText();
            }
            if (m_AniComplete == 1)
            {
                m_AniComplete = 0;
                onTextAniComplete();
            }
        }
        private void doText()
        {
            int charLen = m_CharLen;
            if (m_TextIndex >= charLen)
            {
                return;
            }
            if (Time.time - m_CharTime > 0.05)
            {
                m_CharTime = Time.time;
                m_TextIndex++;
            }
        }
        private void doAlphaText()
        {
            if (Time.time - m_TimeCount <= 0.01)
            {
                return;
            }
            m_TimeCount = Time.time;
            int charLen = m_CharLen;
            bool needModify = false;
            for (int i = m_AlphaIndex; i < m_TextIndex && i < charLen; i++)
            {
                float alpha = m_ArrVertex[i];
                alpha += 0.1f;
                needModify = true;
                if (alpha >= 1)
                {
                    m_AlphaIndex++;
                    alpha = 1;
                }
                m_ArrVertex[i] = alpha;
            }
            if (needModify)
            {
                graphic.SetVerticesDirty();
            }
        }
        public void PlayAni(string contents)
        {
            if (m_State > 0)
            {
                return;
            }
            m_Text.text = contents;
            m_HasInit = 0;
            m_AlphaIndex = 0;
            m_State = 1;
            m_TextIndex = 0;
            graphic.SetVerticesDirty();
        }
        public void stopAni()
        {
            m_State = 0;
        }
        public void ShowText()
        {
            if (m_HasInit == 0)
            {
                return;
            }
            m_AlphaIndex = m_CharLen;
            m_TextIndex = m_CharLen;
            for (int i = 0; i < m_CharLen; i++)
            {
                m_ArrVertex[i] = 1.0f;
            }
            graphic.SetVerticesDirty();
        }
        private void onTextAniComplete()
        {

        }
        public void test()
        {
            PlayAni("这是一段剧情对话,<color=#FF0000>尽量长一点</color>方便测试.");
        }
    }
}
