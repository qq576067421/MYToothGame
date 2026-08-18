using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public enum GameLayer
    {
        Default = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        Water = 4,
        UI = 5,
        Floor = 9,
        Building = 10,
        ClickAble = 11,
        UI3D = 12,
        Tower = 13,
        Char = 14,
        TowerBase = 15,
        Hidden = 31
    }
}
