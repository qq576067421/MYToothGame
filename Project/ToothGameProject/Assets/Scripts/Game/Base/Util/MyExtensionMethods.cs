using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class MyExtensionMethods
{
    public static Vector3 vec3 = Vector3.zero;

    public static void Clear(this StringBuilder sb)
    {
        sb.Length = 0;
    }
    public static void AppendLineEx(this StringBuilder sb, string str = "")
    {
        sb.Append(str + "\r\n");
    }
    public static Vector2 GetValue(this Vector2 v2)
    {
        Vector2 hr = Vector2.zero;
        hr.x = v2.x;
        hr.y = v2.y;
        return hr;
    }
    //写该扩展方法的原因是js里面的Vector2和3是类，没有结构体的概念，我们实际上只是需要一个值而已
    public static Vector3 GetValue(this Vector3 v3)
    {
        Vector3 hr = Vector3.zero;
        hr.x = v3.x;
        hr.y = v3.y;
        hr.z = v3.z;
        return hr;
    }
    public static BigInteger ToBigInteger(this string value)
    {
        BigInteger hr = BigInteger.Zero;
        bool hasLetter = false;
        int char_count = value.Length;
        if (char_count > 0)
        {
            if (char.IsLetter(value[char_count - 1]))
            {
                hasLetter = true;
            }
            else
            {
                hasLetter = false;
            }
        }
        if (hasLetter)
        {
            return BigIntegerParser.ParseWithSuffix(value);
        }
        else
        {
            BigInteger big = BigInteger.Zero;
            BigInteger.TryParse(value, out big);
            return big * 1000;
        }
    }
    public static BigInteger ToBigInteger(this int value)
    {
        long lvalue = value;
        BigInteger hr = lvalue.ToBigInteger();
        return hr;
    }
    public static BigInteger ToBigInteger(this long value)
    {
        BigInteger hr = new BigInteger(1000 * value);
        return hr;
    }
    public static BigInteger ToBigInteger(this float value)
    {
        //先转double再乘1000，避免float精度不足导致截断误差（如5.099f*1000→5098）
        BigInteger hr = new BigInteger(Math.Round(1000.0 * (double)value));
        return hr;
    }
    public static BigInteger ToBigInteger(this double value)
    {
        BigInteger hr = new BigInteger(Math.Round(1000.0 * value));
        return hr;
    }
    //包括小数点在内的6个字符（单位是 k m b t zz到zz  每级1000）
    public static string ToFormatString(this BigInteger value, int maxLength = 6)
    {
        return BigIntegerParser.ParseToString(value, maxLength);
    }
    public static string ToFormatString(this int value, int maxLength = 6)
    {
        var big = value.ToBigInteger();
        return big.ToFormatString(maxLength);
    }
    public static string ToFormatString(this long value, int maxLength = 6)
    {
        var big = value.ToBigInteger();
        return big.ToFormatString(maxLength);
    }
    public static string ToFormatString(this float value, int maxLength = 6)
    {
        var big = value.ToBigInteger();
        return big.ToFormatString(maxLength);
    }
    public static float ToFloat(this BigInteger value)
    {
        if (value >= int.MaxValue)
        {
            //是一个超大数，没有必要转float
            return int.MaxValue;
        }
        else if (value <= int.MinValue)
        {
            return int.MinValue;
        }
        else
        {
            int value_int = (int)value;
            return value_int / 1000.0f;
        }

    }
}



public static class BigIntegerParser
{
    private static readonly Dictionary<string, int> suffixMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> orderedSuffixes = new List<string>();

    public static BigInteger Div(BigInteger a, BigInteger b)
    {
        return a * 1000 / b;
    }
    public static BigInteger Mul(BigInteger a, BigInteger b)
    {
        return a * b / 1000;
    }
    public static BigInteger Div(BigInteger a, int b)
    {
        return a * 1000 / b.ToBigInteger();
    }
    public static BigInteger Mul(BigInteger a, int b)
    {
        return a * b.ToBigInteger() / 1000;
    }
    public static BigInteger Div(BigInteger a, long b)
    {
        return a * 1000 / b.ToBigInteger();
    }
    public static BigInteger Mul(BigInteger a, long b)
    {
        return a * b.ToBigInteger() / 1000;
    }
    public static BigInteger Mul(BigInteger a, float b)
    {
        return a * b.ToBigInteger() / 1000;
    }

    public static BigInteger Div(int a, BigInteger b)
    {
        return a.ToBigInteger() * 1000 / b;
    }
    public static BigInteger Mul(int a, BigInteger b)
    {
        return a.ToBigInteger() * b / 1000;
    }
    public static BigInteger Div(long a, BigInteger b)
    {
        return a.ToBigInteger() * 1000 / b;
    }
    public static BigInteger Mul(long a, BigInteger b)
    {
        return a.ToBigInteger() * b / 1000;
    }


