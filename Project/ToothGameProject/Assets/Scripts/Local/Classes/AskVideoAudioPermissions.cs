using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public class AskVideoAudioPermissions
    {
#if UNITY_ANDROID
                public bool CheckSdkAudioPermissionsAuth()
        {
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO");
        }
        public bool CheckSdkVideoPermissionsAuth()
        {
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.CAMERA");
        }

        public void OnRequestUserPermissions(bool audioOnly, Action<bool> callBack)
        {
            List<string> permissions = new List<string>();
            permissions.Add("android.permission.RECORD_AUDIO");
            if(!audioOnly)
            {
                permissions.Add("android.permission.CAMERA");
            }
            UnityEngine.Android.PermissionCallbacks pc = new UnityEngine.Android.PermissionCallbacks();

            Dictionary<string, int> permission_states = new Dictionary<string, int>();
            foreach (var per in permissions)
            {
                if (!permission_states.ContainsKey(per))
                {
                    //0表示初始化  1表示同意 2表示不同意 3表示不同意且不再问
                    permission_states.Add(per, 0);
                }
            }

            Action check_is_all = () =>
            {
                bool is_all_config = true;
                foreach (var kv in permission_states)
                {
                    if (kv.Value == 0)
                    {
                        is_all_config = false;
                        break;
                    }
                }
                if (is_all_config)
                {
                    bool is_all_agree = true;
                    foreach (var kv in permission_states)
                    {
                        if (kv.Value == 2 || kv.Value == 3)
                        {
                            is_all_agree = false;
                            break;
                        }
                    }

                    callBack(is_all_agree);
                }
            };

            pc.PermissionDenied += (permission) =>
            {
                if (!string.IsNullOrEmpty(permission))
                {
                    Debug.Log("PermissionDenied:  " + permission);

                    if (permission_states.ContainsKey(permission))
                    {
                        permission_states[permission] = 2;
                        check_is_all();
                    }

                }
                else
                {
                    Debug.Log("PermissionDenied:  None");
                }
            };
            pc.PermissionDeniedAndDontAskAgain += (permission) =>
            {
                if (!string.IsNullOrEmpty(permission))
                {
                    Debug.Log("PermissionDeniedAndDontAskAgain:  " + permission);
                    if (permission_states.ContainsKey(permission))
                    {
                        permission_states[permission] = 3;
                        check_is_all();
                    }
                }
                else
                {
                    Debug.Log("PermissionDeniedAndDontAskAgain:  None");
                }
            };
            pc.PermissionGranted += (permission) =>
            {
                if (!string.IsNullOrEmpty(permission))
                {
                    Debug.Log("PermissionGranted:  " + permission);
                    if (permission_states.ContainsKey(permission))
                    {
                        permission_states[permission] = 1;
                        check_is_all();
                    }
                }
                else
                {
                    Debug.Log("PermissionGranted:  None");
                }
            };
            foreach (var p in permissions)
            {
                Debug.Log("请求权限：" + p);
            }
            UnityEngine.Android.Permission.RequestUserPermissions(permissions.ToArray(), pc);
        }

#elif UNITY_IOS || UNITY_IPHONE


#else
        public bool CheckSdkAudioPermissionsAuth()
        {
            return true;
        }
        public bool CheckSdkVideoPermissionsAuth()
        {
            return true;
        }

        public void OnRequestUserPermissions(bool audioOnly, Action<bool> callBack)
        {
            callBack(true);
        }
#endif
    }
}
