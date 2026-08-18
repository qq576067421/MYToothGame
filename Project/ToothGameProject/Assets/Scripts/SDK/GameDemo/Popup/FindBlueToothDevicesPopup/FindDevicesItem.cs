
using UnityEngine;
using UnityEngine.UI;
using static YouDooSDKConstants;

public class FindDevicesItem : MonoBehaviour
{

    [SerializeField] private Text TextAddress;

    [SerializeField] private Text TextStatus;

    [SerializeField] private Text TextName;

    [SerializeField] private Image ImageBg;

    [SerializeField] private Color[] ColorBg;

    private int _index;

    public void InitDetailInfoItem(DeviceStatusInfo blueToothDevices, int index, int curSelectIndex)
    {
        _index = index;
        TextAddress.text = blueToothDevices.address;
        TextStatus.text = blueToothDevices.status;
        TextName.text = blueToothDevices.name;
        SetSelectedState(curSelectIndex);
    }

    public void SetSelectedState(int curSelectIndex)
    {
        ImageBg.color = curSelectIndex == _index ? ColorBg[0] : ColorBg[1];
    }

}
