/*
* LCL support c# hotfix here.
*Copyright(C) LCL.All rights reserved.
* URL:https://github.com/qq576067421/cshotfix 
*QQ:576067421 
* QQ Group: 673735733 
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at 
*  
* Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[InitializeOnLoad]
public class CodeManagerEditor
{
    static CodeManagerEditor()
    {

    }


    public static List<string> GetDefineSymbols()
    {
#if UNITY_IPHONE
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
#elif UNITY_ANDROID
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
#else
        string symbolsDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
        return symbolsDefines.Split(';').ToList();
    }

    public static void SetChannelVersion(string channel)
    {
        var definesList = GetDefineSymbols();
        definesList.RemoveAll((str) =>
        {
            return str.Contains("Channel"); 
        });
        definesList.Add(channel);

        ChangeDefineSymbol(definesList);
    }
    public static void SetChinaVersion(bool is_china)
    {
        var definesList = GetDefineSymbols();
        if (is_china)
        {
            if (!definesList.Contains("LCL_china"))
            {
                definesList.Add("LCL_china");
            }
        }
        else
        {
            if (definesList.Contains("LCL_china"))
            {
                definesList.Remove("LCL_china");
            }
        }

        ChangeDefineSymbol(definesList);
    }

    private static void ChangeDefineSymbol(List<string> definesList)
    {
        string defineSymbols = string.Join(";", definesList.ToArray());
#if UNITY_IPHONE
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defineSymbols);
#elif UNITY_ANDROID
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defineSymbols);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
#endif
    }



}