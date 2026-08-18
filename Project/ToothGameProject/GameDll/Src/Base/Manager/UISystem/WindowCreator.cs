using System;
using System.Collections.Generic;
using System.Text;
using GameDll;

namespace GameHot
{
    public  class WindowCreator
    {

        public static WindowBase GetWindowInstance(string windowfile, params object[] data)
        {
            WindowBase ui = null;
            Type t = Type.GetType("GameDll."+windowfile);
            if (t == null)
            {
                UDebug.LogError("GameDll 实例化失败，" + windowfile);
                return null;
            }
            ui = (WindowBase)Activator.CreateInstance(t);
            return ui;
        }
    }
}
