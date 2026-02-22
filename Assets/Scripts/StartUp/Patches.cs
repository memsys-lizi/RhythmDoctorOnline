using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using RDOnline.Utils;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#pragma warning disable 0455
namespace RDOnline
{
#if RHYTHMDOCTOR
    public class Patches
    {
        public static RankScore RankScore = new();
        [HarmonyPatch(typeof(scnGame), "EndLevel")]
        public static class scnGame_EndLevel_Patch
        {
            public static void Prefix(scnGame __instance)
            {
                try
                {
                    Rank rankFromMistakes = __instance.currentLevel.GetRankFromMistakes();
                    string rank = rankFromMistakes.ToString();
                    RankScore rankScore = new RankScore
                    {
                        rank = rank,
                        earlyOffsetsSum = __instance.mistakesManager.earlyOffsetsSum,
                        lateOffsetsSum = __instance.mistakesManager.lateOffsetsSum,
                        totalOffsetsSum = __instance.mistakesManager.totalOffsetsSum,
                        earlyOffsetsSumP1 = __instance.mistakesManager.earlyOffsetsSumP1,
                        lateOffsetsSumP1 = __instance.mistakesManager.lateOffsetsSumP1,
                        totalOffsetsSumP1 = __instance.mistakesManager.totalOffsetsSumP1,
                        earlyOffsetsSumP2 = __instance.mistakesManager.earlyOffsetsSumP2,
                        lateOffsetsSumP2 = __instance.mistakesManager.lateOffsetsSumP2,
                        totalOffsetsSumP2 = __instance.mistakesManager.totalOffsetsSumP2,
                        mistakes = __instance.mistakesManager.mistakes,
                        mistakesP1 = __instance.mistakesManager.mistakesP1,
                        mistakesP2 = __instance.mistakesManager.mistakesP2
                    };
                    RankScore = rankScore;
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                }
            }
        }
    }
#endif
}