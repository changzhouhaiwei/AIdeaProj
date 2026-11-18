using UnityEngine;
using System;
using TMPro;

public class CheckNameLengthScript : MonoBehaviour
{
    public TMP_InputField input;
    public int CHARACTER_LIMIT = 10;
    public SplitType m_SplitType = SplitType.ASCII;

    public enum SplitType
    {
        ASCII = 1,
        GB = 2,
        Unicode = 3,
        UTF8 = 4,
    }

    private void Start()
    {
        input.onValueChanged.AddListener(delegate { Check(); });
    }

    public void Check()
    {
        input.text = GetSplitName();
    }

    public string GetSplitName()
    {
        string temp = input.text.Substring(0, (input.text.Length < CHARACTER_LIMIT + 1) ? input.text.Length : CHARACTER_LIMIT + 1);
        if (m_SplitType == SplitType.ASCII)
        {
            return SplitNameByASCII(temp);
        }
        else if (m_SplitType == SplitType.GB)
        {
            return SplitNameByGB(temp);
        }
        else if (m_SplitType == SplitType.Unicode)
        {
            return SplitNameByUnicode(temp);
        }
        else if (m_SplitType == SplitType.UTF8)
        {
            return SplitNameByUTF8(temp);
        }

        return "";
    }

    private string SplitNameByASCII(string temp)
    {
        byte[] encodedBytes = System.Text.Encoding.ASCII.GetBytes(temp);

        string outputStr = "";
        int count = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            //双字节
            if ((int)encodedBytes[i] == 63)
                count += 2;
            else
                count += 1;

            if (count <= CHARACTER_LIMIT)
                outputStr += temp.Substring(i, 1);
            else if (count > CHARACTER_LIMIT)
                break;
        }

        if (count <= CHARACTER_LIMIT)
        {
            outputStr = temp;

        }

        return outputStr;
    }

    private string SplitNameByGB(string temp)
    {
        string outputStr = "";
        int count = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            string tempStr = temp.Substring(i, 1);
            byte[] encodedBytes = System.Text.Encoding.Default.GetBytes(tempStr);
            if (encodedBytes.Length == 1)
            {
                //单字节
                count += 1;
            }
            else
            {
                //双字节
                count += 2;
            }

            if (count <= CHARACTER_LIMIT)
            {
                outputStr += tempStr;
            }
            else
            {
                break;
            }
        }
        return outputStr;
    }

    private string SplitNameByUnicode(string temp)
    {
        string outputStr = "";
        int count = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            string tempStr = temp.Substring(i, 1);
            //Unicode用两个字节对字符进行编码
            byte[] encodedBytes = System.Text.Encoding.Unicode.GetBytes(tempStr);
            if (encodedBytes.Length == 2)
            {
                int byteValue = (int)encodedBytes[1];
                //这里是单个字节
                if (byteValue == 0)
                {
                    count += 1;
                }
                else
                {
                    count += 2;
                }
            }
            if (count <= CHARACTER_LIMIT)
            {
                outputStr += tempStr;
            }
            else
            {
                break;
            }
        }
        return outputStr;
    }

    // UTF8编码格式,汉字3byte,英文1byte
    // UTF8编码格式,目前是最常用的 
    private string SplitNameByUTF8(string temp)
    {
        Debug.Log("temp =============== " + temp);
        string outputStr = "";
        int count = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            string tempStr = temp.Substring(i, 1);
            byte[] encodedBytes = System.Text.Encoding.UTF8.GetBytes(tempStr);
            string output = "[" + temp + "]";
            for (int byteIndex = 0; byteIndex < encodedBytes.Length; byteIndex++)
            {
                //二进制
                output += Convert.ToString((int)encodedBytes[byteIndex], 2) + "  ";
            }
            Debug.Log(output);

            int byteCount = System.Text.Encoding.UTF8.GetByteCount(tempStr);
            Debug.Log("字节数=" + byteCount);

            if (byteCount > 1)
            {
                count += 2;
            }
            else
            {
                count += 1;
            }
            if (count <= CHARACTER_LIMIT)
            {
                outputStr += tempStr;
            }
            else
            {
                break;
            }
        }
        return outputStr;
    }
}

