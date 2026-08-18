using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class BloodCtrl : RawImage
{
    /// <summary>
    /// 血条的值
    /// </summary>
    public Slider BloodSlider;
    /// <summary>
    /// 血量图片红色
    /// </summary>
    public Texture RedTexture;
    /// <summary>
    /// 血量图片绿色
    /// </summary>
    [SerializeField]
    public Texture GreenTextre;

    public float ShowRedThreshold = 0.2f;
    /// <summary>
    /// 重写父类方法
    /// </summary>
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (BloodSlider != null)
        {
            float Value = BloodSlider.value;
            uvRect = new Rect(0, 0, Value, 1);

            //血量为0时，没有血量图片
            if (Value == 0)
            {
                texture = null;
            }
            //血量大于0，小于最大血量的20%时，血量为红色
            else if (Value < BloodSlider.maxValue * ShowRedThreshold && Value > 0)
            {
                texture = RedTexture;
            }
            //血量大于等于最大血量的20%时，血量为绿色
            else if (Value >= BloodSlider.maxValue * ShowRedThreshold)
            {
                texture = GreenTextre;
            }

        }

    }
}
