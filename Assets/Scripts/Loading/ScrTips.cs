using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using RDOnline.Utils;

[System.Serializable]
public class TipsData
{
    public string[] tips;
}

public static class ScrTips
{
    private static string[] tips;
    private static bool isLoaded = false;

    /// <summary>
    /// 从 Resources 文件夹加载提示数据
    /// </summary>
    private static void LoadTips()
    {
        if (isLoaded) return;

        TextAsset tipsJson = AssetBundleManager.instance.LoadAsset<TextAsset>("tips");
        if (tipsJson != null)
        {
            TipsData tipsData = JsonConvert.DeserializeObject<TipsData>(tipsJson.text);
            tips = tipsData.tips;
            isLoaded = true;
        }
        else
        {
            Debug.LogWarning("无法加载 tips.json 文件，使用默认提示");
            tips = new string[] { "享受游戏时光！" };
            isLoaded = true;
        }
    }

    /// <summary>
    /// 获取随机提示文本
    /// </summary>
    /// <returns>随机提示字符串</returns>
    public static string GetTips()
    {
        LoadTips();

        if (tips == null || tips.Length == 0)
            return "享受游戏时光！";

        int randomIndex = Random.Range(0, tips.Length);
        return tips[randomIndex];
    }
    
    /// <summary>
    /// 获取格式化的提示文本
    /// </summary>
    /// <returns>格式化后的提示文本</returns>
    public static string GetFormattedTips()
    {
        return GetTips();
    }
}
