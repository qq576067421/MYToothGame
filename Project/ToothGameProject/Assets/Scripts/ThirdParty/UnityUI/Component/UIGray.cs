using UnityEngine;
using System.Collections;
using UnityEngine.UI;
namespace UnityUI
{
    [ExecuteInEditMode]
    [AddComponentMenu("UITools/Others/UIGray")]
    public class UIGray : MonoBehaviour
    {
        public Graphic m_Graphics;

        private bool _Gray;
        public bool Gray
        {
            set
            {
                _Gray = value;
                OnGray();
            }
            get
            {
                return _Gray;
            }
        }
        void Start()
        {
            OnGray();
        }
        void OnGray()
        {
            var graphic = m_Graphics;
            if (graphic != null)
            {
                //暂时不支持默认含有材质的UI控件的置灰，如有需要，可以在逻辑里面具体问题具体写
                if (_Gray)
                {
                    if (graphic.material != null && graphic.material.name != "Default UI Material")
                    {
                        return;
                    }
                    graphic.material = new Material(ShaderManager.GetShader("Custom/UI/Transparent Colored Gray Stencil"));
                    graphic.material.name = "UIGrayMaterial";
                }
                else
                {
                    if (graphic.material != null && graphic.material.name == "UIGrayMaterial")
                    {
                        graphic.material = null;
                    }
                }
            }
        }
    }
}