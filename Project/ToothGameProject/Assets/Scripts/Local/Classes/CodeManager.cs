using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class CodeManager
{
    public static bool EnableDevelopment
    {
        get
        {
#if CSHotFix
            return false;
#else
            return true;
#endif
        }
    }
}

