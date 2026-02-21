using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#pragma warning disable 0455
namespace ModEntry
{
#if RHYTHMDOCTOR
    public class Patches
    {
        private const string modName = "RDOL";
        [HarmonyPatch(typeof(scnMenu), "Awake")]
        public class Patch_scnMenu_Start
        {
            public static void Prefix(scnMenu __instance)
            {
                RectTransform optionsContainer = __instance.optionsContainer;
                if (optionsContainer != null)
                {
                    try
                    {
                        Transform val = optionsContainer.Find("customLevels");
                        GameObject val2 = Object.Instantiate(val.gameObject);
                        val2.transform.SetParent(optionsContainer.transform, false);
                        val2.transform.SetSiblingIndex(val.GetSiblingIndex() + 1);
                        val2.name = modName;
                        val2.gameObject.SetActive(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
            }

            public static void Postfix(scnMenu __instance)
            {
                RectTransform options = __instance.options;
                if (options != null)
                {
                    Transform val = __instance.optionsContainer.Find(modName);
                    if (val != null)
                    {
                        Rect rect = val.GetComponent<RectTransform>().rect;
                        float height = rect.height;
                        RectTransform component = options.GetComponent<RectTransform>();
                        component.position = new Vector2(component.position.x, component.position.y + height / 1.5f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RDString), "GetWithCheck")]
        public static class RDString_GetWithCheck
        {
            private static bool Prefix(string key, out bool exists, ref string __result)
            {
                if (key == "mainMenu." + modName)
                {
                    __result = "多人游戏";
                    exists = true;
                    return false;
                }
                exists = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(scnMenu), "SelectOption")]
        public static class scnMenu_SelectOption
        {
            private static void Prefix(scnMenu __instance)
            {
                int num = (int)__instance.GetType()
                    .GetField("currentOption", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(__instance);
                if ((__instance.GetType()
                        .GetField("optionsText", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(__instance) as Text[])[num].gameObject.name == modName)
                {
                    __instance.conductor.StopSong();
                    typeof(scnMenu).GetMethod("TransitionToScene", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[1] { "scnCheckUpdate" });
                }
            }
        }
    }
#endif
}