    static BigIntegerParser()
    {
        // 基础后缀映射
        suffixMap["k"] = 1;
        suffixMap["m"] = 2;
        suffixMap["b"] = 3;
        suffixMap["t"] = 4;

        // 生成aa到zz的后缀映射
        for (char firstChar = 'a'; firstChar <= 'z'; firstChar++)
        {
            for (char secondChar = 'a'; secondChar <= 'z'; secondChar++)
            {
                string suffix = firstChar.ToString() + secondChar;
                int level = 4 + (firstChar - 'a') * 26 + (secondChar - 'a') + 1;
                suffixMap[suffix] = level;
            }
        }

        // 构建有序后缀列表用于格式化
        orderedSuffixes.Add(""); // 无后缀
        orderedSuffixes.Add("k");
        orderedSuffixes.Add("m");
        orderedSuffixes.Add("b");
        orderedSuffixes.Add("t");

        for (char firstChar = 'a'; firstChar <= 'z'; firstChar++)
        {
            for (char secondChar = 'a'; secondChar <= 'z'; secondChar++)
            {
                string suffix = firstChar.ToString() + secondChar;
                orderedSuffixes.Add(suffix);
            }
        }
    }
    //负号不计算在内，需要做缩小处理1000或者一个单位级别，例如数值是1.2k 转出字符串为"1.2"
    public static string ParseToString(BigInteger value, int maxLength = 6)
    {
        if (value == 0)
            return "0";

        bool isNegative = value < 0;
        if (isNegative)
            value = -value;
        string valueStr = "";
        //内部存储放大了1000倍，小于1000的实际数值不应进入带后缀分支，否则会被当成k级别并丢失小数精度
        if (value < 1000000)
        {
            BigInteger integerPart = value / 1000;
            BigInteger remainder = value % 1000;


            if (remainder == 0)
            {
                valueStr = integerPart.ToString();
            }
            else
            {
                string decimalPart = ((int)remainder).ToString("D3").TrimEnd('0');
                valueStr = decimalPart.Length > 0 ? integerPart + "." + decimalPart : integerPart.ToString();
            }

            valueStr = TruncateDecimal(valueStr);
            return (isNegative ? "-" : "") + valueStr;
        }

        // 直接计算后缀级别
        int suffixLevel = (int)BigInteger.Log(value, 1000);
        suffixLevel = Math.Min(suffixLevel, orderedSuffixes.Count - 1);

        string suffix = "";
        //需要将单位缩小一个级别
        if (suffixLevel >= 1)
        {
            //suffixLevel -= 1;
            // 缩小数值
            var pow = BigInteger.Pow(1000, suffixLevel);
            var pow2 = BigInteger.Pow(1000, suffixLevel - 1);

            int scaledValue = (int)(value / pow);
            int mod = (int)((value % pow) / pow2);
            if (mod > 0)
            {
                string modStr = mod.ToString("D3");
                valueStr = scaledValue + "." + modStr;
            }
            else
            {
                valueStr = scaledValue.ToString();
            }
            suffix = orderedSuffixes[suffixLevel - 1];
        }
        else
        {
            //是一个小于1000的数
            float scaledValue = (float)value / 1000.0f;
            valueStr = scaledValue.ToString();
        }


        // 处理最大长度限制
        valueStr = TruncateDecimal(valueStr);
        return (isNegative ? "-" : "") + valueStr + suffix;
    }
    //输入的字符串提前处理了负数问题
    public static string TruncateDecimal(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 检查是否为负数
        bool isNegative = input.StartsWith("-");
        if (isNegative)
        {
            input = input.Substring(1);
            maxLength--; // 负号占用一个字符
        }

        // 分割整数部分和小数部分
        string[] parts = input.Split('.');
        if (parts.Length == 1)
            return isNegative ? "-" + parts[0] : parts[0]; // 没有小数部分

        string integerPart = parts[0];
        string decimalPart = parts[1];

        // 计算剩余可用的字符数
        int remainingLength = maxLength - integerPart.Length - 1; // 1 是小数点
        if (remainingLength <= 0)
            return isNegative ? "-" + integerPart : integerPart; // 无法保留小数部分

        // 截取小数部分
        if (decimalPart.Length > remainingLength)
            decimalPart = decimalPart.Substring(0, remainingLength);

        // 去除小数部分末尾多余的0
        decimalPart = decimalPart.TrimEnd('0');

        // 如果小数部分全部是0，则直接返回整数部分
        if (string.IsNullOrEmpty(decimalPart))
            return isNegative ? "-" + integerPart : integerPart;

        // 拼接结果
        return isNegative ? "-" + integerPart + "." + decimalPart : integerPart + "." + decimalPart;
    }

