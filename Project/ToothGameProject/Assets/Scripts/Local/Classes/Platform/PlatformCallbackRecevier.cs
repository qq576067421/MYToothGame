using System.Collections;
using UnityEngine;

namespace GameDll
{
    public class PlatformCallbackRecevier : MonoBehaviour
    {
        private void OnEnable()
        {
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
        public void OnReceivePlatformMessage(string data)
        {
            //Debug.LogError("LCL  platform message:" + data);

            var cmd_value = data.Split("|");
            string cmd = cmd_value[0];
            string value = "";
            if(cmd_value.Length == 2)
            {
                value = cmd_value[1];   
            }
            else
            {
                value = data;
            }

            RenderEvent.Event.OnPlatformMessageReceived(cmd, value);

        }


    }
}