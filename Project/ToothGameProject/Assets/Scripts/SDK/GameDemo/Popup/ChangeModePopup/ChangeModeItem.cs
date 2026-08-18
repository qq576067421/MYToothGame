
using UnityEngine;
using UnityEngine.UI;

public class ChangeModeItem : MonoBehaviour
{

    [SerializeField] private Text TitleText;

    [SerializeField] private Image ImageBg;

    [SerializeField] private Color[] ColorBg;

    private int _index;

    public void InitDetailInfoItem(string title, int index, int curSelectIndex)
    {
        _index = index;
        TitleText.text = title;
        SetSelectedState(curSelectIndex);
    }

    public void SetSelectedState(int curSelectIndex)
    {
        ImageBg.color = curSelectIndex == _index ? ColorBg[0] : ColorBg[1];
    }

}
