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
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using SandBox.ViewModelCollection.MobilePartyTracker;
using TaleWorlds.SaveSystem;

using TaleWorlds.Library;

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
    [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "SwitchToMenuIfThereIsAnInterrupt")]
    public static void SwitchToMenuIfThereIsAnInterrupt(PlayerTownVisitCampaignBehavior instance, string currentMenuId)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_settlement_wait_on_init")]
    public static void game_menu_settlement_wait_on_init(PlayerTownVisitCampaignBehavior instance, MenuCallbackArgs args)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(MobilePartyTrackerVM), "RemoveIfExists", new Type[] { typeof(MobileParty) })]
    public static void RemoveIfExists(MobilePartyTrackerVM instance, MobileParty party)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Campaign), "InitializeTypes")]
    public static void InitializeTypes(Campaign instance)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyBase), "OnFinishLoadState")]
    public static void OnFinishLoadState(PartyBase instance)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Settlement), "OnLoad")]
    public static void OnLoad(Settlement instance, MetaData metaData)
    {
      return;
    }


    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Settlement), "Position2D")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetSettlementPosition(Settlement instance, Vec2 value)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SettlementComponent), "BackgroundMeshName")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetHideoutBackgroundMeshName(SettlementComponent instance, string value)
    {
      return;
    }
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SettlementComponent), "WaitMeshName")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetHideoutWaitMeshName(SettlementComponent instance, string value)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(InventoryManager), "GetCurrentMarketData")]
    public static IMarketData GetCurrentMarketData(InventoryManager __instance)
    {
      return null;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_attack_hideout_parties_on_condition")]
    public static bool AttackHideoutCondition(HideoutCampaignBehavior __instance, MenuCallbackArgs args)
    {
      return false; //overridden, right?
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_encounter_attack_on_consequence")]
    public static void AttackHideoutConsequence(HideoutCampaignBehavior __instance, MenuCallbackArgs args)
    {
      return;
    }
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "CanChangeStatusOfTroop")]
    public static bool CanChangeStatusOfTroop(HideoutCampaignBehavior __instance, CharacterObject character)
    {
      return false;
    }
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "OnTroopRosterManageDone")]
    public static void OnTroopRosterManageDone(HideoutCampaignBehavior __instance, TroopRoster hideoutTroops)
    {
      return;
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
