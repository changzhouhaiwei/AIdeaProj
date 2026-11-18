using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AccountData
{
    public List<string> accountIds = new List<string>();

    public static void AddAccountId(string accountId)
    {
        List<string> accountIds = GetAccountIds();
        int index = -1;
        for (int i = accountIds.Count - 1; i >= 0; i--)
        {
            if (accountId == accountIds[i])
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
        {
            string temp = accountIds[index];
            accountIds.RemoveAt(index);
            accountIds.Insert(0, temp);
        }
        else
        {
            accountIds.Insert(0, accountId);
        }

        AccountData accountData = new AccountData
        {
            accountIds = accountIds
        };
        PlayerPrefs.SetString("AccountIds", JsonUtility.ToJson(accountData));
    }

    public static List<string> GetAccountIds()
    {
        AccountData accountData = new AccountData();
        JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString("AccountIds"), accountData);
        return accountData.accountIds;
    }

    public static string GetFirstAccountId()
    {
        List<string> accountIds = GetAccountIds();
        return accountIds.Count > 0 ? accountIds[0] : "1";
    }
}