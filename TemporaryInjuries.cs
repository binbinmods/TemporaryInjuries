using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
// using static Obeliskial_Essentials.Essentials;
using System;
using static Ultrafast.CustomFunctions;
using static TemporaryInjuries.Plugin;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Make sure your namespace is the same everywhere
namespace TemporaryInjuries
{

    [HarmonyPatch] //DO NOT REMOVE/CHANGE

    public class TemporaryInjuries
    {
        // To create a patch, you need to declare either a prefix or a postfix. 
        // Prefixes are executed before the original code, postfixes are executed after
        // Then you need to tell Harmony which method to patch.

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardCraftManager), nameof(CardCraftManager.ShowElements))]
        public static void ShowElementsPostfix(ref CardCraftManager __instance, string direction, string cardId = "")
        {
            LogInfo("ShowElementsPostfix");
            if (__instance.craftType != 1 || direction == "")
            {
                return;
            }
            if (Globals.Instance == null || AtOManager.Instance == null)
            {
                LogDebug("Null Instance");
                return;
            }
            CardData cardData = Globals.Instance.GetCardData(cardId, instantiate: false);
            bool flag = true;
            bool flag2 = false;
            Hero currentHero = Traverse.Create(__instance).Field("currentHero").GetValue<Hero>();
            if (currentHero == null)
            {
                LogError("Null currentHero");
                return;
            }
            if (cardData.CardClass == Enums.CardClass.Injury && AtOManager.Instance.GetNgPlus() >= 9)
            {
                flag2 = false;
            }
            else if (cardData.CardClass == Enums.CardClass.Injury || cardData.CardClass == Enums.CardClass.Boon)
            {
                if (currentHero.GetTotalCardsInDeck() <= 15)
                {
                    flag = false;
                }
            }
            else if (currentHero.GetTotalCardsInDeck(excludeInjuriesAndBoons: true) <= 15)
            {
                flag = false;
            }
            BotonGeneric BG_Remove = Traverse.Create(__instance).Field("BG_Remove").GetValue<BotonGeneric>();
            if (BG_Remove == null)
            {
                LogError("Null BG_Remove");
                return;
            }
            if (!flag2 && (flag || AtOManager.Instance.Sandbox_noMinimumDecksize))
            {
                if (__instance.CanBuy("Remove"))
                {

                    BG_Remove.Enable();
                }
                else
                {
                    BG_Remove.Disable();
                }
            }
            else
            {
                BG_Remove.Disable();
            }
            return;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardCraftManager), nameof(CardCraftManager.SelectCard))]
        public static void SelectCardPostfix()
        {
            if (craftType == 1)
            {
                oldcostRemoveText.text = "";
                costRemove = SetPrice("Remove", "");
                BG_Remove.SetText(ButtonText(costRemove));
                num = SetPrice("Remove", "", "", 0, useShopDiscount: false);
                if (num != costRemove)
                {
                    oldcostRemoveText.text = string.Format(Texts.Instance.GetText("oldCost"), num.ToString());
                }
                ShowElements("Remove", text);
            }
        }


    }
}