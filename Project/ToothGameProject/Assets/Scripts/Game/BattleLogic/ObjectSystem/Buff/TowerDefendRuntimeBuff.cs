using MonoBean;

namespace GameDll
{
    // 兼容保留：当前最终 Buff 表已经不再通过 1101 承载通用属性语义。
    // 这里保留同名类型只为避免工程引用断裂；真实效果统一回到通用 Buff/技能逻辑。
    public sealed class TowerDefendRuntimeBuff : Buff
    {
        protected override void ChangeProperties()
        {
            base.ChangeProperties();
        }
    }
}
