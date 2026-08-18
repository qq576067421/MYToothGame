using GameDll;
using ICSharpCode.SharpZipLib.Zip;
using LCL;
using MonoBean;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityUI;

public class RenderAPI
{
    #region 平台相关接口

    public static void SystemCopyBuffer(string info)
    {
        UnityEngine.GUIUtility.systemCopyBuffer = info;
    }
    public static object GetSignature()
    {
        try
        {
#if UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android)
            {
                AndroidJavaClass Player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject Activity = Player.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject PackageManager = Activity.Call<AndroidJavaObject>("getPackageManager");

                string packageName = Activity.Call<string>("getPackageName");

                int GET_SIGNATURES = PackageManager.GetStatic<int>("GET_SIGNATURES");
                AndroidJavaObject PackageInfo = PackageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, GET_SIGNATURES);
                AndroidJavaObject[] Signatures = PackageInfo.Get<AndroidJavaObject[]>("signatures");

                if (Signatures != null && Signatures.Length > 0)
                {
                    return Signatures[0];
                }
            }
            return null;
#else
            return null;
#endif
        }
        catch (Exception e)
        {
            UDebug.LogError(e.ToString());
            return null;
        }
    }

    public static void EditorLogError(string error)
    {
#if UNITY_EDITOR
        UDebug.LogError(error);
#endif
    }

    public static string GetHashCode(object obj)
    {
        try
        {
#if UNITY_ANDROID
            if (Application.platform == RuntimePlatform.Android)
            {
                AndroidJavaClass Player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject Activity = Player.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject PackageManager = Activity.Call<AndroidJavaObject>("getPackageManager");

                string packageName = Activity.Call<string>("getPackageName");

                int GET_SIGNATURES = PackageManager.GetStatic<int>("GET_SIGNATURES");
                AndroidJavaObject PackageInfo = PackageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, GET_SIGNATURES);
                AndroidJavaObject[] Signatures = PackageInfo.Get<AndroidJavaObject[]>("signatures");

                if (Signatures != null && Signatures.Length > 0)
                {
                    if (obj == null)
                    {
                        return "";
                    }
                    byte[] bytes = ((AndroidJavaObject)obj).Call<byte[]>("toByteArray");

                    var md5String = GetMD5Hash(bytes);
                    md5String = md5String.ToUpper();

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < md5String.Length; ++i)
                    {
                        if (i > 0 && i % 2 == 0)
                        {
                            sb.Append(':');
                        }
                        sb.Append(md5String[i]);
                    }

                    return sb.ToString();

                }
            }
            return "yourmd5";
#else

            return "NoneAndroid";
#endif
        }
        catch (Exception e)
        {
            UDebug.LogError(e.Message);
            return "";
        }
    }

    public static string GetMD5Hash(byte[] bytedata)
    {
        try
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(bytedata);



            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
        }
    }
    public static string GetMD5Hash(string str)
    {
        try
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));



            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
        }
    }

    public static Vector3 GetColliderHit(Vector3 startPos, Vector3 endPos, Collider collider)
    {
        if (collider == null)
        {
            return endPos;
        }
        var hitPos = collider.ClosestPointOnBounds(startPos);
        //UDebug.Log("hitPos:" + hitPos.ToString() + " startPos:" + startPos.ToString() + " endPos:" + endPos.ToString());
        return hitPos;
    }

    public static string APKMD5 = "";
    public static void GetAndroidAPKMD5Hash(byte[] bytedata)
    {
        try
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(bytedata);



            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            APKMD5 = sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception("GetMD5Hash() fail,error:" + ex.Message);
        }
    }


    #endregion
    #region 常用模块接口

    #endregion
    public static string GetStringAsUTF8(string value)
    {
        byte[] rawData = Encoding.Default.GetBytes(value);
        string reencoded = Encoding.UTF8.GetString(rawData);
        return reencoded;
    }
    //0表示中文，1表示英文，以此类推
    private static int m_LanguageVer = 0;
    private static string m_LanNameShort = "CN";
    public static bool m_DebugMobile = false;
    //public static List<UIButtonScale> uIButtonScales;
    private static List<ActiveButtons> m_CurActiveButtons = new List<ActiveButtons>();
    private static Button m_CurActiveLeftButton;
    private static Button m_CurActiveRightButton;
    private static Button m_CurActiveUpButton;
    private static Button m_CurActiveDownButton;

    // 当前选中的行与列索引
    private static int m_CurrentRow = 0;
    private static int m_CurrentCol = 0;
    private static bool m_HasCurrentSelection = false;
    public static void SetCurrentCol(int col)
    {
        m_CurrentCol = col;
    }
    public static int GetCurrentCol()
    {
        return m_CurrentCol;
    }
    public static int GetCurrentRow()
    {
        return m_CurrentRow;
    }
    public static bool IsUseCSV()
    {
        return true;
    }

    public static bool IsHotFix()
    {
        return false;
    }

    public static string ReadGLESVersion()
    {
        string version = "0";
#if (UNITY_ANDROID && !UNITY_EDITOR) || ANDROID_CODE_VIEW
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject curApplication = currentActivity.Call<AndroidJavaObject>("getApplication"))
                    {
                        using (AndroidJavaObject curSystemService = curApplication.Call<AndroidJavaObject>("getSystemService", "activity"))
                        {
                            using (AndroidJavaObject curConfigurationInfo = curSystemService.Call<AndroidJavaObject>("getDeviceConfigurationInfo"))
                            {
                                int reqGlEsVersion = curConfigurationInfo.Get<int>("reqGlEsVersion");
                                using (AndroidJavaClass curInteger = new AndroidJavaClass("java.lang.Integer"))
                                {
                                    version = curInteger.CallStatic<string>("toString",reqGlEsVersion,16);
                                }
                            }
                        }
                    }
                } 
            }
        }
        catch (Exception e)
        {
             UDebug.LogError("GetOpenGL, Exception: " + e.ToString());
        }
#elif (UNITY_IOS && !UNITY_EDITOR) || IOS_CODE_VIEW
        version = "-1";
