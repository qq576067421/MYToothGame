using UnityEngine;
using AmazingAssets.CurvedWorld;

namespace GameDll
{
    // Curve World 视觉偏移工具：返回单位/目标在弯折画面中的视觉位置（仍用逻辑空间坐标表示）。
    // 激光（trajectory=3）与对应瞄准线按视觉位置瞄准与判定，直线渲染即可落在被弯折画下去的单位身上。
    // 与 HpBar 的锚点补偿同源（CurvedWorldUtilities.TransformPosition 正向弯折）。
    internal static class CurveWorldVisualUtility
    {
        // CurvedWorld_CylindricalRolloff_Z_ID1.cginc 里的材质级弯曲开关，>0.5 时该材质不做弯折
        public const string BendOffPropertyName = "_CurvedWorldBendOff";

        private static CurvedWorldController s_Controller;
        private static MaterialPropertyBlock s_BendOffPropertyBlock;

        public static Vector3 ApplyVisualOffset(Vector3 worldPosition)
        {
            var controller = ReadController();
            if (controller == null)
            {
                return worldPosition;
            }

            return CurvedWorldUtilities.TransformPosition(worldPosition, controller);
        }

        public static Vector3 ReadVisualOffset(Vector3 worldPosition)
        {
            return ApplyVisualOffset(worldPosition) - worldPosition;
        }

        // 表现（直线）口径特效的统一弯折开关：激光相关的落点/受击特效都放在“视觉坐标”上，
        // 材质必须同时关闭 Curve World 弯折，否则会被世界再弯一次、位置（尤其 Y 方向）对不上。
        // 特效回池复用前传 false 恢复默认，避免直线口径泄漏给其它特效。
        public static void SetBendOff(RenderEff eff, bool bendOff)
        {
            if (eff == null || !eff.IsLoaded())
            {
                return;
            }

            var renderObj = eff.GetRenderObj();
            if (renderObj == null)
            {
                return;
            }

            var renderers = renderObj.GetComponentsInChildren<Renderer>(true);
            if (!bendOff)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].SetPropertyBlock(null);
                }

                return;
            }

            if (s_BendOffPropertyBlock == null)
            {
                s_BendOffPropertyBlock = new MaterialPropertyBlock();
                s_BendOffPropertyBlock.SetFloat(BendOffPropertyName, 1f);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(s_BendOffPropertyBlock);
            }
        }

        private static CurvedWorldController ReadController()
        {
            if (s_Controller == null)
            {
                // 场景切换后控制器会被销毁（Unity 假 null），此时重新查找
                s_Controller = Object.FindObjectOfType<CurvedWorldController>();
            }

            return s_Controller;
        }
    }
}
