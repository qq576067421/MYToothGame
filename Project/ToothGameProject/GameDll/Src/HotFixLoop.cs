using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameHot
{
    public class HotFixLoop : IGameHotFixInterface
    {
        private static HotFixLoop m_Instance;



        public override void Start()
        {
   
            m_Instance = this;
            UDebug.Log("GameDll Start Ok");

            CGameProcedure.InitStaticMemeber();
        }

        public override void Update()
        {
            CGameProcedure.Update();
        }



        public static HotFixLoop GetInstance()
        {
            return m_Instance;
        }

        public override void OnDestroy()
        {
            CGameProcedure.ReleaseStaticMember();
        }
        public override void OnApplicationQuit()
        {
        
        }
        public override object OnMono2GameDll(string func, params object[] datas)
        {
            return Mono2GameDll.Call(func, datas);
        }
    }
}
