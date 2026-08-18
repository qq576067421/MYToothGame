using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCL
{
    public static class NumberExtension
    {
        #region 转换大数值；
        private static string[] toLargeNumSign = new string[]
        {"K","M","B","T","AA","AB","AC","AD","AE","AF","AG","AH","AI","AJ","AK","AL","AM","AN","AO","AP","AQ","AR","AS","AT","AU","AV","AW","AX","AY","AZ","BA","BB","BC","BD","BE","BF","BG","BH","BI","BJ","BK","BL","BM","BN","BO","BP","BQ","BR","BS","BT","BU","BV","BW","BX","BY","BZ","CA","CB","CC","CD","CE","CF","CG","CH","CI","CJ","CK","CL","CM","CN","CO","CP","CQ","CR","CS","CT","CU","CV","CW","CX","CY","CZ","DA","DB","DC","DD","DE","DF","DG","DH","DI","DJ","DK","DL","DM","DN","DO","DP","DQ","DR","DS","DT","DU","DV","DW","DX","DY","DZ","EA","EB","EC","ED","EE","EF","EG","EH","EI","EJ","EK","EL","EM","EN","EO","EP","EQ","ER","ES","ET","EU","EV","EW","EX","EY","EZ","FA","FB","FC","FD","FE","FF","FG","FH","FI","FJ","FK","FL","FM","FN","FO","FP","FQ","FR","FS","FT","FU","FV","FW","FX","FY","FZ","GA","GB","GC","GD","GE","GF","GG","GH","GI","GJ","GK","GL","GM","GN","GO","GP","GQ","GR","GS","GT","GU","GV","GW","GX","GY","GZ","HA","HB","HC","HD","HE","HF","HG","HH","HI","HJ","HK","HL","HM","HN","HO","HP","HQ","HR","HS","HT","HU","HV","HW","HX","HY","HZ","IA","IB","IC","ID","IE","IF","IG","IH","II","IJ","IK","IL","IM","IN","IO","IP","IQ","IR","IS","IT","IU","IV","IW","IX","IY","IZ","JA","JB","JC","JD","JE","JF","JG","JH","JI","JJ","JK","JL","JM","JN","JO","JP","JQ","JR","JS","JT","JU","JV","JW","JX","JY","JZ","KA","KB","KC","KD","KE","KF","KG","KH","KI","KJ","KK","KL","KM","KN","KO","KP","KQ","KR","KS","KT","KU","KV","KW","KX","KY","KZ","LA","LB","LC","LD","LE","LF","LG","LH","LI","LJ","LK","LL","LM","LN","LO","LP","LQ","LR","LS","LT","LU","LV","LW","LX","LY","LZ","MA","MB","MC","MD","ME","MF","MG","MH","MI","MJ","MK","ML","MM","MN","MO","MP","MQ","MR","MS","MT","MU","MV","MW","MX","MY","MZ","NA","NB","NC","ND","NE","NF","NG","NH","NI","NJ","NK","NL","NM","NN","NO","NP","NQ","NR","NS","NT","NU","NV","NW","NX","NY","NZ","OA","OB","OC","OD","OE","OF","OG","OH","OI","OJ","OK","OL","OM","ON","OO","OP","OQ","OR","OS","OT","OU","OV","OW","OX","OY","OZ","PA","PB","PC","PD","PE","PF","PG","PH","PI","PJ","PK","PL","PM","PN","PO","PP","PQ","PR","PS","PT","PU","PV","PW","PX","PY","PZ","QA","QB","QC","QD","QE","QF","QG","QH","QI","QJ","QK","QL","QM","QN","QO","QP","QQ","QR","QS","QT","QU","QV","QW","QX","QY","QZ","RA","RB","RC","RD","RE","RF","RG","RH","RI","RJ","RK","RL","RM","RN","RO","RP","RQ","RR","RS","RT","RU","RV","RW","RX","RY","RZ","SA","SB","SC","SD","SE","SF","SG","SH","SI","SJ","SK","SL","SM","SN","SO","SP","SQ","SR","SS","ST","SU","SV","SW","SX","SY","SZ","TA","TB","TC","TD","TE","TF","TG","TH","TI","TJ","TK","TL","TM","TN","TO","TP","TQ","TR","TS","TT","TU","TV","TW","TX","TY","TZ","UA","UB","UC","UD","UE","UF","UG","UH","UI","UJ","UK","UL","UM","UN","UO","UP","UQ","UR","US","UT","UU","UV","UW","UX","UY","UZ","VA","VB","VC","VD","VE","VF","VG","VH","VI","VJ","VK","VL","VM","VN","VO","VP","VQ","VR","VS","VT","VU","VV","VW","VX","VY","VZ","WA","WB","WC","WD","WE","WF","WG","WH","WI","WJ","WK","WL","WM","WN","WO","WP","WQ","WR","WS","WT","WU","WV","WW","WX","WY","WZ","XA","XB","XC","XD","XE","XF","XG","XH","XI","XJ","XK","XL","XM","XN","XO","XP","XQ","XR","XS","XT","XU","XV","XW","XX","XY","XZ","YA","YB","YC","YD","YE","YF","YG","YH","YI","YJ","YK","YL","YM","YN","YO","YP","YQ","YR","YS","YT","YU","YV","YW","YX","YY","YZ","ZA","ZB","ZC","ZD","ZE","ZF","ZG","ZH","ZI","ZJ","ZK","ZL","ZM","ZN","ZO","ZP","ZQ","ZR","ZS","ZT","ZU","ZV","ZW","ZX","ZY","ZZ"};
        private static string[] toLargeNumSignCn = new string[]
        { "万", "亿", "兆", "京","垓","秭","穰","㘬","涧","正","载","极","恒河沙","阿僧祗","那由他","不可思议","无量","大数","古戈尔"};

        private static int SplitNum = 10000;
        private static int SplitCn = 4;
        private static bool More10 = true;
        public static void SetMore10(bool more10)
        {
            More10 = more10;
        }
        public static void SetNumberChina(bool isChina)
        {
            isChina = false;
            if (isChina)
            {
                SplitNum = 10000;
                SplitCn = 4;
            }
            else
            {
                SplitNum = 1000;
                SplitCn = 3;
            }
        }
        public static string ToLargeNum(this decimal value, int dotRightCount = 1)
        {
            int ten10 = More10 ? 10 : 1;
            if (value < SplitNum * ten10 && value > -SplitNum * ten10)
                return value.ToString();
            string str = value.ToString("#");
            return GetToLargeNum(str, dotRightCount);
        }
        public static string ToLargeNum(this int value, int dotRightCount = 1)
        {
            int ten10 = More10 ? 10 : 1;
            if (value < SplitNum * ten10 && value > -SplitNum * ten10)
                return value.ToString();
            string str = value.ToString("#");
            return GetToLargeNum(str, dotRightCount);
        }
        public static string ToLargeNum(this long value, int dotRightCount = 1)
        {
            int ten10 = More10 ? 10 : 1;
            if (value < SplitNum * ten10 && value > -SplitNum * ten10)
                return value.ToString();
            string str = value.ToString("#");
            return GetToLargeNum(str, dotRightCount);
        }
        public static string ToLargeNum(this float value, int dotRightCount = 1)
        {
            int ten10 = More10 ? 10 : 1;
            if (value < SplitNum * ten10 && value > -SplitNum * ten10)
                return value.ToString("f" + dotRightCount);
            string str = value.ToString("#");
            return GetToLargeNum(str, dotRightCount);
        }
        public static string ToLargeNum(this double value, int dotRightCount = 1)
        {
            int ten10 = More10 ? 10 : 1;
            if (value < SplitNum * ten10 && value > -SplitNum * ten10)
                return value.ToString("f" + dotRightCount);
            string str = value.ToString("#");
            return GetToLargeNum(str, dotRightCount);
        }
        private static string GetToLargeNum(string str, int dotRightCount)
        {
            bool isNegative = str[0] == '-';
            if (isNegative)
                str = str.Remove(0, 1);
            int count = str.Length - (SplitCn + 1);
            int sign_num = Mathf.FloorToInt(count / SplitCn);
            int mod_num = count % SplitCn;
            bool isLower = false;
            if (mod_num == 0 && More10)
            {
                //刚好这一位
                if (sign_num > 0)
                {
                    sign_num -= 1;
                    isLower = true;
                }
                else
                {
                    return str;
                }
            }
            string sign = "";
            if (SplitCn == 4)
            {
                if (sign_num < toLargeNumSignCn.Length)
                {
                    sign = toLargeNumSignCn[sign_num];
                }
                else
                {
                    sign = toLargeNumSignCn[toLargeNumSignCn.Length - 1];
                }
            }
            else
            {
                if (sign_num < toLargeNumSign.Length)
                {
                    sign = toLargeNumSign[sign_num];
                }
                else
                {
                    sign = toLargeNumSign[toLargeNumSign.Length - 1];
                }
            }
            int dotLeftCount = count % SplitCn + 1;
            if (isLower)
            {
                dotLeftCount += SplitCn;
            }

            StringBuilder valueStr = new StringBuilder();
            if (dotLeftCount == 1)
            {
                valueStr.Append(str[0]);
                if (dotRightCount > 0)
                {
                    valueStr.Append('.');
                }
                if (dotRightCount >= 1)
                {
                    valueStr.Append(str[1]);
                }
                if (dotRightCount >= 2)
                {
                    valueStr.Append(str[2]);
                }
            }
            else if (dotLeftCount == 2)
            {
                valueStr.Append(str[0]);
                valueStr.Append(str[1]);
                if (dotRightCount > 0)
                {
                    valueStr.Append('.');
                }
                if (dotRightCount >= 1)
                {
                    valueStr.Append(str[2]);
                }
                if (dotRightCount >= 2)
                {
                    valueStr.Append(str[3]);
                }
            }
            else if (dotLeftCount == 3)
            {
                valueStr.Append(str[0]);
                valueStr.Append(str[1]);
                valueStr.Append(str[2]);
                if (dotRightCount > 0)
                {
                    valueStr.Append('.');
                }
                if (dotRightCount >= 1)
                {
                    valueStr.Append(str[3]);
                }
                if (dotRightCount >= 2)
                {
                    valueStr.Append(str[4]);
                }
            }
            else if (dotLeftCount == 4)
            {
                valueStr.Append(str[0]);
                valueStr.Append(str[1]);
                valueStr.Append(str[2]);
                valueStr.Append(str[3]);
                if (dotRightCount > 0)
                {
                    valueStr.Append('.');
                }
                if (dotRightCount >= 1)
                {
                    valueStr.Append(str[4]);
                }
                if (dotRightCount >= 2)
                {
                    valueStr.Append(str[5]);
                }
            }
			else if (dotLeftCount == 5)
            {
                valueStr.Append(str[0]);
                valueStr.Append(str[1]);
                valueStr.Append(str[2]);
                valueStr.Append(str[3]);
                valueStr.Append(str[4]);
                if (dotRightCount > 0)
                {
                    valueStr.Append('.');
                }
                if (dotRightCount >= 1)
                {
                    valueStr.Append(str[5]);
                }
                if (dotRightCount >= 2)
                {
                    valueStr.Append(str[6]);
                }
            }
            valueStr.Append(sign);
            if (isNegative)
                return '-' + valueStr.ToString();
            else
                return valueStr.ToString();
        }

        public static string ToLargeNumSimple(long num)
        {
            return num.ToLargeNum();
        }
        #endregion

        public static string FormatHurtNumber(float value)
        {
            if (value < 0)
            {
                return "-" + FormatHurtNumber(-value);
            }
            if (value <= 9999)
            {
                if (value == Mathf.FloorToInt(value))
                {
                    return Mathf.FloorToInt(value).ToString();
                }
                else
                {
                    return Mathf.CeilToInt(value * 10).ToString("F1").TrimEnd('0').TrimEnd('.');
                }
            }
            return ToLargeNum((decimal)value, 1);
        }
    }
}
