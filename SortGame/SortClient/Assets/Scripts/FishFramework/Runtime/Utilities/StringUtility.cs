using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public static class StringUtility
{
    private static StringBuilder _builder = new StringBuilder(512);

    public static StringBuilder SingleBuilder()
    {
        _builder.Length = 0;
        return _builder;
    }

    public static StringBuilder PopBuilder()
    {
#if !UNITY_2017 && !UNITY_5
        lock (temp)
#endif
        {
            if (temp.Count > 0)
            {
                var builder = temp.Pop();
                builder.Length = 0;
                return builder;
            }

            return new StringBuilder(64);
        }
    }

    public static void PushBuilder(StringBuilder builder)
    {
        if (null == builder) return;
#if !UNITY_2017 && !UNITY_5
        lock (temp)
#endif
        {
            temp.Push(builder);
        }
    }

    private static Stack<StringBuilder> temp = new Stack<StringBuilder>();

    public static string FormatEx(string format, params object[] args)
    {
        if (string.IsNullOrEmpty(format))
            return string.Empty;

        StringBuilder tempbuilder = PopBuilder();
        try
        {
            return tempbuilder.AppendFormat(format, args).ToString();
        }
        catch (Exception)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogError(format);
#endif
            return string.Empty;
        }
        finally
        {
            PushBuilder(tempbuilder);
        }
    }

    public static string FormatEx(IFormatProvider provider, string format, params object[] args)
    {
        if (null == provider)
            return string.Empty;
        if (string.IsNullOrEmpty(format))
            return string.Empty;

        StringBuilder tempbuilder = PopBuilder();
        try
        {
            return tempbuilder.AppendFormat(provider, format, args).ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
        finally
        {
            PushBuilder(tempbuilder);
        }
    }

    /// <summary>
    /// StringUtility.Join("_", "a", "b") => "a_b"
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static string Join(String separator, params object[] args)
    {
        StringBuilder tempbuilder = PopBuilder();
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                tempbuilder.Append(args[i]);
                if (i < args.Length - 1)
                    tempbuilder.Append(separator);
            }

            return tempbuilder.ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
        finally
        {
            PushBuilder(tempbuilder);
        }
    }

    /// <summary>
    ///  StringUtility.Combine("a", "b", "c")  => "abc"
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static string Concat(params object[] args)
    {
        StringBuilder tempbuilder = PopBuilder();
        try
        {
            for (int i = 0; i < args.Length; i++)
                tempbuilder.Append(args[i]);
            return tempbuilder.ToString();
        }
        catch (Exception)
        {
            return string.Empty;
        }
        finally
        {
            PushBuilder(tempbuilder);
        }
    }

    public static string[] Split(string value, char separator)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return value.Split(separator);
    }

    public static int AsciiLength(this string value)
    {
        int length = 0;
        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c > 127)
                length += 2;
            else
                length++;
        }

        return length;
    }

    /// <summary>
    /// Table used to convert character to lowercase.
    /// </summary>
    const string k_LookupStringL =
        "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

    /// <summary>
    /// Table used to convert character to uppercase.
    /// </summary>
    const string k_LookupStringU =
        "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";


    /// <summary>
    /// Get lowercase version of this ASCII character.
    /// </summary>
    public static char ToLowerFast(char c)
    {
        if (c > k_LookupStringL.Length - 1)
            return c;

        return k_LookupStringL[c];
    }

    /// <summary>
    /// Get uppercase version of this ASCII character.
    /// </summary>
    public static char ToUpperFast(char c)
    {
        if (c > k_LookupStringU.Length - 1)
            return c;

        return k_LookupStringU[c];
    }

    /// <summary>
    /// Get uppercase version of this ASCII character.
    /// </summary>
    public static uint ToUpperASCIIFast(uint c)
    {
        if (c > k_LookupStringU.Length - 1)
            return c;

        return k_LookupStringU[(int)c];
    }

    /// <summary>
    /// Get lowercase version of this ASCII character.
    /// </summary>
    public static uint ToLowerASCIIFast(uint c)
    {
        if (c > k_LookupStringL.Length - 1)
            return c;

        return k_LookupStringL[(int)c];
    }

    public static char[] EmojiCode = {
        '\ue400', '\ue401','\ue402','\ue403','\ue404','\ue405','\ue406','\ue407','\ue408','\ue409',
        '\ue410', '\ue411','\ue412','\ue413','\ue414','\ue415','\ue416','\ue417','\ue418','\ue419',
        '\ue420', '\ue421','\ue422','\ue423','\ue424','\ue425','\ue426','\ue427','\ue428','\ue429',
        '\ue430', '\ue431','\ue432','\ue433','\ue434','\ue435','\ue436','\ue437','\ue438','\ue439',
        '\ue440', '\ue441','\ue442','\ue443','\ue444','\ue445','\ue446','\ue447','\ue448','\ue449',
        '\ue450', '\ue451','\ue452','\ue453','\ue454','\ue455','\ue456','\ue457','\ue458','\ue459',
        '\ue460', '\ue461','\ue462','\ue463','\ue464','\ue465','\ue466','\ue467','\ue468','\ue469',
        '\ue470', '\ue471','\ue472','\ue473','\ue474','\ue475','\ue476','\ue477','\ue478','\ue479',
        '\ue480', '\ue481','\ue482','\ue483','\ue484','\ue485','\ue486','\ue487','\ue488','\ue489',
        '\ue490', '\ue491','\ue492','\ue493','\ue494','\ue495','\ue496','\ue497','\ue498','\ue499',
    };

    public static string EmojiChar(int i)
    {
        return new string(EmojiCode[i], 1);
    }

    public static string GetPYString(string str)
    {
        string tempStr = "";
        foreach (char c in str)
        {
            if ((int)c >= 33 && (int)c <= 126)
            {//字母和符号原样保留 
                tempStr += c.ToString();
            }
            else
            {//累加拼音声母 
                tempStr += GetPYChar(c.ToString());
            }
        }

        // UnityEngine.Debug.Log( "str="+str+"=tempStr="+tempStr );

        return tempStr;
    }

    /// 取单个字符的拼音声母 
     /// 要转换的单个汉字 
     /// 拼音声母 
    public static string GetPYChar(string c)
    {
        string result = string.Empty;
   
        //byte[] array = new byte[2];
        //array = System.Text.Encoding.GetEncoding("GB2312").GetBytes(c);

        byte[] array = System.Text.Encoding.GetEncoding("GB2312").GetBytes(c);
        if (array.Length != 2)
        {
            return result;
        }


        int i = (short)(array[0] - '\0') * 256 + ((short)(array[1] - '\0'));

        //if ( i < 0xB0A1) return "";

        if ((i >= 0xB0A1) && (i <= 0xB0C4))
        {
            result = "a";
        }
        else if ((i >= 0xB0C5) && (i <= 0xB2C0))
        {
            result = "b";
        }
        else if ((i >= 0xB2C1) && (i <= 0xB4ED))
        {
            result = "c";
        }
        else if ((i >= 0xB4EE) && (i <= 0xB6E9))
        {
            result = "d";
        }
        else if ((i >= 0xB6EA) && (i <= 0xB7A1))
        {
            result = "e";
        }
        else if ((i >= 0xB7A2) && (i <= 0xB8C0))
        {
            result = "f";
        }
        else if ((i >= 0xB8C1) && (i <= 0xB9FD))
        {
            result = "g";
        }
        else if ((i >= 0xB9FE) && (i <= 0xBBF6))
        {
            result = "h";
        }
        else if ((i >= 0xBBF7) && (i <= 0xBFA5))
        {
            result = "j";
        }
        else if ((i >= 0xBFA6) && (i <= 0xC0AB))
        {
            result = "k";
        }
        else if ((i >= 0xC0AC) && (i <= 0xC2E7))
        {
            result = "l";
        }
        else if ((i >= 0xC2E8) && (i <= 0xC4C2))
        {
            result = "m";
        }
        else if ((i >= 0xC4C3) && (i <= 0xC5B5))
        {
            result = "n";
        }
        else if ((i >= 0xC5B6) && (i <= 0xC5BD))
        {
            result = "o";
        }
        else if ((i >= 0xC5BE) && (i <= 0xC6D9))
        {
            result = "p";
        }
        else if ((i >= 0xC6DA) && (i <= 0xC8BA))
        {
            result = "q";
        }
        else if ((i >= 0xC8BB) && (i <= 0xC8F5))
        {
            result = "r";
        }
        else if ((i >= 0xC8F6) && (i <= 0xCBF9))
        {
            result = "s";
        }
        else if ((i >= 0xCBFA) && (i <= 0xCDD9))
        {
            result = "t";
        }
        else if ((i >= 0xCDDA) && (i <= 0xCEF3))
        {
            result = "w";
        }
        else if ((i >= 0xCEF4) && (i <= 0xD1B8))
        {
            result = "x";
        }
        else if ((i >= 0xD1B9) && (i <= 0xD4D0))
        {
            result = "y";
        }
        else if ((i >= 0xD4D1) && (i <= 0xD7F9))
        {
            result = "z";
        }
        else
        {
            result = string.Empty;
        }


        bool Upper = false;

        if (Upper)
        {
            result = result.ToUpper();
        }
        return result;
    }









    /// <summary>
    /// 汉字转拼音类
    /// </summary>

    private static int[] pyValue = new int[]
    {
        -20319,-20317,-20304,-20295,-20292,-20283,-20265,-20257,-20242,-20230,-20051,-20036,
        -20032,-20026,-20002,-19990,-19986,-19982,-19976,-19805,-19784,-19775,-19774,-19763,
        -19756,-19751,-19746,-19741,-19739,-19728,-19725,-19715,-19540,-19531,-19525,-19515,
        -19500,-19484,-19479,-19467,-19289,-19288,-19281,-19275,-19270,-19263,-19261,-19249,
        -19243,-19242,-19238,-19235,-19227,-19224,-19218,-19212,-19038,-19023,-19018,-19006,
        -19003,-18996,-18977,-18961,-18952,-18783,-18774,-18773,-18763,-18756,-18741,-18735,
        -18731,-18722,-18710,-18697,-18696,-18526,-18518,-18501,-18490,-18478,-18463,-18448,
        -18447,-18446,-18239,-18237,-18231,-18220,-18211,-18201,-18184,-18183, -18181,-18012,
        -17997,-17988,-17970,-17964,-17961,-17950,-17947,-17931,-17928,-17922,-17759,-17752,
        -17733,-17730,-17721,-17703,-17701,-17697,-17692,-17683,-17676,-17496,-17487,-17482,
        -17468,-17454,-17433,-17427,-17417,-17202,-17185,-16983,-16970,-16942,-16915,-16733,
        -16708,-16706,-16689,-16664,-16657,-16647,-16474,-16470,-16465,-16459,-16452,-16448,
        -16433,-16429,-16427,-16423,-16419,-16412,-16407,-16403,-16401,-16393,-16220,-16216,
        -16212,-16205,-16202,-16187,-16180,-16171,-16169,-16158,-16155,-15959,-15958,-15944,
        -15933,-15920,-15915,-15903,-15889,-15878,-15707,-15701,-15681,-15667,-15661,-15659,
        -15652,-15640,-15631,-15625,-15454,-15448,-15436,-15435,-15419,-15416,-15408,-15394,
        -15385,-15377,-15375,-15369,-15363,-15362,-15183,-15180,-15165,-15158,-15153,-15150,
        -15149,-15144,-15143,-15141,-15140,-15139,-15128,-15121,-15119,-15117,-15110,-15109,
        -14941,-14937,-14933,-14930,-14929,-14928,-14926,-14922,-14921,-14914,-14908,-14902,
        -14894,-14889,-14882,-14873,-14871,-14857,-14678,-14674,-14670,-14668,-14663,-14654,
        -14645,-14630,-14594,-14429,-14407,-14399,-14384,-14379,-14368,-14355,-14353,-14345,
        -14170,-14159,-14151,-14149,-14145,-14140,-14137,-14135,-14125,-14123,-14122,-14112,
        -14109,-14099,-14097,-14094,-14092,-14090,-14087,-14083,-13917,-13914,-13910,-13907,
        -13906,-13905,-13896,-13894,-13878,-13870,-13859,-13847,-13831,-13658,-13611,-13601,
        -13406,-13404,-13400,-13398,-13395,-13391,-13387,-13383,-13367,-13359,-13356,-13343,
        -13340,-13329,-13326,-13318,-13147,-13138,-13120,-13107,-13096,-13095,-13091,-13076,
        -13068,-13063,-13060,-12888,-12875,-12871,-12860,-12858,-12852,-12849,-12838,-12831,
        -12829,-12812,-12802,-12607,-12597,-12594,-12585,-12556,-12359,-12346,-12320,-12300,
        -12120,-12099,-12089,-12074,-12067,-12058,-12039,-11867,-11861,-11847,-11831,-11798,
        -11781,-11604,-11589,-11536,-11358,-11340,-11339,-11324,-11303,-11097,-11077,-11067,
        -11055,-11052,-11045,-11041,-11038,-11024,-11020,-11019,-11018,-11014,-10838,-10832,
        -10815,-10800,-10790,-10780,-10764,-10587,-10544,-10533,-10519,-10331,-10329,-10328,
        -10322,-10315,-10309,-10307,-10296,-10281,-10274,-10270,-10262,-10260,-10256,-10254
     };
    private static string[] pyName = new string[]
    {
        "A","Ai","An","Ang","Ao","Ba","Bai","Ban","Bang","Bao","Bei","Ben",
        "Beng","Bi","Bian","Biao","Bie","Bin","Bing","Bo","Bu","Ba","Cai","Can",
        "Cang","Cao","Ce","Ceng","Cha","Chai","Chan","Chang","Chao","Che","Chen","Cheng",
        "Chi","Chong","Chou","Chu","Chuai","Chuan","Chuang","Chui","Chun","Chuo","Ci","Cong",
        "Cou","Cu","Cuan","Cui","Cun","Cuo","Da","Dai","Dan","Dang","Dao","De",
        "Deng","Di","Dian","Diao","Die","Ding","Diu","Dong","Dou","Du","Duan","Dui",
        "Dun","Duo","E","En","Er","Fa","Fan","Fang","Fei","Fen","Feng","Fo",
        "Fou","Fu","Ga","Gai","Gan","Gang","Gao","Ge","Gei","Gen","Geng","Gong",
        "Gou","Gu","Gua","Guai","Guan","Guang","Gui","Gun","Guo","Ha","Hai","Han",
        "Hang","Hao","He","Hei","Hen","Heng","Hong","Hou","Hu","Hua","Huai","Huan",
        "Huang","Hui","Hun","Huo","Ji","Jia","Jian","Jiang","Jiao","Jie","Jin","Jing",
        "Jiong","Jiu","Ju","Juan","Jue","Jun","Ka","Kai","Kan","Kang","Kao","Ke",
        "Ken","Keng","Kong","Kou","Ku","Kua","Kuai","Kuan","Kuang","Kui","Kun","Kuo",
        "La","Lai","Lan","Lang","Lao","Le","Lei","Leng","Li","Lia","Lian","Liang",
        "Liao","Lie","Lin","Ling","Liu","Long","Lou","Lu","Lv","Luan","Lue","Lun",
        "Luo","Ma","Mai","Man","Mang","Mao","Me","Mei","Men","Meng","Mi","Mian",
        "Miao","Mie","Min","Ming","Miu","Mo","Mou","Mu","Na","Nai","Nan","Nang",
        "Nao","Ne","Nei","Nen","Neng","Ni","Nian","Niang","Niao","Nie","Nin","Ning",
        "Niu","Nong","Nu","Nv","Nuan","Nue","Nuo","O","Ou","Pa","Pai","Pan",
        "Pang","Pao","Pei","Pen","Peng","Pi","Pian","Piao","Pie","Pin","Ping","Po",
        "Pu","Qi","Qia","Qian","Qiang","Qiao","Qie","Qin","Qing","Qiong","Qiu","Qu",
        "Quan","Que","Qun","Ran","Rang","Rao","Re","Ren","Reng","Ri","Rong","Rou",
        "Ru","Ruan","Rui","Run","Ruo","Sa","Sai","San","Sang","Sao","Se","Sen",
        "Seng","Sha","Shai","Shan","Shang","Shao","She","Shen","Sheng","Shi","Shou","Shu",
        "Shua","Shuai","Shuan","Shuang","Shui","Shun","Shuo","Si","Song","Sou","Su","Suan",
        "Sui","Sun","Suo","Ta","Tai","Tan","Tang","Tao","Te","Teng","Ti","Tian",
        "Tiao","Tie","Ting","Tong","Tou","Tu","Tuan","Tui","Tun","Tuo","Wa","Wai",
        "Wan","Wang","Wei","Wen","Weng","Wo","Wu","Xi","Xia","Xian","Xiang","Xiao",
        "Xie","Xin","Xing","Xiong","Xiu","Xu","Xuan","Xue","Xun","Ya","Yan","Yang",
        "Yao","Ye","Yi","Yin","Ying","Yo","Yong","You","Yu","Yuan","Yue","Yun",
        "Za", "Zai","Zan","Zang","Zao","Ze","Zei","Zen","Zeng","Zha","Zhai","Zhan",
        "Zhang","Zhao","Zhe","Zhen","Zheng","Zhi","Zhong","Zhou","Zhu","Zhua","Zhuai","Zhuan",
        "Zhuang","Zhui","Zhun","Zhuo","Zi","Zong","Zou","Zu","Zuan","Zui","Zun","Zuo"
    };

    /// <summary>
    /// 把汉字转换成拼音(全拼)
    /// </summary>
    /// <param name="hzString">汉字字符串</param>
    /// <returns>转换后的拼音(全拼)字符串</returns>
    public static string GetQPPyConvert(string hzString)
    {
        // 匹配中文字符
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("^[\u4e00-\u9fa5]$");
        byte[] array = new byte[2];
        string pyString = "";
        int chrAsc = 0;
        int i1 = 0;
        int i2 = 0;
        char[] noWChar = hzString.ToCharArray();

        for (int j = 0; j < noWChar.Length; j++)
        {
            // 中文字符
            if (regex.IsMatch(noWChar[j].ToString()))
            {
                array = System.Text.Encoding.GetEncoding("GB2312").GetBytes(noWChar[j].ToString());
                i1 = (short)(array[0]);
                i2 = (short)(array[1]);
                chrAsc = i1 * 256 + i2 - 65536;
                if (chrAsc > 0 && chrAsc < 160)
                {
                    pyString += noWChar[j];
                }
                else
                {
                    // 修正部分文字
                    if (chrAsc == -9254)  // 修正“圳”字
                        pyString += "Zhen";
                    else if (chrAsc == -8527)// 修正“薇”字
                        pyString += "Wei";
                    else
                    {
                        for (int i = (pyValue.Length - 1); i >= 0; i--)
                        {
                            if (pyValue[i] <= chrAsc)
                            {
                                pyString += pyName[i];
                                break;
                            }
                        }
                    }
                }
            }
            // 非中文字符
            else
            {
                pyString += noWChar[j].ToString();
            }
        }
        return pyString.ToLower();
    }
}
