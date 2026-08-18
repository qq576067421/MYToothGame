using System;
using System.Collections.Generic;

using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameHot
{
    //注意热更新这边的委托不支持数组和List等形式，要用需要用DelegateWrapper封装一次
    public class DispatcherHF
    {
        public Action OnStartTestPerformanceEvent = () => { };
        public Action<int> OnPlayerEnterEvent = (id) => { };
        public Action<string, bool> OnRedPointValueChange = (str, red) => { };
        public Action OnCameraPositionChangedEvent = () => { };
        public Action<int> OnStartLoadLevelEvent = (id) => { };
        public Action<int, int> OnNetStateChanged = (arg1, arg2) => { };
        public Action<long> OnChangeMySelfEvent = (arg) => { };
        public Action<int> OnPingChangeEvent = (arg) => { };
        public Action<int> OnCameraTargetChangedEvent = (arg) => { };
        public Action OnReceivedHomeRankDatas = () => { };
        public Action OnReceivedMasterRankDatas = () => { };


        public Action<string, string> OnAddSystemChat = (sender, content) => { };




        public Action OnFinishStartGameCountdown = () => { };

        public Action OnGetAllMailEvent = () => { };
        public Action<long> OnGetMailContentEvent = (mailId) => { };


        public Action OnDelAllReadMailsEvent = () => { };
        public Action OnReceivePushMailEvent = () => { };

        public Action OnCheckGuideGroup = () => { };
        public Action<guide_group, guide_step_id> OnGuideStepFinish = (guide_group group, guide_step_id guideId) => { };


        public Action<int> OnTimeChangeEvent = (arg) => { };

        public Action OnGetBagItems = () => { };
        public Action OnMoneyChanged = () => { };


        public Action<long, int> OnItemGetFly = (item_id, count) => { };
        public Action<int, int> OnMoneyGetFly = (type, count) => { };
        public Action<int> OnGemGetFly = (count) => { };

        public Action OnUpdateChooseZone = () => { };

        public Action<int> OnChangeNameResult = (rst) => { };

        public Action OnFunctionOpened = () => { };

        public Action OnOpenNewLoginSDK = () => { };
        public Action<bool> OnLoginSDKResult = (hr) => { };

        public Action<WindowBase> OnUIClosedEvent = (name) => { };
        public Action<WindowBase> OnUIOpenedEvent = (name) => { };

        public Action<InputAction.CallbackContext, InputType> OnInputAction = (InputAction.CallbackContext context, InputType inputType) => { };
        public Action<InputAction.CallbackContext> OnEscapPressed = (InputAction.CallbackContext context) => {};
    }
}
