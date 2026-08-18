using System;
using System.Collections.Generic;

namespace GameHot
{

    public abstract class SystemBaseManager
    {
        public abstract void Init();
        public abstract void UnInit();
        public abstract void OnReceivedMainStartMessage(object msg);
        public abstract void OnReceivedSystemStartMessage(object msg);
    }
}
