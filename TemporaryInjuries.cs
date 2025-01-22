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
using UnityEngine.UIElements;

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
        public static void SelectCardPostfix(ref CardCraftManager __instance, string cardName)
        {
            string[] array = cardName.Split('_');
            string text = array[0];
            float multiplier = 5.0f;
            // float multiplier = HighCostInjuryMultiplier.Value;

            EnableHighCostInjuries.Value = true;
            if (!EnableHighCostInjuries.Value)
            {
                LogDebug("High Cost Injuries Disabled");
                return;
            }

            CardData cardData =Traverse.Create(__instance).Field("cardData").GetValue<CardData>();
            if (cardData.CardClass != Enums.CardClass.Injury || AtOManager.Instance.GetNgPlus() < 9)
            {
                return;
            }
            BotonGeneric BG_Remove = Traverse.Create(__instance).Field("BG_Remove").GetValue<BotonGeneric>();
            int costRemove = Traverse.Create(__instance).Field("costRemove").GetValue<int>();

            if (__instance.craftType == 1)
            {
                __instance.oldcostRemoveText.text = "";
                costRemove = Mathf.RoundToInt(SetPrice(__instance,"Remove", "")*multiplier);
                BG_Remove.SetText(__instance.ButtonText(costRemove));
                int num = SetPrice(__instance,"Remove", "", "", 0, useShopDiscount: false);
                if (num != costRemove)
                {
                    __instance.oldcostRemoveText.text = string.Format(Texts.Instance.GetText("oldCost"), num.ToString());
                }
                __instance.ShowElements("Remove", text);
            }
        }
        public static int SetPrice(CardCraftManager __instance, string function, string rarity, string cardName = "", int zoneTier = 0, bool useShopDiscount = true)
        {
            LogDebug("SetPriceLocal");

            int num = 0;
            bool isPetShop=Traverse.Create(__instance).Field("isPetShop").GetValue<bool>();
            // CardData cardData = Globals.Instance.GetCardData(cardName, instantiate: false);
            CardData cardData =Traverse.Create(__instance).Field("cardData").GetValue<CardData>();
            int discount = Traverse.Create(__instance).Field("discount").GetValue<int>();
            if(cardData == null)
            {
                LogError("Invalid SetPrice Traverse");
                return 0;
            }
            if (isPetShop)
            {
                if (cardName != "")
                {
                    rarity = Enum.GetName(typeof(Enums.CardRarity), Globals.Instance.GetCardData(cardName, instantiate: false).CardRarity);
                }
                if (rarity.ToLower() == "common")
                {
                    num = 72;
                }
                else if (rarity.ToLower() == "uncommon")
                {
                    num = 156;
                }
                else if (rarity.ToLower() == "rare")
                {
                    num = 348;
                }
                else if (rarity.ToLower() == "epic")
                {
                    num = 744;
                }
                else if (rarity.ToLower() == "mythic")
                {
                    num = 1200;
                }
            }
            else
            {
                switch (function)
                {
                    case "Remove":
                        if (zoneTier == 0 && GameManager.Instance.IsSingularity())
                        {
                            num = 0;
                            break;
                        }
                        num = Globals.Instance.GetRemoveCost(cardData.CardType, cardData.CardRarity);
                        if (cardData.CardType == Enums.CardType.Injury)
                        {
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_4"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.3f);
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_2"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.15f);
                            }
                            break;
                        }
                        if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_5"))
                        {
                            num -= Functions.FuncRoundToInt((float)num * 0.5f);
                        }
                        else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_3"))
                        {
                            num -= Functions.FuncRoundToInt((float)num * 0.25f);
                        }
                        if (!AtOManager.Instance.CharInTown())
                        {
                            break;
                        }
                        if (AtOManager.Instance.GetTownTier() == 1)
                        {
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_6"))
                            {
                                num = 0;
                            }
                        }
                        else if (AtOManager.Instance.GetTownTier() == 0 && PlayerManager.Instance.PlayerHaveSupply("townUpgrade_3_1"))
                        {
                            num = 0;
                        }
                        break;
                    case "Upgrade":
                    case "Transform":
                        num = Globals.Instance.GetUpgradeCost(function, rarity);
                        if (function == "Upgrade")
                        {
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_6"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.5f);
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_4"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.3f);
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_2"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.15f);
                            }
                        }
                        if (function == "Transform")
                        {
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_5"))
                            {
                                num = 0;
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_3"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.5f);
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_1"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.25f);
                            }
                        }
                        break;
                    case "Craft":
                        {
                            float discountCraft = 0f;
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_1_5"))
                            {
                                discountCraft = 0.3f;
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_1_2"))
                            {
                                discountCraft = 0.15f;
                            }
                            float discountUpgrade = 0f;
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_6"))
                            {
                                discountUpgrade = 0.5f;
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_4"))
                            {
                                discountUpgrade = 0.3f;
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_2_2"))
                            {
                                discountUpgrade = 0.15f;
                            }
                            num = Globals.Instance.GetCraftCost(cardName, discountCraft, discountUpgrade, zoneTier);
                            break;
                        }
                    case "Item":
                        num = Globals.Instance.GetItemCost(cardName);
                        if (AtOManager.Instance.CharInTown())
                        {
                            if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_5_5"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.3f);
                            }
                            else if (PlayerManager.Instance.PlayerHaveSupply("townUpgrade_5_2"))
                            {
                                num -= Functions.FuncRoundToInt((float)num * 0.15f);
                            }
                        }
                        break;
                    case "Corruption":
                        num = 300;
                        break;
                }
            }
            if (num > 0)
            {
                int num2 = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (AtOManager.Instance.GetHero(i) != null)
                    {
                        num2 += AtOManager.Instance.GetHero(i).GetItemDiscountModification();
                    }
                }
                int num3 = num2;
                if (useShopDiscount)
                {
                    num3 += discount;
                }
                if (num3 != 0)
                {
                    num -= Functions.FuncRoundToInt(num * num3 / 100);
                    if (num < 0)
                    {
                        num = 0;
                    }
                }
            }
            if (function == "Corruption" && IsCorruptionEnabled(__instance,0))
            {
                num += 400;
            }
            if (function == "Upgrade" && AtOManager.Instance.Sandbox_cardUpgradePrice != 0)
            {
                num += Functions.FuncRoundToInt((float)num * (float)AtOManager.Instance.Sandbox_cardUpgradePrice * 0.01f);
            }
            else if (function == "Transform" && AtOManager.Instance.Sandbox_cardTransformPrice != 0)
            {
                num += Functions.FuncRoundToInt((float)num * (float)AtOManager.Instance.Sandbox_cardTransformPrice * 0.01f);
            }
            else if (function == "Remove" && AtOManager.Instance.Sandbox_cardRemovePrice != 0)
            {
                num += Functions.FuncRoundToInt((float)num * (float)AtOManager.Instance.Sandbox_cardRemovePrice * 0.01f);
            }
            else if (function == "Item" && AtOManager.Instance.Sandbox_itemsPrice != 0)
            {
                num += Functions.FuncRoundToInt((float)num * (float)AtOManager.Instance.Sandbox_itemsPrice * 0.01f);
            }
            else if (isPetShop && AtOManager.Instance.Sandbox_petsPrice != 0)
            {
                num += Functions.FuncRoundToInt((float)num * (float)AtOManager.Instance.Sandbox_petsPrice * 0.01f);
            }
            return num;
        }


        public static bool IsCorruptionEnabled(CardCraftManager __instance, int _index)
    {
        LogDebug("IsCorruptionEnabledLocal");
        if (__instance.corruptionArrows[_index].gameObject.activeSelf)
        {
            return true;
        }
        return false;
    }


    }
}