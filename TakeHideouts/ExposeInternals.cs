﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using SandBox.ViewModelCollection.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.SaveSystem;
using SandBox.ViewModelCollection.Nameplate;

using TaleWorlds.Library;

using HarmonyLib;
using Helpers;

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
    [HarmonyPatch(typeof(Clan), "OnWarPartyRemoved")]
    public static void OnWarPartyRemoved(Clan instance, WarPartyComponent warparty)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyScreenLogic), "SetPartyGoldChangeAmount")]
    public static void SetPartyGoldChangeAmount(PartyScreenLogic instance, int newTotalAmount)
    {
      return;
    }
/*
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyVisual), "IsEnemy")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetPVIsEnemy(PartyVisual instance, bool value)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyVisual), "IsFriend")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetPVIsFriend(PartyVisual instance, bool value)
    {
      return;
    }
    */
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyScreenManager), "IsDonating")]
    [HarmonyPatch(MethodType.Setter)]
    public static void SetIsDonating(PartyScreenManager instance, bool value)
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
    [HarmonyPatch(typeof(MapMobilePartyTrackerVM), "RemoveIfExists", new Type[] { typeof(MobileParty) })]
    public static void RemoveIfExists(MapMobilePartyTrackerVM instance, MobileParty party)
    {
      return;
    }

    /*
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Campaign), "InitializeTypes")]
    public static void InitializeTypes(Campaign instance)
    {
      return;
    }
    */

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
      throw new NotImplementedException("GetCurrentMarketData not found");
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_attack_hideout_parties_on_condition")]
    public static bool AttackHideoutCondition(HideoutCampaignBehavior __instance, MenuCallbackArgs args)
    {
      throw new NotImplementedException("game_menu_attack_hideout_parties_on_condition not found");
      //return false; //overridden, right?
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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SettlementNameplatesVM), "RefreshRelationsOfNameplates")]
    public static void RefreshRelationsOfNameplates(SettlementNameplatesVM __instance)
    {
      return;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SettlementHelper), "FindNearestSettlementToMapPointInternal")]
    public static Settlement FindNearestSettlementToMapPointInternal(IMapPoint mapPoint, IEnumerable<Settlement> settlementsToIterate, Func<Settlement,bool> condition=null)
    {
      return (Settlement)null;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SettlementHelper), "FindRandomInternal")]
    public static Settlement FindRandomInternal(Func<Settlement, bool> condition, IEnumerable<Settlement> settlementsToIterate)
    {
      return (Settlement)null;
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
