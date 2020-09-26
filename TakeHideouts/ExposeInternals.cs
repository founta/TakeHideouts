using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;


using HarmonyLib;

namespace TakeHideouts
{
  [HarmonyPatch]
  public class ExposeInternals
  {

    //expose internal function Clan.RemoveWarPartyInternal
    //this shouldn't cause any problems to use
    //(war parties is only to calculate clan strength and take up
    // clan party limit, I think)
    //after removing them from war parties, you can no longer 
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Clan), "RemoveWarPartyInternal")]
    public static void RemoveWarPartyInternal(Clan instance, MobileParty warparty)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyScreenLogic), "SetPartyGoldChangeAmount")]
    public static void SetPartyGoldChangeAmount(PartyScreenLogic instance, int newTotalAmount)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(BanditsCampaignBehavior), "InitBanditParty")]
    public static void InitBanditParty(BanditsCampaignBehavior instance, MobileParty banditParty, TaleWorlds.Localization.TextObject name, Clan faction, Settlement homeSettlement)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(InventoryManager), "GetCurrentMarketData")]
    public static IMarketData GetCurrentMarketData(InventoryManager __instance)
    {
      return null;
    }
    /*
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyAi), "SetDefaultBehavior")]
    public static void SetDefaultBehavior(PartyAi instance, AiBehavior newAiState, PartyBase targetParty)
    {
      return;
    }
    */
  }
}