#endif
        return version;
    }
    //public static bool IsSmallVersion()
    //{
    //    return Main.GetInstance().m_IsSmall;
    //}
    public static bool IsWebGL()
    {
#if UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }
    public static bool IsWebSocket()
    {
        if (IsWebGL())
        {
            return true;
        }
#if USE_WEBSOCKET
        return true;
#else
        return false;
#endif
    }
    public static void InitLanguage(int languageVer, string lanCfgName, int fontSizeFactor = 100)
    {
        m_LanguageVer = languageVer;
        m_LanNameShort = lanCfgName;
        DataManager.LoadLanguage(lanCfgName);

        UnityUI.LUIText.OnSetTextLanguageCall = GetTextByLanId;
        UnityUI.LUITextMesh.OnSetTextLanguageCall = GetTextByLanId;

        //int fontSizeFactor = 100;
        //if(languageVer == 0 || languageVer == 2 || languageVer == 3 || languageVer == 4)
        //{
        //    //中文
        //}
        //else
        //{
        //    fontSizeFactor = 80;
        //}

        UnityUI.LUIText.m_LanguageFontSizeFactor = fontSizeFactor;
        UnityUI.LUITextMesh.m_LanguageFontSizeFactor = fontSizeFactor;

        UnityUI.ArtText.OnSetTextLanguageCall = GetTextByLanId;
        UnityUI.ArtText.OnSetTextFontCall = GetTextFont;
    }

    private static Font m_Font;
    public static Font GetTextFont()
    {
        return m_Font;
    }
    public static void LoadFont(string font_url, Action finishCall)
    {
        UIRes.LoadPrefabAsync(typeof(UnityEngine.Font), font_url, Tool.GetAssetName(font_url), (rd, obj) =>
        {
            m_Font = rd.m_Obj as Font;
            finishCall();
        });
    }
    //内部使用，暂时不当做常规接口使用
    public static string ReadLanguageByKey(string key)
    {
        if (m_LanguageVer == 0)
        {
            var cfg = t_languageCNBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else if (m_LanguageVer == 1)
        {
            var cfg = t_languageENBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else if (m_LanguageVer == 2)
        {
            var cfg = t_languageJPBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else if (m_LanguageVer == 3)
        {
            var cfg = t_languageKRBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else if (m_LanguageVer == 4)
        {
            var cfg = t_languageCNFTBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else if (m_LanguageVer == 5)
        {
            var cfg = t_languageVNBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
        else
        {
            var cfg = t_languageENBean.GetConfig(key);
            if (cfg != null)
            {
                return cfg.t_content;
            }
            else
            {
                return key + ".";
            }
        }
    }
    public static int ConvertSystemLan2GameLanId(int system)
    {
        SystemLanguage lan = (SystemLanguage)system;
        if (lan == SystemLanguage.ChineseSimplified)
        {
            return 0;
        }
        else if (lan == SystemLanguage.English)
        {
            return 1;
        }
        else if (lan == SystemLanguage.Japanese)
        {
            return 2;
        }
        else if (lan == SystemLanguage.Korean)
        {
            return 3;
        }
        else if (lan == SystemLanguage.ChineseTraditional)
        {
            return 4;
        }
        else if (lan == SystemLanguage.Vietnamese)
        {
            return 5;
        }
        else
        {
            return 1;
        }
    }
    public static string ConvertGameLanId2GameLanCfgName(int lan)
    {
        if (lan == 0)
        {
            return "CN";
        }
        else if (lan == 1)
        {
            return "EN";
        }
        else if (lan == 2)
        {
            return "JP";
        }
        else if (lan == 3)
        {
            return "KR";
        }
        else if (lan == 4)
        {
            return "CNFT";
        }
        else if (lan == 5)
        {
            return "VN";
        }
        else
        {
            return "EN";
        }
    }
    public static List<string> GetGameLanNames()
    {
        List<string> names = new List<string>()
        {
            "简体中文","English","日本語","한국어","繁體中文","Tiếng Việt"
        };
        return names;
    }
    public static string GetLanNameShort()
    {
        return m_LanNameShort;
    }
    public static string GetTextByLanId(string lanId)
    {
        return DataManager.GetLanguageByKey(lanId);
    }
    public static string GetTextByLanId(string lanId, params object[] param)
    {
        var hr = DataManager.GetLanguageByKey(lanId);
        if (param != null)
        {
            return string.Format(hr, param);
        }
        else
        {
            return hr;
        }
    }
    public static int VerLan()
    {
        return m_LanguageVer;
    }
    //用于文本需要换行的地方
    public static string ConvertMultiLineText(string oldText)
    {
        return oldText.Replace("\\n", "\n");
    }
    #region UI控件
    public static void SetEnableTextLineBreakFormatter(bool enable)
    {
        LUITextLineBreakFormatter.m_IsOpenFormatter = enable;
    }
    public static void SetTextLineBreakFormatterAvoidAtStartOfLine(List<char> list)
    {
        LUITextLineBreakFormatter.avoidAtStartOfLineDefault = list;
    }
    /// <summary>
    /// 判断组件是否为空
    /// </summary>
    /// <param name="com"></param>
    /// <returns></returns>
    /// 

    public static bool IsNull(Component com)
    {
        if (com == null || com.gameObject.Equals(null))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static bool IsNull(GameObject com)
    {
        if (com == null || com.Equals(null))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    /// <summary>
    /// 设置文本组件的文本和颜色
    /// </summary>
    /// <param name="txt"></param>
    /// <param name="str"></param>
    /// <param name="color"></param>
    public static void SetText(Text txt, string str)
    {
        if (str == null)
        {
            str = "";
        }
        if (IsNull(txt))
        {
            UDebug.LogError("SetText txt nil, str:" + str);
            return;
        }
        if (txt is LUIText)
        {
            var uit = (LUIText)txt;
            //配置表等输入的时候 只需要输入\n就可以了，c#会自动添加一个\， 变成\\n，然后通过如下代码转换正确
            if (uit.CheckAndReplaceMultiLine)
            {
                str = str.Replace("\\n", "\n");
            }
        }
        txt.text = str;

        if (txt is LUIText)
        {
            ((LUIText)txt).FixLineBreakFormatter();
        }
        else
        {
            if (LUITextLineBreakFormatter.m_IsOpenFormatter)
            {
                LUITextLineBreakFormatter.OnTextContentChanged(txt);
            }
        }
    }
    public static void SetText(TMP_Text txt, string str)
    {
        if (str == null)
        {
            str = "";
        }
        if (IsNull(txt))
        {
            UDebug.LogError("SetText txt nil, str:" + str);
            return;
        }
        if (txt is LUITextMesh)
        {
            var uit = (LUITextMesh)txt;
            if (uit.CheckAndReplaceMultiLine)
            {
                str = str.Replace("\\n", "\n");
            }
        }

        txt.text = str;

        if (txt is LUITextMesh)
        {
            ((LUITextMesh)txt).FixLineBreakFormatter();
        }
    }
    public static void SetTextNumber(Text txt, long str, long bigger = 0)
    {
        if (IsNull(txt))
        {
            UDebug.LogError("SetText txt nil, str:" + str);
            return;
        }

        if (bigger > 0)
        {
            txt.text = NumberExtension.ToLargeNumSimple(str);
        }
        else
        {
            txt.text = str.ToString();
        }

    }
    public static void SetTextNumber(TMP_Text txt, long str, long bigger = 0)
    {
        if (IsNull(txt))
        {
            UDebug.LogError("SetText txt nil, str:" + str);
            return;
        }

        if (bigger > 0)
        {
            txt.text = NumberExtension.ToLargeNumSimple(str);
        }
        else
        {
            txt.text = str.ToString();
        }
    }
    public static void SetTextLan(Text txt, string lanId, params object[] param)
    {
        if (txt is LUIText)
        {
            var uit = (LUIText)txt;
            uit.LanguageId = lanId;
            uit.InputType = TextInputType.ID;
            uit.StyleParams = param;
        }
        if (param == null || param.Length == 0)
        {
            SetText(txt, RenderAPI.GetTextByLanId(lanId));
        }
        else
        {
            SetText(txt, string.Format(RenderAPI.GetTextByLanId(lanId), param));
        }
    }
    public static void SetTextLan(TMP_Text txt, string lanId, params object[] param)
    {
        if (txt is LUITextMesh)
        {
            var uit = (LUITextMesh)txt;
            uit.LanguageId = lanId;
            uit.InputType = TextInputType.ID;
            uit.StyleParams = param;
        }
        if (param == null || param.Length == 0)
        {
            SetText(txt, RenderAPI.GetTextByLanId(lanId));
        }
        else
        {
            SetText(txt, string.Format(RenderAPI.GetTextByLanId(lanId), param));
        }
    }

    private static void OnClickButtonSoundCommon(int _soundId)
    {
        if (_soundId == int.MinValue)
        {
            _soundId = m_GlobalSoundId_ButtonClick;
        }
        if (_soundId != 0)
        {
            AudioManager.GetInstance().Play2D(_soundId);
        }
    }

    //public static void SetTextLan(InlineText txt, string lanId, Action<string, int> OnClick, int soundId = int.MinValue, params object[] param)
    //{
    //    if(IsNull(txt))
    //    {
    //        return;
    //    }
    //    if (txt is LUIText)
    //    {
    //        var uit = (LUIText)txt;
    //        uit.LanguageId = lanId;
    //        uit.InputType = TextInputType.ID;
    //        uit.StyleParams = param;
    //    }
    //    if (param == null || param.Length == 0)
    //    {
    //        SetText(txt, RenderAPI.GetTextByLanId(lanId));
    //    }
    //    else
    //    {
    //        SetText(txt, string.Format(RenderAPI.GetTextByLanId(lanId), param));
    //    }
    //    txt.OnHrefClick.RemoveAllListeners();
    //    var _onClick = OnClick;

    //    int _soundId = soundId;

    //    txt.OnHrefClick.AddListener((str, id) => 
    //    {
    //        if(_onClick != null)
    //        {
    //            OnClickButtonSoundCommon(_soundId);
    //            _onClick(str, id);
    //        }
    //    });
    //}
    //public static void SetText(InlineText txt, string str, Action<string, int> OnClick, int soundId = int.MinValue)
    //{
    //    if (str == null)
    //    {
    //        str = "";
    //    }
    //    if (IsNull(txt))
    //    {
    //        UDebug.LogError("SetText txt nil, str:" + str);
    //        return;
    //    }
    //    txt.text = str;

    //    txt.OnHrefClick.RemoveAllListeners();
    //    var _onClick = OnClick;

    //    int _soundId = soundId;

    //    txt.OnHrefClick.AddListener((str, id) =>
    //    {
    //        if (_onClick != null)
    //        {
    //            OnClickButtonSoundCommon(_soundId);
    //            _onClick(str, id);
    //        }
    //    });
    //}
    public static void SetText(InputField txt, string str)
    {
        if (str == null)
        {
            str = "";
        }
        if (IsNull(txt))
        {
            UDebug.LogError("SetText txt nil, str:" + str);
            return;
        }
        txt.text = str;
    }
    public static void SetTextLan(InputField txt, string lanId, params object[] param)
    {
        if (param == null || param.Length == 0)
        {
            SetText(txt, RenderAPI.GetTextByLanId(lanId));
        }
        else
        {
            SetText(txt, string.Format(RenderAPI.GetTextByLanId(lanId), param));
        }
    }
    /// <summary>
    /// 获取文本
    /// </summary>
    /// <param name="txt"></param>
    /// <returns></returns>
    public static string GetText(Text txt)
    {
        if (IsNull(txt))
        {
            UDebug.LogError("GetText(Text txt)  txt nill");
            return string.Empty;
        }
        return txt.text;
    }
    public static string GetText(InputField txt)
    {
        if (IsNull(txt))
        {
            UDebug.LogError("GetText(InputField txt)  txt nill");
            return string.Empty;
        }
        return txt.text;
    }
    /// <summary>
    /// 设置组件的世界位置信息
    /// </summary>
    /// <param name="com"></param>
    /// <param name="pos"></param>
    public static void SetPosition(Component com, Vector3 pos)
    {
        if (IsNull(com))
        {
            return;
        }
        com.transform.position = pos;
    }
    public static void SetPositionByOffset(Component com, float dx, float dy, float dz)
    {
        if (IsNull(com))
        {
            return;
        }
        Vector3 pos = com.transform.position;
        com.transform.position = pos + new Vector3(dx, dy, dz);
    }
    /// <summary>
    /// 获取组件的世界位置信息
    /// </summary>
    /// <param name="com"></param>
    /// <returns></returns>
    public static Vector3 GetPosition(Component com)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetPosition com is null");
            return Vector3.zero;
        }
        return com.transform.position;
    }

    public static Component GetComponent(Component com, Type _type, string path)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetComponent com is null");
            return null;
        }
        if (string.IsNullOrEmpty(path))
        {
            return com.GetComponent(_type);
        }
        else
        {
            var trans = com.transform.Find(path);
            if (trans != null)
            {
                return trans.GetComponent(_type);
            }
            else
            {
                return null;
            }
        }
    }
    public static Component GetComponent(GameObject com, Type _type, string path)
    {
        if (com != null)
        {
            return GetComponent(com.transform, _type, path);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 设置组件的本地位置信息
    /// </summary>
    /// <param name="com"></param>
    /// <param name="pos"></param>
    public static void SetLocalPosition(Component com, Vector3 pos)
    {
        if (IsNull(com))
        {
            return;
        }
        com.transform.localPosition = pos;
    }
    /// <summary>
    /// 获取组件本地位置信息
    /// </summary>
    /// <param name="com"></param>
    /// <returns></returns>
    public static Vector3 GetLocalPosition(Component com)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetLocalPosition com is null");
            return Vector3.zero;
        }
        return com.transform.localPosition;
    }

    /// <summary>
    /// 设置大小，这里一般是针对LocalScale的
    /// </summary>
    /// <param name="com"></param>
    /// <param name="scale"></param>
    public static void SetScale(Component com, Vector3 scale)
    {
        if (IsNull(com))
        {
            UDebug.LogError("SetScale com is null");
            return;
        }
        com.transform.localScale = scale;
    }
    public static Vector3 GetScale(Component com)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetScale com is null");
            return Vector3.one;
        }
        return com.transform.localScale;
    }

    public static void SetEulaRotation(Component com, Vector3 rotation)
    {
        if (IsNull(com))
        {
            UDebug.LogError("SetEulaRotation com is null");
            return;
        }
        com.transform.eulerAngles = rotation;
    }
    public static Vector3 GetEulaRotation(Component com)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetEulaRotation com is null");
            return Vector3.zero;
        }
        return com.transform.eulerAngles;
    }
    public static void SetLocalEulaRotation(Component com, Vector3 rotation)
    {
        if (IsNull(com))
        {
            UDebug.LogError("SetLocalEulaRotation com is null");
            return;
        }
        com.transform.localEulerAngles = rotation;
    }
    public static Vector3 GetLocalEulaRotation(Component com)
    {
        if (IsNull(com))
        {
            UDebug.LogError("GetLocalEulaRotation com is null");
            return Vector3.zero;
        }
        return com.transform.localEulerAngles;
    }
    /// <summary>
    /// 将组件的转换矩阵归一化
    /// </summary>
    /// <param name="com"></param>
    public static void ResetTransform(Component com, bool resetLocal)
    {
        if (IsNull(com))
        {
            UDebug.LogError("ResetTransform com is null");
            return;
        }
        Transform trans = com.transform;
        if (resetLocal)
        {
            trans.localScale = Vector3.one;
            trans.localEulerAngles = Vector3.zero;
            trans.localPosition = Vector3.zero;
        }
        else
        {
            trans.localScale = Vector3.one;
            trans.eulerAngles = Vector3.zero;
            trans.position = Vector3.zero;
        }

    }
    public static void ResetTransform(GameObject com, bool resetLocal)
    {
        if (IsNull(com))
        {
            UDebug.LogError("ResetTransform com is null");
            return;
        }
        Transform trans = com.transform;
        if (resetLocal)
        {
            trans.localScale = Vector3.one;
            trans.localEulerAngles = Vector3.zero;
            trans.localPosition = Vector3.zero;
        }
        else
        {
            trans.localScale = Vector3.one;
            trans.eulerAngles = Vector3.zero;
            trans.position = Vector3.zero;
        }

    }
    public static void ResetRectTransform(GameObject com, bool resetLocal)
    {
        if (IsNull(com))
        {
            UDebug.LogError("ResetTransform com is null");
            return;
        }
        RectTransform trans = com.transform as RectTransform;
        if (trans == null)
        {
            UDebug.LogError("transform无法转换为RectTransform组件， 名字：" + com.transform.name);
            return;
        }
        if (resetLocal)
        {
            trans.localScale = Vector3.one;
            trans.localEulerAngles = Vector3.zero;
            trans.localPosition = Vector3.zero;
            trans.anchoredPosition3D = Vector3.zero;
        }
        else
        {
            trans.localScale = Vector3.one;
            trans.eulerAngles = Vector3.zero;
            trans.position = Vector3.zero;
            trans.anchoredPosition3D = Vector3.zero;
        }

    }
    /// <summary>
    /// 设置组件的父对象，并且设置是否保持世界坐标不变化
    /// </summary>
    /// <param name="child"></param>
    /// <param name="parent"></param>
    /// <param name="worldPositionStays"></param>
    public static void SetParent(Component child, Transform parent, bool worldPositionStays)
    {
        if (IsNull(child))
        {
            UDebug.LogError("SetParent com is null");
            return;
        }
        child.transform.SetParent(parent, worldPositionStays);
    }
    public static void SetParent(GameObject child, GameObject parent, bool worldPositionStays)
    {
        if (IsNull(child))
        {
            UDebug.LogError("SetParent com is null");
            return;
        }
        child.transform.SetParent(parent.transform, worldPositionStays);
    }
    public static GameObject Instantiate(GameObject src)
    {
        GameObject dest = GameObject.Instantiate(src);
        Transform dest_trans = dest.transform;
        Transform src_trans = src.transform;
        dest_trans.SetParent(src_trans.parent);
        dest_trans.localPosition = src_trans.localPosition;
        dest_trans.localRotation = src_trans.localRotation;
        dest_trans.localScale = src_trans.localScale;
        return dest;
    }
    public static void BindUIMenu(UIWindow ui)
    {
        if(ui == null)
        {
            ClearUIMenu();
            return;
        }
        bool selectDefaultActiveButton = ui.m_DefRowIndex >= 0;
        ResetMenu(ui.m_ActiveButtons, Mathf.Max(0, ui.m_DefRowIndex), ui.m_DefColIndex,
            ui.m_ActiveLeftButton, ui.m_ActiveRightButton, ui.m_ActiveUpButton, ui.m_ActiveDownButton,
            selectDefaultActiveButton);
    }
    public static void ClearUIMenu()
    {
        // 遥控菜单是全局静态状态，清空时要先把上一份选中表现关掉，
        // 否则缓存窗口重新显示前会残留 choose 高亮。
        SetSelectionVisuals(false);
        m_CurActiveButtons.Clear();
        m_CurActiveLeftButton = null;
        m_CurActiveRightButton = null;
        m_CurActiveUpButton = null;
        m_CurActiveDownButton = null;
        m_CurrentRow = 0;
        m_CurrentCol = 0;
        m_HasCurrentSelection = false;
    }
    public static UnityEngine.Object Instantiate(UnityEngine.Object src)
    {
        var dest = UnityEngine.Object.Instantiate(src);
        return dest;
    }

    /// <summary>
    /// 获取组件的父对象
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public static Transform GetParent(Component child)
    {
        if (IsNull(child))
        {
            UDebug.LogError("GetParent com is null");
            return null;
        }
        return child.transform.parent;
    }

    public static Component AddComponent(Component addTo, Type addType)
    {
        if (IsNull(addTo))
        {
            return null;
        }
        return addTo.gameObject.AddComponent(addType);
    }
    public static Component AddComponent(GameObject addTo, Type addType)
    {
        if (IsNull(addTo))
        {
            return null;
        }
        return addTo.AddComponent(addType);
    }
    public static Component GetOrAddComponent(GameObject addTo, Type addType)
    {
        if (IsNull(addTo))
        {
            return null;
        }
        Component hrCom = addTo.GetComponent(addType);
        if (hrCom == null)
        {
            hrCom = addTo.AddComponent(addType);
        }
        return hrCom;
    }
    public static Component GetOrAddComponent(Component addTo, Type addType)
    {
        if (IsNull(addTo))
        {
            return null;
        }
        Component hrCom = addTo.gameObject.GetComponent(addType);
        if (hrCom == null)
        {
            hrCom = addTo.gameObject.AddComponent(addType);
        }
        return hrCom;
    }

    public static void SetActiveIfNeed(Component com, bool active)
    {
        if (IsNull(com))
        {
            return;
        }
        SetActiveIfNeed(com.gameObject, active);
    }
    public static void SetActiveIfNeed(GameObject com, bool active)
    {
        if (IsNull(com))
        {
            return;
        }
        if (active)
        {
            if (!com.activeSelf)
            {
                com.SetActive(true);
            }
        }
        else
        {
            if (com.activeSelf)
            {
                com.SetActive(false);
            }
        }
    }
    public static void SetActive(GameObject com, bool active)
    {
        if (IsNull(com))
        {
            return;
        }
        com.SetActive(active);
    }
    public static void SetActive(Component com, bool active)
    {
        if (IsNull(com))
        {
            return;
        }
        com.gameObject.SetActive(active);
    }
    public static bool GetActive(GameObject com)
    {
        if (IsNull(com))
        {
            return false;
        }
        return com.activeSelf;
    }
    public static bool GetActive(Component com)
    {
        if (IsNull(com))
        {
            return false;
        }
        return com.gameObject.activeSelf;
    }

    public static void SetRect(Component com, Vector2 center, Vector2 size)
    {
        if (IsNull(com))
        {
            return;
        }
        var rect = com.GetComponent<RectTransform>();
        if (rect != null)
        {
            SetRect(rect, center, size);
        }
    }
    public static void SetRect(RectTransform com, Vector2 center, Vector2 size)
    {
        if (IsNull(com))
        {
            return;
        }
        com.anchoredPosition = center;
        com.sizeDelta = size;
    }
    public static string RemoveRichTextColor(string str)
    {
        while (str.Contains("</color>") || str.Contains("</Color>"))
        {
            var startIndex = str.IndexOf("<");
            var endIndex = str.IndexOf(">");
            str = str.Remove(startIndex, endIndex - startIndex + 1);
        }
        return str;
    }
    public static void SetGray(Component com, bool gray, bool withChildren, bool ignoreText = true)
    {
        if (IsNull(com))
        {
            return;
        }
        if (withChildren)
        {
            var graphics = com.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g is LUIText)
                {
                    if (!ignoreText)
                    {
                        ((LUIText)g).SetGray(gray);
                    }
                }
                else if (g is LUITextMesh)
                {
                    if (!ignoreText)
                    {
                        ((LUITextMesh)g).SetGray(gray);
                    }
                }
                else if (g is TMP_SubMeshUI)
                {
                    var subMesh = (TMP_SubMeshUI)g;
                    if (subMesh.textComponent is LUITextMesh)
                    {
                        continue;
                    }
                    SetGray(g, gray);
                }
                else
                {
                    SetGray(g, gray);
                }

            }
        }
        else
        {
            var g = com.GetComponent<Graphic>();
            if (g is LUIText)
            {
                if (!ignoreText)
                {
                    ((LUIText)g).SetGray(gray);
                }
            }
            else if (g is LUITextMesh)
            {
                if (!ignoreText)
                {
                    ((LUITextMesh)g).SetGray(gray);
                }
            }
            else if (g is TMP_SubMeshUI)
            {
                var subMesh = (TMP_SubMeshUI)g;
                if (!(subMesh.textComponent is LUITextMesh))
                {
                    SetGray(g, gray);
                }
            }
            else
            {
                SetGray(g, gray);
            }
        }

    }
    public static void SetGray(Graphic graphic, bool gray)
    {
        if (graphic == null)
        {
            return;
        }
        if (gray)
        {
            //暂时不支持默认含有材质的UI控件的置灰，如有需要，可以在逻辑里面具体问题具体写
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
    public static void UIArrayCopy(UIArray array, int count, bool reset)
    {
        var coms = array.m_Items;
        coms.Clear();

        var template_index = array.m_Template.transform.GetSiblingIndex();
        var parent = array.m_Template.transform.parent;
        int hasCount = parent.childCount;
        if (reset)
        {
            for (int i = 0; i < hasCount; ++i)
            {
                if (i == template_index)
                {
                    continue;
                }
                var child = parent.GetChild(i);
                var go = child.gameObject;
                GameObject.Destroy(go);
            }
            hasCount = 0;
        }
        else
        {
            hasCount = parent.childCount - 1;
        }


        array.m_Template.gameObject.SetActive(false);

        template_index = array.m_Template.transform.GetSiblingIndex();

        //1 1 2 1
        for (int i = 0; i < hasCount + 1; ++i)
        {
            if (i == template_index)
            {
                continue;
            }
            if (i < count)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(true);
                coms.Add(child);
            }
            else
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);
            }
        }
        int cloneCount = count - hasCount;
        for (int i = 0; i < cloneCount; ++i)
        {
            var clone = UnityEngine.Object.Instantiate(array.m_Template, parent);
            clone.gameObject.SetActive(true);
            coms.Add(clone);
        }

    }



    public static void UIArrayCopy(UIArray array, int count)
    {
        var coms = array.m_Items;
        coms.Clear();
        var parent = array.m_Template.transform.parent;
        int hasCount = array.m_Template.transform.parent.childCount;

        for (int i = 0; i < hasCount; ++i)
        {
            if (i < count)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(true);
                coms.Add(child);
            }
            else
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);
            }
        }
        int cloneCount = count - hasCount;
        for (int i = 0; i < cloneCount; ++i)
        {
            var clone = UnityEngine.Object.Instantiate(array.m_Template, parent);
            coms.Add(clone);
        }
    }

    public static Component UIArrayGetOrAdd(UIArray array, int index)
    {
        if (array.m_Items.Count > index)
        {
            var com = array.m_Items[index];
            if (!com.gameObject.activeSelf)
            {
                com.gameObject.SetActive(true);
            }
            return com;
        }
        else
        {
            var parent = array.m_Template.transform.parent;
            var coms = array.m_Items;
            var clone = UnityEngine.Object.Instantiate(array.m_Template, parent);
            if (!clone.gameObject.activeSelf)
            {
                clone.gameObject.SetActive(true);
            }
            coms.Add(clone);
            return clone;
        }
    }

    #endregion

    private static string m_WorkSpaceName = null;
    public static string GetWorkSpaceName()
    {
        if (m_WorkSpaceName == null)
        {
#if UNITY_EDITOR
            m_WorkSpaceName = System.IO.Path.GetFullPath(Application.dataPath);
#else
            m_WorkSpaceName = "";
#endif
        }
        return m_WorkSpaceName;
    }
    public static string GetPersistentPath()
    {
        return MonoTool.GetPersistentPath();
    }
    #region UI 事件监听注册
    public static int m_GlobalSoundId_ButtonClick = 0;

    public static void ResetMenu(List<ActiveButtons> activeButtons, int defRow = 0, int defCol = 0,
        Button left = null, Button right = null, Button up = null, Button down = null,
        bool selectDefaultActiveButton = true)
    {
        // 切换菜单来源前先关闭旧窗口上的选中态，避免全局静态菜单切页后残留旧高亮。
        SetSelectionVisuals(false);
        m_CurActiveButtons.Clear();

        m_CurActiveLeftButton = left;
        m_CurActiveRightButton = right;
        m_CurActiveUpButton = up;
        m_CurActiveDownButton = down;

        if(activeButtons != null && activeButtons.Count > 0)
        {
            m_CurActiveButtons.AddRange(activeButtons);
        }
        m_CurrentRow = defRow;
        m_CurrentCol = defCol;
        m_HasCurrentSelection = false;
        if (!NormalizeCurrentSelection())
        {
            return;
        }

        m_HasCurrentSelection = selectDefaultActiveButton;
        SetSelectionVisuals(m_HasCurrentSelection);
    }

    public static void OnUpArrowPressed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        //CGameProcedure.Event.OnInputAction(context, InputType.Up);
        if (IsGridEmpty())
        {
            return;
        }
        if (TryActivateDefaultSelection())
        {
            return;
        }
        // 特殊处理：如果只有一行数据，上键等同于左键（向前切）
        if (m_CurActiveButtons.Count == 1)
        {
            MoveColumn(-1);
        }
        else // 正常多行逻辑
        {
            m_CurrentRow--;
            if (m_CurrentRow < 0) m_CurrentRow = m_CurActiveButtons.Count - 1;
            ClampColumnIndex();
        }
        UpdateSelectionVisuals();

        if (m_CurActiveUpButton != null && m_CurActiveUpButton.interactable)
        {
            m_CurActiveUpButton.onClick.Invoke();
        }
    }

    public static void OnDownArrowPressed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        //CGameProcedure.Event.OnInputAction(context, InputType.Down);
        if (IsGridEmpty())
        {
            return;
        }
        if (TryActivateDefaultSelection())
        {
            return;
        }
        // 特殊处理：如果只有一行数据，下键等同于右键（向后切）
        if (m_CurActiveButtons.Count == 1)
        {
            MoveColumn(1);
        }
        else // 正常多行逻辑
        {
            m_CurrentRow++;
            if (m_CurrentRow >= m_CurActiveButtons.Count)
            {
                m_CurrentRow = 0;
            }
            ClampColumnIndex();
        }
        UpdateSelectionVisuals();

        if(m_CurActiveDownButton != null && m_CurActiveDownButton.interactable)
        {
            m_CurActiveDownButton.onClick.Invoke();
        }
    }

    public static void OnLeftArrowPressed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        //CGameProcedure.Event.OnInputAction(context, InputType.Left);
        if (IsGridEmpty())
        {
            return;
        }
        if (TryActivateDefaultSelection())
        {
            return;
        }
        // 无论单行还是多行，左键都是列递减
        MoveColumn(-1);
        UpdateSelectionVisuals();

        if (m_CurActiveLeftButton != null && m_CurActiveLeftButton.interactable)
        {
            m_CurActiveLeftButton.onClick.Invoke();
        }
    }

    public static void OnRightArrowPressed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        //CGameProcedure.Event.OnInputAction(context, InputType.Right);
        if (IsGridEmpty())
        {
            return;
        }
        if (TryActivateDefaultSelection())
        {
            return;
        }
        // 无论单行还是多行，右键都是列递增
        MoveColumn(1);
        UpdateSelectionVisuals();

        if (m_CurActiveRightButton != null && m_CurActiveRightButton.interactable)
        {
            m_CurActiveRightButton.onClick.Invoke();
        }
    }

    public static void OnEnterAction(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        RenderEvent.Event.OnRenderPrepareConfirmRequest();
        //CGameProcedure.Event.OnInputAction(context, InputType.Enter);
        if (!TryGetCurrentButton(out var currentButton))
        {
            return;
        }
        if (currentButton != null && currentButton.interactable)
        {
            currentButton.onClick.Invoke();
        }
    }

    public static void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInputDuringLoading())
        {
            return;
        }

        RenderEvent.Event.OnRenderEscapePressedRequest(context);
    }

    private static bool ShouldIgnoreInputDuringLoading()
    {
        return RenderEvent.Event.OnRenderShouldIgnoreInputDuringLoading();
    }

    /// <summary>
    /// 抽取出来的公用列移动方法
    /// </summary>
    private static void MoveColumn(int amount)
    {
        if (!NormalizeCurrentSelection())
        {
            return;
        }

        List<Button> currentLine = m_CurActiveButtons[m_CurrentRow].buttons;
        if (currentLine == null || currentLine.Count == 0)
        {
            return;
        }
        m_CurrentCol += amount;

        // 边界循环处理
        if (m_CurrentCol < 0)
        {
            m_CurrentCol = currentLine.Count - 1;
        }
        else if (m_CurrentCol >= currentLine.Count)
        {
            m_CurrentCol = 0;
        }
    }

    private static void UpdateSelectionVisuals()
    {
        RenderEvent.Event.OnUpdateSelectionVisuals();
        if (!NormalizeCurrentSelection())
        {
            return;
        }
        SetSelectionVisuals(true);
    }

    // 未默认选中的窗口，第一次方向输入只建立首项选中，不同时执行一次菜单移动。
    private static bool TryActivateDefaultSelection()
    {
        if (m_HasCurrentSelection)
        {
            return false;
        }

        if (!NormalizeCurrentSelection())
        {
            return true;
        }

        m_HasCurrentSelection = true;
        UpdateSelectionVisuals();
        return true;
    }

    private static void SetSelectionVisuals(bool selected)
    {
        if (m_CurActiveButtons == null || m_CurActiveButtons.Count == 0)
        {
            return;
        }

        for (int r = 0; r < m_CurActiveButtons.Count; r++)
        {
            List<Button> rowButtons = m_CurActiveButtons[r].buttons;
            if (rowButtons == null)
            {
                continue;
            }
            for (int c = 0; c < rowButtons.Count; c++)
            {
                if (rowButtons[c] == null)
                {
                    continue;
                }
                var button = rowButtons[c] as LUIButton;
                if (button != null)
                {
                    button.SetAsChooseState(selected && r == m_CurrentRow && c == m_CurrentCol);
                }
                else if (selected)
                {
                    //AudioManager.GetInstance().Play2D(7);   //打开失败
                }
            }
        }
    }

    private static bool IsGridEmpty()
    {
        if(m_CurActiveButtons == null || m_CurActiveButtons.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < m_CurActiveButtons.Count; ++i)
        {
            if (HasValidRow(m_CurActiveButtons[i]))
            {
                return false;
            }
        }

        return true;
    }

    // 菜单数据来自不同窗口的预制体与缓存实例，行列索引必须在每次使用前重新钳制，
    // 否则窗口按钮数量变化后，静态索引会直接越界。
    private static bool NormalizeCurrentSelection()
    {
        if (!ClampRowIndex())
        {
            return false;
        }

        ClampColumnIndex();
        return true;
    }

    private static bool ClampRowIndex()
    {
        if (IsGridEmpty())
        {
            return false;
        }

        if (m_CurrentRow < 0 || m_CurrentRow >= m_CurActiveButtons.Count || !HasValidRow(m_CurActiveButtons[m_CurrentRow]))
        {
            for (int i = 0; i < m_CurActiveButtons.Count; ++i)
            {
                if (HasValidRow(m_CurActiveButtons[i]))
                {
                    m_CurrentRow = i;
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private static void ClampColumnIndex()
    {
        if (!ClampRowIndex())
        {
            m_CurrentCol = 0;
            return;
        }

        var rowButtons = m_CurActiveButtons[m_CurrentRow];
        if (rowButtons.buttons == null || rowButtons.buttons.Count == 0)
        {
            m_CurrentCol = 0;
            return;
        }

        int maxColInNewRow = rowButtons.buttons.Count - 1;
        if (m_CurrentCol < 0)
        {
            m_CurrentCol = 0;
            return;
        }

        if (m_CurrentCol > maxColInNewRow)
        {
            m_CurrentCol = maxColInNewRow;
        }
    }

    private static bool TryGetCurrentButton(out Button currentButton)
    {
        currentButton = null;
        if (!m_HasCurrentSelection || !NormalizeCurrentSelection())
        {
            return false;
        }

        var rowButtons = m_CurActiveButtons[m_CurrentRow].buttons;
        if (rowButtons == null || m_CurrentCol < 0 || m_CurrentCol >= rowButtons.Count)
        {
            return false;
        }

        currentButton = rowButtons[m_CurrentCol];
        return currentButton != null;
    }

    private static bool HasValidRow(ActiveButtons rowButtons)
    {
        return rowButtons != null && rowButtons.buttons != null && rowButtons.buttons.Count > 0;
    }

    public static void AddButtonClick(Button com, System.Action call, int soundId = int.MinValue)
    {
        if (IsNull(com))
        {
            return;
        }
        int _soundId = soundId;
        com.onClick.RemoveAllListeners();
        com.onClick.AddListener(() =>
        {
            OnClickButtonSoundCommon(_soundId);
            call();
        });
    }
    public static void AddButtonClickDown(LUIButton com, System.Action call, int soundId = int.MinValue)
    {
        if (IsNull(com))
        {
            return;
        }
        int _soundId = soundId;
        com.OnClickDownCall = () =>
        {
            OnClickButtonSoundCommon(_soundId);
            call();
        };
    }
    public static int m_GlobalSoundId_ToggleChanged = 0;
    public static void AddToggleChanged(Toggle com, System.Action<int, bool> call, int index, int soundId = int.MinValue)
    {
        if (IsNull(com))
        {
            return;
        }
        com.onValueChanged.RemoveAllListeners();
        int idx = index;
        int _soundId = soundId;
        com.onValueChanged.AddListener((value) =>
        {
            if (_soundId == int.MinValue)
            {
                _soundId = m_GlobalSoundId_ToggleChanged;
            }
            if (_soundId != 0)
            {
                AudioManager.GetInstance().Play2D(_soundId);
            }
            call(idx, value);
        });
    }
    public static void AddSliderChanged(Slider com, System.Action<float> call)
    {
        if (IsNull(com))
        {
            return;
        }
        com.onValueChanged.RemoveAllListeners();
        com.onValueChanged.AddListener((value) => { call(value); });
    }
    public static void AddScrollbarChanged(ScrollRect com, System.Action<float, float> call)
    {
        if (IsNull(com))
        {
            return;
        }
        com.onValueChanged.RemoveAllListeners();
        com.onValueChanged.AddListener((xy) =>
        {
            call(xy.x, xy.y);
        });
    }

    public static int m_GlobalSoundId_DrapDownChanged = 0;
    public static void AddDrapDownChanged(Dropdown com, System.Action<int> call, int soundId = int.MinValue)
    {
        if (IsNull(com))
        {
            return;
        }
        int _soundId = soundId;
        com.onValueChanged.RemoveAllListeners();
        com.onValueChanged.AddListener((index) =>
        {
            if (_soundId == int.MinValue)
            {
                _soundId = m_GlobalSoundId_DrapDownChanged;
            }
            if (_soundId != 0)
            {
                //AudioManager.GetInstance().Play2D(_soundId);
            }
            call(index);
        });
    }
    public static void AddInputFieldChanged(InputField com, System.Action<string> call)
    {
        if (IsNull(com))
        {
            return;
        }
        com.onValueChanged.RemoveAllListeners();
        com.onValueChanged.AddListener((str) =>
        {
            call(str);
        });
    }
    //输入结束或者确认输入
    public static void AddInputFieldSubmit(InputField com, System.Action<string> call)
    {
        if (IsNull(com))
        {
            return;
        }
        com.onEndEdit.RemoveAllListeners();
        com.onEndEdit.AddListener((str) =>
        {
            call(str);
        });
    }
    public static void AddDragEvent(UIDraggableLimit com, System.Action<float, float> beginCall, System.Action<float, float> draggingCall, System.Action<float, float> endCall)
    {
        if (IsNull(com))
        {
            return;
        }
        com.OnBeginDragCall = beginCall;
        com.OnDraggingCall = draggingCall;
        com.OnEndDragCall = endCall;

    }

    //public static void AddParticleImageFinish(ParticleImage com, Action call)
    //{
    //    if (IsNull(com))
    //    {
    //        return;
    //    }

    //    com.onParticleFinish.RemoveAllListeners();
    //    com.onParticleFinish.AddListener(() =>
    //    {
    //        if (call != null)
    //        {
    //            call();
    //        }
    //    });
    //}
    //public static void AddParticleImageStart(ParticleImage com, Action call)
    //{
    //    if (IsNull(com))
    //    {
    //        return;
    //    }

    //    com.onStart.RemoveAllListeners();
    //    com.onStart.AddListener(() =>
    //    {
    //        if (call != null)
    //        {
    //            call();
    //        }
    //    });
    //}
    //public static void AddParticleImageStop(ParticleImage com, Action call)
    //{
    //    if (IsNull(com))
    //    {
    //        return;
    //    }

    //    com.onStop.RemoveAllListeners();
    //    com.onStop.AddListener(() =>
    //    {
    //        if (call != null)
    //        {
    //            call();
    //        }
    //    });
    //}
    //public static void AddParticleImageLastParticleFinish(ParticleImage com, Action call)
    //{
    //    if (IsNull(com))
    //    {
    //        return;
    //    }

    //    com.onLastParticleFinish.RemoveAllListeners();
    //    com.onLastParticleFinish.AddListener(() =>
    //    {
    //        if (call != null)
    //        {
    //            call();
    //        }
    //    });
    //}
    //public static void AddParticleImageFirstParticleFinish(ParticleImage com, Action call)
    //{
    //    if (IsNull(com))
    //    {
    //        return;
    //    }

    //    com.onFirstParticleFinish.RemoveAllListeners();
    //    com.onFirstParticleFinish.AddListener(() =>
    //    {
    //        if (call != null)
    //        {
    //            call();
    //        }
    //    });
    //}
    #endregion

    #region UI常用的一些节省开销的函数
    //private static Camera m_UICamera;
    //public static void SetUICamera(Camera cam)
    //{
    //    m_UICamera = cam;
    //}
    //public static Camera GetUICamera()
    //{
    //    return m_UICamera;
    //}
    public static Image GetImage(Component com, string path)
    {
        Transform tr = null;
        if (string.IsNullOrEmpty(path))
        {
            tr = com.transform;
        }
        else
        {
            tr = GetTransform(com, path);
        }
        return GetImage(tr);
    }
    public static Image GetImage(Component com)
    {
        if (com == null)
        {
            return null;
        }
        else
        {
            return com.GetComponent<Image>();
        }
    }
    public static Text GetText(Component com, string path)
    {
        Transform tr = null;
        if (string.IsNullOrEmpty(path))
        {
            tr = com.transform;
        }
        else
        {
            tr = GetTransform(com, path);
        }
        return GetText(tr);
    }
    public static Text GetText(Component com)
    {
        if (com == null)
        {
            return null;
        }
        else
        {
            return com.GetComponent<Text>();
        }
    }
    public static Button GetButton(Component com, string path)
    {
        Transform tr = null;
        if (string.IsNullOrEmpty(path))
        {
            tr = com.transform;
        }
        else
        {
            tr = GetTransform(com, path);
        }
        return GetButton(tr);
    }
    public static Button GetButton(Component com)
    {
        if (com == null)
        {
            return null;
        }
        else
        {
            return com.GetComponent<Button>();
        }
    }
    public static LoopListView2 GetList(Component com, string path)
    {
        Transform tr = null;
        if (string.IsNullOrEmpty(path))
        {
            tr = com.transform;
        }
        else
        {
            tr = GetTransform(com, path);
        }
        return GetList(tr);
    }
    public static LoopListView2 GetList(Component com)
    {
        if (com == null)
        {
            return null;
        }
        else
        {
            return com.GetComponent<LoopListView2>();
        }
    }
    public static Transform GetTransform(Component com, string path, bool withChild = false)
    {
        if (com == null)
        {
            return null;
        }
        return GetTransform(com.gameObject, path, withChild);
    }
    public static Transform GetTransform(GameObject com, string path, bool withChild = false)
    {
        if (com == null)
        {
            return null;
        }
        if (string.IsNullOrEmpty(path))
        {
            return com.transform;
        }
        else
        {
            var transform = com.transform.Find(path);
            if (transform == null && withChild)
            {
                string targetName = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
                transform = FindChildRecursive(com.transform, targetName);
            }
            return transform;
        }
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            var found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public static object CallGameDllFunction(string func, params object[] data)
    {
        return Main.GetInstance().CallGameDllFunction(func, data);
    }
    public static object OpenWindow(string classEnum, object parentName, params object[] param)
    {
        return Main.GetInstance().OpenWindow(classEnum, parentName, param);
    }
    public static void CloseWindow(string classEnum)
    {
        Main.GetInstance().CloseWindow(classEnum);
    }
    public static void CloseWindow(object winClass)
    {
        Main.GetInstance().CloseWindow(winClass);
    }
    #endregion


    #region 常用功能封装
    public static long AddCounter(int intervalMMSec, int count, System.Action perCall)
    {
        return GameDll.CounterManager.GetInstance().AddCounter(intervalMMSec, count, perCall);
    }
    public static void RemoveCounter(long id)
    {
        GameDll.CounterManager.GetInstance().RemoveCounter(id);
    }
    public static int GetDataBeanIntId(object bean)
    {
        BeanBase bean_base = (BeanBase)bean;
        return bean_base.GetId_int();
    }
    public static long GetDataBeanLongId(object bean)
    {
        BeanBase bean_base = (BeanBase)bean;
        return bean_base.GetId_long();
    }
    public static string GetDataBeanStringId(object bean)
    {
        BeanBase bean_base = (BeanBase)bean;
        return bean_base.GetId_string();
    }

    public static Array GetEnumValues(Type typeof_enum)
    {
        return Enum.GetValues(typeof_enum);
    }
    public static string GetEnumString(Type typeof_enum, object value)
    {
        return Enum.GetName(typeof_enum, value);
    }

    public static string GetShortRandomGuid()
    {
        long i = 1;
        var bytes = Guid.NewGuid().ToByteArray();
        foreach (byte b in bytes)
        {
            i *= ((int)b + 1);
        }
        return string.Format("{0:x}", i - DateTime.Now.Ticks);
    }
    public static long GetRandomGuid64()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt64(buffer, 0);
    }
    public static string GetRandomNormalGuid()
    {
        return Guid.NewGuid().ToString();
    }
    public static void NextFrameCall(Action call)
    {
        Main.GetInstance().NextFrameCall(call);
    }

    #region List的Item为主工程类的时候调用的接口
    public static void ListItemSort(List<double> list, Func<double, double, int> compareFunc)
    {
        if (list == null) return;
        var l = list;
        int count = l.Count;
        if (count <= 1)
        {
            return;
        }
        OptimizedQuickSort(l, compareFunc);
    }
    public static void ListItemSort(List<float> list, Func<float, float, int> compareFunc)
    {
        if (list == null) return;
        var l = list;
        int count = l.Count;
        if (count <= 1)
        {
            return;
        }
        OptimizedQuickSort(l, compareFunc);
    }
    public static void ListItemSort(List<long> list, Func<long, long, int> compareFunc)
    {
        if (list == null) return;
        var l = list;
        int count = l.Count;
        if (count <= 1)
        {
            return;
        }
        OptimizedQuickSort(l, compareFunc);
    }
    public static void ListItemSort(List<int> list, Func<int, int, int> compareFunc)
    {
        if (list == null) return;
        var l = list;
        int count = l.Count;
        if (count <= 1)
        {
            return;
        }
        OptimizedQuickSort(l, compareFunc);
    }

    //默认是升序排列  
    public static void ListItemSort(object list, Func<object, object, int> compareFunc)
    {
        if (list == null) return;

        System.Collections.IList l = (System.Collections.IList)list;
        int count = l.Count;
        if (count <= 1)
        {
            return;
        }
        OptimizedQuickSort(l, compareFunc);
    }
    #region 快速排序优化版
    public static void OptimizedQuickSort(List<int> list, Func<int, int, int> compare)
    {
        const int INSERTION_THRESHOLD = 16;
        var stack = new Stack<(int, int)>();
        stack.Push((0, list.Count - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();

            // 处理小数组
            if (right - left <= INSERTION_THRESHOLD)
            {
                InsertionSort(list, left, right, compare);
                continue;
            }

            // 三数取中法优化
            int mid = left + (right - left) / 2;
            MedianOfThree(list, left, mid, right, compare);

            // 分区操作
            int pivotIndex = Partition(list, left, right, compare);

            // 优先处理较大分区
            if (pivotIndex - left > right - pivotIndex)
            {
                stack.Push((left, pivotIndex - 1));
                stack.Push((pivotIndex + 1, right));
            }
            else
            {
                stack.Push((pivotIndex + 1, right));
                stack.Push((left, pivotIndex - 1));
            }
        }
    }
    public static void OptimizedQuickSort(List<long> list, Func<long, long, int> compare)
    {
        const int INSERTION_THRESHOLD = 16;
        var stack = new Stack<(int, int)>();
        stack.Push((0, list.Count - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();

            // 处理小数组
            if (right - left <= INSERTION_THRESHOLD)
            {
                InsertionSort(list, left, right, compare);
                continue;
            }

            // 三数取中法优化
            int mid = left + (right - left) / 2;
            MedianOfThree(list, left, mid, right, compare);

            // 分区操作
            int pivotIndex = Partition(list, left, right, compare);

            // 优先处理较大分区
            if (pivotIndex - left > right - pivotIndex)
            {
                stack.Push((left, pivotIndex - 1));
                stack.Push((pivotIndex + 1, right));
            }
            else
            {
                stack.Push((pivotIndex + 1, right));
                stack.Push((left, pivotIndex - 1));
            }
        }
    }
    public static void OptimizedQuickSort(List<float> list, Func<float, float, int> compare)
    {
        const int INSERTION_THRESHOLD = 16;
        var stack = new Stack<(int, int)>();
        stack.Push((0, list.Count - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();

            // 处理小数组
            if (right - left <= INSERTION_THRESHOLD)
            {
                InsertionSort(list, left, right, compare);
                continue;
            }

            // 三数取中法优化
            int mid = left + (right - left) / 2;
            MedianOfThree(list, left, mid, right, compare);

            // 分区操作
            int pivotIndex = Partition(list, left, right, compare);

            // 优先处理较大分区
            if (pivotIndex - left > right - pivotIndex)
            {
                stack.Push((left, pivotIndex - 1));
                stack.Push((pivotIndex + 1, right));
            }
            else
            {
                stack.Push((pivotIndex + 1, right));
                stack.Push((left, pivotIndex - 1));
            }
        }
    }
    public static void OptimizedQuickSort(List<double> list, Func<double, double, int> compare)
    {
        const int INSERTION_THRESHOLD = 16;
        var stack = new Stack<(int, int)>();
        stack.Push((0, list.Count - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();

            // 处理小数组
            if (right - left <= INSERTION_THRESHOLD)
            {
                InsertionSort(list, left, right, compare);
                continue;
            }

            // 三数取中法优化
            int mid = left + (right - left) / 2;
            MedianOfThree(list, left, mid, right, compare);

            // 分区操作
            int pivotIndex = Partition(list, left, right, compare);

            // 优先处理较大分区
            if (pivotIndex - left > right - pivotIndex)
            {
                stack.Push((left, pivotIndex - 1));
                stack.Push((pivotIndex + 1, right));
            }
            else
            {
                stack.Push((pivotIndex + 1, right));
                stack.Push((left, pivotIndex - 1));
            }
        }
    }
    public static void OptimizedQuickSort(IList list, Func<object, object, int> compare)
    {
        const int INSERTION_THRESHOLD = 16;
        var stack = new Stack<(int, int)>();
        stack.Push((0, list.Count - 1));

        while (stack.Count > 0)
        {
            var (left, right) = stack.Pop();

            // 处理小数组
            if (right - left <= INSERTION_THRESHOLD)
            {
                InsertionSort(list, left, right, compare);
                continue;
            }

            // 三数取中法优化
            int mid = left + (right - left) / 2;
            MedianOfThree(list, left, mid, right, compare);

            // 分区操作
            int pivotIndex = Partition(list, left, right, compare);

            // 优先处理较大分区
            if (pivotIndex - left > right - pivotIndex)
            {
                stack.Push((left, pivotIndex - 1));
                stack.Push((pivotIndex + 1, right));
            }
            else
            {
                stack.Push((pivotIndex + 1, right));
                stack.Push((left, pivotIndex - 1));
            }
        }
    }

    // 三数取中法实现
    private static void MedianOfThree(IList list, int a, int b, int c, Func<object, object, int> compare)
    {
        if (compare(list[a], list[b]) > 0) Swap(list, a, b);
        if (compare(list[a], list[c]) > 0) Swap(list, a, c);
        if (compare(list[b], list[c]) > 0) Swap(list, b, c);
    }
    private static void MedianOfThree(List<int> list, int a, int b, int c, Func<int, int, int> compare)
    {
        if (compare(list[a], list[b]) > 0) Swap(list, a, b);
        if (compare(list[a], list[c]) > 0) Swap(list, a, c);
        if (compare(list[b], list[c]) > 0) Swap(list, b, c);
    }
    private static void MedianOfThree(List<long> list, int a, int b, int c, Func<long, long, int> compare)
    {
        if (compare(list[a], list[b]) > 0) Swap(list, a, b);
        if (compare(list[a], list[c]) > 0) Swap(list, a, c);
        if (compare(list[b], list[c]) > 0) Swap(list, b, c);
    }
    private static void MedianOfThree(List<float> list, int a, int b, int c, Func<float, float, int> compare)
    {
        if (compare(list[a], list[b]) > 0) Swap(list, a, b);
        if (compare(list[a], list[c]) > 0) Swap(list, a, c);
        if (compare(list[b], list[c]) > 0) Swap(list, b, c);
    }
    private static void MedianOfThree(List<double> list, int a, int b, int c, Func<double, double, int> compare)
    {
        if (compare(list[a], list[b]) > 0) Swap(list, a, b);
        if (compare(list[a], list[c]) > 0) Swap(list, a, c);
        if (compare(list[b], list[c]) > 0) Swap(list, b, c);
    }
    // 分区函数（含基准值最终位置调整）
    private static int Partition(IList list, int left, int right, Func<object, object, int> compare)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap(list, i + 1, right);
        return i + 1;
    }
    private static int Partition(List<int> list, int left, int right, Func<int, int, int> compare)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap(list, i + 1, right);
        return i + 1;
    }
    private static int Partition(List<long> list, int left, int right, Func<long, long, int> compare)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap(list, i + 1, right);
        return i + 1;
    }
    private static int Partition(List<float> list, int left, int right, Func<float, float, int> compare)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap(list, i + 1, right);
        return i + 1;
    }
    private static int Partition(List<double> list, int left, int right, Func<double, double, int> compare)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                Swap(list, i, j);
            }
        }

        Swap(list, i + 1, right);
        return i + 1;
    }
    // 插入排序实现
    private static void InsertionSort(IList list, int left, int right, Func<object, object, int> compare)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var key = list[i];
            int j = i - 1;

            while (j >= left && compare(list[j], key) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = key;
        }
    }
    private static void InsertionSort(List<int> list, int left, int right, Func<int, int, int> compare)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var key = list[i];
            int j = i - 1;

            while (j >= left && compare(list[j], key) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = key;
        }
    }
    private static void InsertionSort(List<long> list, int left, int right, Func<long, long, int> compare)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var key = list[i];
            int j = i - 1;

            while (j >= left && compare(list[j], key) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = key;
        }
    }
    private static void InsertionSort(List<float> list, int left, int right, Func<float, float, int> compare)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var key = list[i];
            int j = i - 1;

            while (j >= left && compare(list[j], key) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = key;
        }
    }
    private static void InsertionSort(List<double> list, int left, int right, Func<double, double, int> compare)
    {
        for (int i = left + 1; i <= right; i++)
        {
            var key = list[i];
            int j = i - 1;

            while (j >= left && compare(list[j], key) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = key;
        }
    }
    // 交换元素通用方法
    private static void Swap(IList list, int indexA, int indexB)
    {
        var temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }
    private static void Swap(List<int> list, int indexA, int indexB)
    {
        var temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }
    private static void Swap(List<long> list, int indexA, int indexB)
    {
        var temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }
    private static void Swap(List<float> list, int indexA, int indexB)
    {
        var temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }
    private static void Swap(List<double> list, int indexA, int indexB)
    {
        var temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }
    #endregion












    #endregion
    public static List<string> ReadTxtFile(string fileFullName)
    {
        System.IO.StreamReader reader = null;
        try
        {
            reader = System.IO.File.OpenText(fileFullName);
            List<string> lines = new List<string>();
            do
            {
                lines.Add(reader.ReadLine());
            } while (!reader.EndOfStream);
            return lines;
        }
        catch (Exception e)
        {
            UDebug.LogError(e.ToString());
        }
        finally
        {
            if (reader != null)
            {
                reader.Close();
            }

        }
        return null;
    }

    public static void LoadURP(string urpName, Action call)
    {
        LCL.UIRes.LoadPrefabAsync(typeof(UniversalRenderPipelineAsset), urpName, Tool.GetAssetName(urpName), (rd, ud) =>
        {
            UniversalRenderPipelineAsset p = (UniversalRenderPipelineAsset)GameObject.Instantiate(rd.m_Obj);
            if (p != null)
            {
                GraphicsSettings.renderPipelineAsset = p;
                GraphicsSettings.defaultRenderPipeline = p;
                UDebug.Log("设置URP Asset: " + urpName +
                    "  p.scriptableRenderer == null:" + (p.scriptableRenderer == null));
                if (UniversalRenderPipeline.asset == null)
                {
                    UDebug.LogError(">>>>>>>>>>>>  LoadURP   UniversalRenderPipeline.asset == null");
                }
                else
                {
                    UDebug.LogError(">>>>>>>>>>>>  LoadURP   UniversalRenderPipeline.asset != null");
                }
                if (UniversalRenderPipeline.asset == p)
                {
                    UDebug.LogError(">>>>>>>>>>>>  LoadURP   UniversalRenderPipeline.asset == p");
                }
                else
                {
                    UDebug.LogError(">>>>>>>>>>>>  LoadURP   UniversalRenderPipeline.asset != p");
                }
            }
            if (call != null)
            {
                call();
            }
        });
    }

    public static void SetCameraURPType(Camera cam, int type_0_base_1_overlay)
    {
        UDebug.Log("设置URP Camera:" + cam.name);
        var camera_data = cam.GetUniversalAdditionalCameraData();
        camera_data.renderType = (CameraRenderType)type_0_base_1_overlay;
    }

    public static void SetCameraSplit(Camera cam, bool use_global_render_scale = true)
    {
        //UDebug.Log("设置URP " + cam.name + " use_global_render_scale:" + use_global_render_scale);
        //var camera_data = cam.GetUniversalAdditionalCameraData();
        //camera_data.m_UseGlobalRenderScale = use_global_render_scale;
        //UniversalRenderer.sUISplitEnable = true;
    }
    public static void InitTimeAndPhysics()
    {
        Physics.autoSimulation = false;
        //因为autoSimulation关闭了，autoSyncTransforms不开启可能导致射线检测不准确
        Physics.autoSyncTransforms = true;
        //以下设置 物理更新由默认的17次变成了5次
        Time.maximumDeltaTime = 0.1f;
        Time.maximumParticleDeltaTime = 0.1f;
        Time.fixedDeltaTime = 0.02f;
    }
    public static void SetFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        //默认开启高帧率
        int fps = 55;
        var open_async = PlayerPrefs.GetInt("pic_async", 1) == 1;
        if (open_async)
        {
            fps = 144;
        }
        else
        {
            fps = 60;
        }
        Application.targetFrameRate = fps;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        UDebug.Log("当前游戏使用targetFrameRate=" + Application.targetFrameRate);
    }
    //设置renderScale有时间会导致UI看不到，貌似0.5有点问题 所以暂时使用0.55
    public static void SetResolution()
    {
        var wh = Screen.currentResolution;
        float urp_render_scale = 1.0f;
        //var qualityLevel = QualitySettings.GetQualityLevel();
        //if (qualityLevel >= (int)QualityLevel.Beautiful)
        //{
        //    //Screen.SetResolution(1920, (int)(1920f * wh.height / wh.width), true);
        //    urp_render_scale = 1.0f;
        //}
        //else if (qualityLevel == (int)QualityLevel.Good)
        //{
        //    urp_render_scale = 0.75f;
        //}
        //else if (qualityLevel == (int)QualityLevel.Simple)
        //{
        //    urp_render_scale = 0.55f;
        //}
        //else
        //{
        //    urp_render_scale = 0.35f;
        //}

        var urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = urp_render_scale;
            wh = Screen.currentResolution;
            UDebug.Log("当前分辨率： 宽：" + wh.width + " 高：" + wh.height + " 缩放：" + urp.renderScale);
        }



    }
    public static void SetScreenDirection(ScreenOrientation dir)
    {
        Screen.orientation = dir;
    }

    //public static void RegisterAutoAddURPCameraStack()
    //{
    //    AutoAddURPCameraStack.OnAutoAddURPCameraStack = OnAutoAddURPCameraStack;
    //}

    //public static void OnAutoAddURPCameraStack(Camera camera, int depth, CameraRenderType type)
    //{
    //    if(type == CameraRenderType.Base)
    //    {
    //        UDebug.LogError("需要叠加的相机是Base，是否添加错了呢？或者有其他特殊效果呢？目前暂时不做后视镜等完全覆盖默认Base相机的功能");
    //    }
    //    camera.depth = depth;
    //    SetCameraURPType(camera, (int)type);
    //    var base_camera = GetWorldCamera();
    //    AddCameraStack(base_camera, camera);
    //}
    //这个是手动移除，如果相机不在了，貌似URP会自动处理的
    public static void RemoveCameraStack(Camera baseCamera, Camera addCamera)
    {
        var camera_data = baseCamera.GetUniversalAdditionalCameraData();
        camera_data.cameraStack.Remove(addCamera);
    }
    //注意：多场景叠加那种不支持
    public static void AddCameraStack(Camera baseCamera, Camera addCamera)
    {
        var camera_data = baseCamera.GetUniversalAdditionalCameraData();
        camera_data.SetRenderer(0);
        var stack = camera_data.cameraStack;
        foreach (var c in stack)
        {
            if (c == null || c.Equals(null))
            {
                UDebug.LogError("添加相机的时候发现堆栈中有空相机");
            }
            if (c == addCamera)
            {
                return;
            }
        }
        stack.Add(addCamera);

        stack.Sort((cam_a, cam_b) =>
        {
            if (cam_a.depth != cam_b.depth)
            {
                return cam_a.depth.CompareTo(cam_b.depth);
            }
            else
            {
                return cam_a.GetHashCode().CompareTo(cam_b.GetHashCode());
            }

        });
    }

    public static void EnableKeyword(Renderer r, string key)
    {
        if (r == null || r.material == null ||
           r.materials == null ||
           r.materials.Length == 0)
        {
            return;
        }

        var mats = r.materials;
        foreach (var m in mats)
        {
            if (m == null)
            {
                continue;
            }

            m.EnableKeyword(key);
        }
    }

    public static void DisableKeyword(Renderer r, string key)
    {
        if (r == null || r.material == null ||
           r.materials == null ||
           r.materials.Length == 0)
        {
            return;
        }

        var mats = r.materials;
        foreach (var m in mats)
        {
            if (m == null)
            {
                continue;
            }

            m.DisableKeyword(key);
            UDebug.Log("DisableKeyword 主工程 key:" + key + " mesh:" + r.name);

            //var shader = m.shader;
            //var keywordSpace = shader.keywordSpace;
            //foreach (var localKeyword in keywordSpace.keywords)
            //{
            //    // If the local keyword is overridable (i.e., it was declared with a global scope),
            //    // and a global keyword with the same name exists and is enabled,
            //    // then Unity uses the global keyword state
            //    if (localKeyword.isOverridable && Shader.IsKeywordEnabled(localKeyword.name))
            //    {
            //        UDebug.Log("Local keyword with name of " + localKeyword.name + " is overridden by a global keyword, and is enabled");
            //    }
            //    // Otherwise, Unity uses the local keyword state
            //    else
            //    {
            //        var state = m.IsKeywordEnabled(localKeyword) ? "enabled" : "disabled";
            //        UDebug.Log("Local keyword with name of " + localKeyword.name + " is " + state);
            //    }
            //}
        }
    }

    public static bool IsSQLCharError(string str)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        str = str.ToLower().Trim();

        if (str.Contains("'"))
        {
            return true;
        }
        else if (str.Contains(";"))
        {
            return true;
        }
        else if (str.Contains(","))
        {
            return true;
        }
        else if (str.Contains("?"))
        {
            return true;
        }
        else if (str.Contains("<"))
        {
            return true;
        }
        else if (str.Contains(">"))
        {
            return true;
        }
        else if (str.Contains("("))
        {
            return true;
        }
        else if (str.Contains(")"))
        {
            return true;
        }
        else if (str.Contains("@"))
        {
            return true;
        }
        else if (str.Contains("="))
        {
            return true;
        }
        else if (str.Contains("+"))
        {
            return true;
        }
        else if (str.Contains("*"))
        {
            return true;
        }
        else if (str.Contains("&"))
        {
            return true;
        }
        else if (str.Contains("#"))
        {
            return true;
        }
        else if (str.Contains("%"))
        {
            return true;
        }
        else if (str.Contains("$"))
        {
            return true;
        }

        //删除与数据库相关的词
        if (str.Contains("select")) { return true; }
        else if (str.Contains("insert")) { return true; }
        else if (str.Contains("delete from")) { return true; }
        else if (str.Contains("count")) { return true; }
        else if (str.Contains("drop table")) { return true; }
        else if (str.Contains("truncate")) { return true; }
        else if (str.Contains("asc")) { return true; }
        else if (str.Contains("mid")) { return true; }
        else if (str.Contains("char")) { return true; }
        else if (str.Contains("xp_cmdshell")) { return true; }
        else if (str.Contains("exec master")) { return true; }
        else if (str.Contains("net localgroup administrators")) { return true; }
        else if (str.Contains("and")) { return true; }
        else if (str.Contains("net user")) { return true; }
        else if (str.Contains("or")) { return true; }
        else if (str.Contains("net")) { return true; }
        else if (str.Contains("-")) { return true; }
        else if (str.Contains("delete")) { return true; }
        else if (str.Contains("drop")) { return true; }
        else if (str.Contains("script")) { return true; }
        else if (str.Contains("update")) { return true; }
        else if (str.Contains("and")) { return true; }
        else if (str.Contains("chr")) { return true; }
        else if (str.Contains("master")) { return true; }
        else if (str.Contains("truncate")) { return true; }
        else if (str.Contains("declare")) { return true; }
        else if (str.Contains("mid")) { return true; }

        return false;
    }

    public static bool m_IsFuncAllOpen = false;
    public static bool m_JumpAllGuide = false;
    public static bool IsJumpAllGuide()
    {
        return m_JumpAllGuide;
    }
    public static bool IsFuncAllOpen()
    {
        return m_IsFuncAllOpen;
    }
    #endregion

    private static System.Diagnostics.Stopwatch m_Stopwatch = new System.Diagnostics.Stopwatch();
    public static void StartStopwatch()
    {
        m_Stopwatch.Reset();
        m_Stopwatch.Start();
    }
    public static long StopStopwatch()
    {
        m_Stopwatch.Stop();
        return m_Stopwatch.ElapsedMilliseconds;
    }
    public static byte[] GetMD5HashFromFile(string fileName)
    {
        using (FileStream file = new FileStream(fileName, System.IO.FileMode.Open))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            return retVal;
        }

    }
    public static bool IsMD5Right(string full_path, string cmp_md5)
    {
        bool isOpenCheckMD5 = true;
        if (isOpenCheckMD5 == false)
        {
            return true;
        }
        if (cmp_md5 == null || cmp_md5.Length != 32)
        {
            return true;
        }
        //string full_path = Path.Combine(LCL.MonoTool.GetPersistentPath(), fileInfo.m_FileName);
        if (File.Exists(full_path))
        {
            var hashBytes = RenderAPI.GetMD5HashFromFile(full_path);
            Span<byte> cmpBytes = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
            {
                cmpBytes[i] = Convert.ToByte(cmp_md5.Substring(i * 2, 2), 16);
            }
            if (!hashBytes.AsSpan().SequenceEqual(cmpBytes))
            {
                Debug.LogError("新下载的文件md5不正确, 配置：" + cmp_md5 + " 下载：" + hashBytes);
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            Debug.LogError("需要校验md5的文件本地没有找到，文件是：" + full_path);
            return false;
        }
    }
    public static bool IsMD5Right(byte[] full_path, string cmp_md5)
    {
        if (full_path == null)
        {
            return false;
        }
        if (cmp_md5 == null || cmp_md5.Length != 32)
        {
            return true;
        }
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(full_path);
        Span<byte> cmpBytes = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            cmpBytes[i] = Convert.ToByte(cmp_md5.Substring(i * 2, 2), 16);
        }
        if (!hashBytes.AsSpan().SequenceEqual(cmpBytes))
        {
            Debug.LogError("新下载的文件md5不正确, 配置：" + cmp_md5 + " 下载：" + hashBytes);
            return false;
        }
        else
        {
            return true;
        }
    }
    private static CoroutineCom m_CoroutineCom;
    public static Coroutine StartCoroutine(IEnumerator func)
    {
        if (m_CoroutineCom == null || m_CoroutineCom.Equals(null))
        {
            var go = new GameObject("CoroutineCom");
            GameObject.DontDestroyOnLoad(go);
            m_CoroutineCom = go.AddComponent<CoroutineCom>();
        }
        return m_CoroutineCom.StartCoroutine(func);
    }
    public static void StopCoroutine(Coroutine func)
    {
        if (m_CoroutineCom == null || m_CoroutineCom.Equals(null))
        {
            return;
        }
        if (func == null)
        {
            return;
        }
        m_CoroutineCom.StopCoroutine(func);
    }
    public static void StopAllCoroutines()
    {
        if (m_CoroutineCom == null || m_CoroutineCom.Equals(null))
        {
            return;
        }
        m_CoroutineCom.StopAllCoroutines();
    }
    public static void StartOrAddTweenCanvasAlpha(LoopListViewItem2 item, float time = 0.5f, float from = 0, float to = 1.0f)
    {
        if (item == null || item.Equals(null))
        {
            return;
        }
        if (item.ParentListView == null)
        {
            return;
        }
        if (item.ParentListView.m_EnableAlphaEffect)
        {
            CanvasGroup cg = item.gameObject.GetComponent<CanvasGroup>();
            //var tween = item.GetComponent<TweenCanvasAlpha>();
            //if (tween != null)
            //{
            //    tween.ResetToBeginning();
            //    tween.PlayForward();
            //}
            //else
            //{
            //    cg = item.gameObject.AddComponent<CanvasGroup>();
            //    tween = item.gameObject.AddComponent<TweenCanvasAlpha>();
            //    tween.m_Duration = time;
            //    tween.from = from;
            //    tween.to = to;
            //    tween.PlayForward();
            //}
            if (!cg.interactable)
            {
                cg.interactable = true;
            }
            if (!cg.blocksRaycasts)
            {
                cg.blocksRaycasts = true;
            }
        }
        else
        {
            var cg = item.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1;
                if (!cg.interactable)
                {
                    cg.interactable = true;
                }
                if (!cg.blocksRaycasts)
                {
                    cg.blocksRaycasts = true;
                }
            }
            //var tween = item.GetComponent<TweenCanvasAlpha>();
            //if (tween != null)
            //{
            //    tween.enabled = false;
            //}
        }

    }

    public static long GetUncompressedSizeFromBytes(byte[] zipData, string password)
    {
        long totalSize = 0;
        using (var memStream = new MemoryStream(zipData))
        using (var zipStream = new ZipInputStream(memStream))
        {
            zipStream.Password = password;
            ZipEntry entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                if (!entry.IsDirectory)
                {
                    totalSize += entry.Size; // 累加每个文件的原始大小
                }
            }
        }
        return totalSize;
    }

    private static Camera m_WorldCamera;
    public static Camera GetWorldCamera()
    {
        return m_WorldCamera;
    }
    public static void SetWorldCamera(Camera cam)
    {
        m_WorldCamera = cam;
    }
}