    //小数点前最多3位数，小数点后最多1位数；
    //当小数点前有3位数时，小数点后的位数全部省略；
    //小数点前有2位数时，小数点后显示1位数字；
    //小数点前有一位数时，小数点后显示2位小数
    //开头和末尾的0均省略显示
    //显示为向下取整
    public static string TruncateDecimal(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 检查是否为负数
        bool isNegative = input.StartsWith("-");
        if (isNegative)
        {
            input = input.Substring(1);
        }

        // 分割整数部分和小数部分
        string[] parts = input.Split('.');
        if (parts.Length == 1)
            return isNegative ? "-" + parts[0] : parts[0]; // 没有小数部分

        string integerPart = parts[0];
        string decimalPart = parts[1];

        // 处理整数部分前导0
        integerPart = integerPart.TrimStart('0');
        if (string.IsNullOrEmpty(integerPart))
        {
            integerPart = "0"; // 如果整数部分全为0，保留一个0
        }

        // 根据整数部分的有效位数决定小数部分处理方式
        int integerDigits = integerPart == "0" ? 0 : integerPart.Length;
        string result;

        if (integerDigits >= 3)
        {
            // 3位或以上整数，不显示小数
            result = integerPart;
        }
        else if (integerDigits == 2)
        {
            // 2位整数，显示1位小数（向下取整）
            if (decimalPart.Length > 0)
            {
                decimalPart = decimalPart.Length >= 1 ? decimalPart.Substring(0, 1) : "0";
                decimalPart = decimalPart.TrimEnd('0');
                result = string.IsNullOrEmpty(decimalPart) ? integerPart : $"{integerPart}.{decimalPart}";
            }
            else
            {
                result = integerPart;
            }
        }
        else // 0或1位整数
        {
            // 0位整数（如0.123）：显示3位小数
            if (integerDigits == 0)
            {
                if (decimalPart.Length > 0)
                {
                    decimalPart = decimalPart.Length >= 3 ? decimalPart.Substring(0, 3) : decimalPart.PadRight(3, '0').Substring(0, 3);
                    decimalPart = decimalPart.TrimEnd('0');
                    result = string.IsNullOrEmpty(decimalPart) ? integerPart : $"{integerPart}.{decimalPart}";
                }
                else
                {
                    result = integerPart;
                }
            }
            // 1位整数（如1.234）：显示2位小数
            else
            {
                if (decimalPart.Length > 0)
                {
                    decimalPart = decimalPart.Length >= 2 ? decimalPart.Substring(0, 2) : decimalPart.PadRight(2, '0').Substring(0, 2);
                    decimalPart = decimalPart.TrimEnd('0');
                    result = string.IsNullOrEmpty(decimalPart) ? integerPart : $"{integerPart}.{decimalPart}";
                }
                else
                {
                    result = integerPart;
                }
            }
        }

        // 处理负数
        return isNegative ? "-" + result : result;
    }


    /// <summary>
    /// 解析带有千进制后缀的字符串为BigInteger，转化为放大1000倍的，例如"1.2aa"  转化后是1.2ab， 同理"1.21" 转化后是1210
    /// </summary>
    /// <returns>对应的BigInteger值</returns>
    public static BigInteger ParseWithSuffix(string valueStr)
    {
        if (string.IsNullOrWhiteSpace(valueStr))
            return 0;

        bool isNegative = valueStr.StartsWith("-");
        if (isNegative)
            valueStr = valueStr.Substring(1);
        string number_str = "";
        string suffix = "";
        SplitNumberAndLetters(valueStr, out number_str, out suffix);
        var fnumber = float.Parse(number_str);
        int number = Mathf.RoundToInt(fnumber * 1000);

        int level = suffixMap.TryGetValue(suffix, out int l) ? l : 0;
        BigInteger multiplier = BigInteger.Pow(1000, level);
        BigInteger result = new BigInteger(number) * multiplier;
        return isNegative ? -result : result;
    }
    public static void SplitNumberAndLetters(string input, out string numberPart, out string letterPart)
    {
        numberPart = input;
        letterPart = "";

        if (string.IsNullOrEmpty(input))
            return;

        // 检查是否为纯数字（无字母后缀）
        bool isPureNumber = true;
        int char_count = input.Length;
        if (char_count > 0)
        {
            if (char.IsLetter(input[char_count - 1]))
            {
                isPureNumber = false;
            }
        }
        if (isPureNumber)
        {
            // 尝试解析为 BigInteger
            if (BigInteger.TryParse(input, out BigInteger value))
            {
                // 仅当数值 >= 1000 时才转换为带后缀的形式
                if (value >= 1000)
                {
                    string formatted = BigIntegerParser.ParseToString(value);
                    SplitNumberAndLetters(formatted, out numberPart, out letterPart);
                    return;
                }
                else
                {
                    // 数值小于 1000，直接返回原始字符串
                    numberPart = input;
                    letterPart = "";
                    return;
                }
            }
            else
            {
                // 解析失败，直接返回原始字符串
                numberPart = input;
                letterPart = "";
                return;
            }
        }

        // 原始逻辑：提取数字部分和字母后缀
        int letterCount = 0;
        for (int i = input.Length - 1; i >= 0 && letterCount < 2; i--)
        {
            if (char.IsLetter(input[i]))
            {
                letterCount++;
            }
            else
            {
                break;
            }
        }

        if (letterCount > 0)
        {
            numberPart = input.Substring(0, input.Length - letterCount);
            letterPart = input.Substring(input.Length - letterCount);
        }
    }
}
