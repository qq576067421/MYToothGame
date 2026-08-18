using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCL
{
    public interface ICoroutineHandler
    {
        void Coroutine_StopAllCoroutines();
        int Coroutine_GetInstanceID();
    }
}