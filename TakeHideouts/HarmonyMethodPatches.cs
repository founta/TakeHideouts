﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.CampaignSystem.Barterables;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core.ViewModelCollection;
using SandBox.ViewModelCollection.MobilePartyTracker;

using SandBox.Source.Missions;

using TaleWorlds.ObjectSystem;

using HarmonyLib;

namespace TakeHideouts
{
  //patch Hideout Mapfaction getter to correctly set hideout's mapfaction
  //after we take the hideout
  [HarmonyPatch(typeof(Hideout), "MapFaction")]
  [HarmonyPatch(MethodType.Getter)]
  public class HideoutMapFactionPatch
  {
    static void Postfix(Hideout __instance, ref IFaction __result)
    {
      if (Common.IsOwnedHideout(__instance)) //if we've taken the hideout, ignore the ifBandit check
      {
        bool factionSet = false;
        foreach (MobileParty party in __instance.Settlement.Parties)
        {
          __result = (IFaction)party.ActualClan;
          factionSet = true;
          break;
        }
        if (!factionSet)
        {
          __result = (IFaction)Clan.BanditFactions.FirstOrDefault<Clan>(); //sets it to some default bandit clan if no parties
        }
      }
    }
  }

  //patch IsBandit getter to exclude owned bandits
  [HarmonyPatch(typeof(MobileParty), "IsBandit")]
  [HarmonyPatch(MethodType.Getter)]
  public class IsBanditPatch
  {
    static void Postfix(MobileParty __instance, ref bool __result)
    {
      if (Common.IsOwnedBanditParty(__instance)) //cannot be isbandit for owned bandits, as it will override our custom AI
        __result = false;
    }
  }

  //patch Hideout IsInfested getter to always be false if hideout is taken
  //sometimes it is true for player owned hideouts?? don't know how that's possible though,
  //as IsBandit for player-owned parties is always false...
  //actually wandering bandit parties may have a chance to enter player-owned hideouts
  //If they do that, they can infest the hideout or change the MapFaction
  //will have to look at their AI
  //yep that can happen
  [HarmonyPatch(typeof(Hideout), "IsInfested")]
  [HarmonyPatch(MethodType.Getter)]
  public class HideoutIsInfestedPatch
  {
    static void Postfix(Hideout __instance, ref bool __result)
    {
      if (Common.IsOwnedHideout(__instance)) //if we've taken the hideout, ignore the ifBandit check
      {
        __result = false;
      }
    }
  }

  //patch bandit AI so that they cannot visit player hideouts
  [HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
  public class AiVisitSettlementPatch
  {
    static void Postfix(Hideout __instance, MobileParty mobileParty, PartyThinkParams p)
    {
      if (mobileParty.IsBandit) //then remove any desire to visit player-owned settlements
      {
        //check early exit conditions
        if (p.AIBehaviorScores.Count == 0)
          return;
        List<AIBehaviorTuple> visitSettlementKeys = new List<AIBehaviorTuple>();
        foreach (AIBehaviorTuple key in p.AIBehaviorScores.Keys)
          if (key.AiBehavior == AiBehavior.GoToSettlement)
            visitSettlementKeys.Add(key);
        if (visitSettlementKeys.Count == 0)
          return;

        //remove player hideouts from bandit behavior scores
        List<AIBehaviorTuple> removalKeys = new List<AIBehaviorTuple>();
        List<Hideout> playerHideouts = Common.GetPlayerOwnedHideouts();
        foreach (AIBehaviorTuple key in visitSettlementKeys)
          foreach (Hideout hideout in playerHideouts)
            if (key.Party == (IMapPoint)hideout.Settlement)
              removalKeys.Add(key);

        foreach (AIBehaviorTuple key in removalKeys)
          p.AIBehaviorScores.Remove(key);
      }
    }
  }

  //patch SelectARandomHideout to never select player-owned hideouts
  [HarmonyPatch(typeof(BanditsCampaignBehavior), "SelectARandomHideout")]
  public class SelectHideoutPatch
  {
    static void Postfix(BanditsCampaignBehavior __instance, ref Hideout __result)
    {
      if (__result != null)
      {
        if (Common.IsOwnedHideout(__result)) //then it's a player-owned hideout
        {
          __result = (Hideout)null;
        }
      }
    }
  }

  //patch troop transfer method to allow trading of party members
  //used to only allow trading prisoners
  //This is mostly copy-pasted from decompiled DLL, modified slightly
  [HarmonyPatch(typeof(PartyScreenLogic), "TransferTroop")]
  public class TakeHideoutsTransferTroopPatch
  {
    static void Postfix(PartyScreenLogic __instance, PartyScreenLogic.PartyCommand command)
    {
      if (__instance.MemberTransferState == PartyScreenLogic.TransferState.TransferableWithTrade && command.Type == PartyScreenLogic.TroopType.Member)
      {
        if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero) * command.TotalNumber);
        else
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount - Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero) * command.TotalNumber);
      }
    }    
  }

  //patch 'Wait until nightfall' on_condition to disable attacking the hideout
  //if we own the hideout
  //bonus -- this also disables the attack option if it's already night time
  [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_wait_until_nightfall_on_condition")]
  public class TakeHideoutsHideoutBehaviorPatch
  {
    static void Postfix(HideoutCampaignBehavior __instance, ref bool __result)
    {
      //if we've taken the hideout and we can attack it, disable attacking it
      if (__result && Settlement.CurrentSettlement.IsHideout())
      {
        if (Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout))
        {
          __result = false;
        }
      }
    }
  }


  //patch hideout mission controller to allow transferring hideout control
  //(and make a new bandit boss party as the old one is gone) if the player wins the hideout battle
  [HarmonyPatch(typeof(HideoutMissionController), "OnEndMission")]
  public class MissionControllerPatch
  {
    static void Postfix(HideoutMissionController __instance)
    {
      if (!Settlement.CurrentSettlement.IsHideout())
        return;
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      //if the player has won against a hideout they plan to take, mark it as hideout to take
      if ((MapEvent.PlayerMapEvent.BattleState == BattleState.AttackerVictory) && hideout.IsTaken)
      {
        HideoutOwnershipBehavior.hideoutToTake = hideout;
      }
      else //the player has lost, clear the indicator that they wanted to take the hideout
      {
        hideout.IsTaken = false;
        HideoutOwnershipBehavior.hideoutToTake = null;
      }
    }
    /*
    //if the player has won against a hideout they plan to take, set as an owned hideout and add new bandit boss
    if ((MapEvent.PlayerMapEvent.BattleState == BattleState.AttackerVictory) && hideout.IsTaken)
    {
      Common.SetAsOwnedHideout(hideout, false);

      MobileParty boss = Common.CreateOwnedBanditPartyInHideout(hideout, isBoss: true);
    }
    else //the player has lost, clear the indicator that they wanted to take the hideout
    {
      hideout.IsTaken = false;
    }*/
  }

  /*
//add a new party to prevent hideout from being cleared
[HarmonyPatch(typeof(HideoutMissionController), "OnEndMission")]
public class MissionControllerPatch
{
  static void Postfix(HideoutMissionController __instance)
  {
    if (!Settlement.CurrentSettlement.IsHideout())
      return;
    Hideout hideout = Settlement.CurrentSettlement.Hideout;

    //if the player has won against a hideout they plan to take, add new bandit boss
    if ((MapEvent.PlayerMapEvent.BattleState == BattleState.AttackerVictory) && hideout.IsTaken)
    {
      MobileParty boss = Common.CreateOwnedBanditPartyInHideout(hideout);
      boss.IsBanditBossParty = true;
    }
    else //the player has lost, clear the indicator that they wanted to take the hideout
    {
      hideout.IsTaken = false;
    }
  }
}
*/


  [HarmonyPatch(typeof(PlayerEncounter), "DoEnd")]
  public class MapEventPatch
  {
    //static void Postfix(HideoutMissionController __instance)
    static void Postfix(PlayerEncounter __instance)
    {
      if (HideoutOwnershipBehavior.hideoutToTake == null)
        return;

      Hideout hideout = HideoutOwnershipBehavior.hideoutToTake;
      Settlement settlement = hideout.Settlement;

      MobileParty newBoss = Common.CreateOwnedBanditPartyInHideout(hideout, isBoss: true); //add a party to prevent hideout from disappearing
      newBoss.DisableAi();

      hideout.IsSpotted = true;
      settlement.IsVisible = true;
      Common.SetAsOwnedHideout(hideout, true);

      HideoutOwnershipBehavior.hideoutToTake = null;
    }
  }

  //TODO common function for removing parties from clan parties VM that takes some comparison function

  //patch clan parties VM so that we can't see bandit boss parties
  [HarmonyPatch(typeof(ClanPartiesVM), "RefreshPartiesList")]
  public class ClanPartiesVmPartyListPatch
  {
    static void Postfix(ClanPartiesVM __instance)
    {
      List<int> removalIndices = new List<int>();
      for (int i = 0; i < __instance.Parties.Count; ++i)//ClanPartyItemVM item in __instance.Parties.)
      {
        ClanPartyItemVM item = __instance.Parties[i];
        MobileParty party = item.Party.MobileParty;
        
        if (Common.IsOwnedBanditParty(party))//then it's one of our bandit parties
          removalIndices.Add(i);
      }

      //remove parties from parties list.
      //in reverse order to prevent issues with index updates as we remove parties
      removalIndices.Reverse();
      foreach (int idx in removalIndices)
      {
        __instance.Parties.RemoveAt(idx);
      }
    }
    static bool Prepare()
    {
      return !TakeHideoutsSettings.Instance.ShowBanditsOnPartyScreen;
    }
  }

  //patch clan parties VM so that we can't see bandit boss parties
  [HarmonyPatch(typeof(ClanPartiesVM), "RefreshPartiesList")]
  public class DontShowBanditBossPatch
  {
    static void Postfix(ClanPartiesVM __instance)
    {
      List<int> removalIndices = new List<int>();
      for (int i = 0; i < __instance.Parties.Count; ++i)
      {
        ClanPartyItemVM item = __instance.Parties[i];
        MobileParty party = item.Party.MobileParty;

        if (Common.IsOwnedBanditParty(party))
          if (party.IsBanditBossParty)
            removalIndices.Add(i);
      }

      //remove parties from parties list.
      //in reverse order to prevent issues with index updates as we remove parties
      removalIndices.Reverse();
      foreach (int idx in removalIndices)
      {
        __instance.Parties.RemoveAt(idx);
      }
    }
  }

  //Patch mobile party after load so that it prevents bandit parties from taking up party limit
  [HarmonyPatch(typeof(MobileParty), "AfterLoad")]
  public class ClanPartyLimitPatch
  {
    static void Postfix(MobileParty __instance)
    {
      if (Common.IsOwnedBanditParty(__instance))
        ExposeInternals.RemoveWarPartyInternal(__instance.ActualClan, __instance);
    }
  }

  //patch mobile tracker VM so that we can't see bandit parties on the map, if the user wants
  [HarmonyPatch(typeof(MobilePartyTrackerVM), "InitList")]
  public class PartyTrackerPatch
  {
    static void Postfix(MobilePartyTrackerVM __instance)
    {
      foreach (MobileParty party in Clan.PlayerClan.AllParties)
      {
        if (Common.IsOwnedBanditParty(party))
          ExposeInternals.RemoveIfExists(__instance, party);
      }
    }
    static bool Prepare()
    {
      return !TakeHideoutsSettings.Instance.ShowBanditPatrolMobilePartyTracker;
    }
  }

  //patch mobile tracker VM so that we can't see bandit boss parties on the map
  [HarmonyPatch(typeof(MobilePartyTrackerVM), "InitList")]
  public class PartyTrackerBanditBossPatch
  {
    static void Postfix(MobilePartyTrackerVM __instance)
    {
      foreach (MobileParty party in Clan.PlayerClan.AllParties)
      {
        if (Common.IsOwnedBanditParty(party) && party.IsBanditBossParty)
            ExposeInternals.RemoveIfExists(__instance, party);
      }
    }
  }

  //patch mobile tracker VM so that bandit banners are set correctly
  [HarmonyPatch(typeof(MobilePartyTrackItemVM), "UpdateProperties")]
  public class BanditBannerPatch
  {
    static void Postfix(MobilePartyTrackItemVM __instance)
    {
      MobileParty _concernedMobileParty = __instance.TrackedArmy?.LeaderParty ?? __instance.TrackedParty; //this line taken from dlls
      ref ImageIdentifierVM _factionVisualBind = ref AccessTools.FieldRefAccess<MobilePartyTrackItemVM, ImageIdentifierVM>(__instance, "_factionVisualBind");

      if (Common.IsOwnedBanditParty(_concernedMobileParty))
      {
        _factionVisualBind = new ImageIdentifierVM(BannerCode.CreateFrom(Clan.PlayerClan.Banner), true);
      }
    }
  }
  /*
  [HarmonyPatch(typeof(SandBoxManager), "OnCampaignStart")]
  public class SandboxManagerPatch
  {
    static void Postfix(SandBoxManager __instance)
    {
      if (HideoutsAnywhere.save_companion != null)
      {
        MBObjectManager.Instance.LoadXml(HideoutsAnywhere.save_companion, null);
        InformationManager.DisplayMessage(new InformationMessage($"loaded custom xml"));
      }

      InformationManager.DisplayMessage(new InformationMessage($"loaded xmls, save companion {HideoutsAnywhere.save_companion}"));
    }
    static bool Prepare()
    {
      return false;
    }
  }
  */
  //remove all hideouts from possible barterable fiefs, as they crash the game
  [HarmonyPatch(typeof(FiefBarterBehavior), "CheckForBarters")]
  public class FiefBarterPatch
  {
    static void Postfix(FiefBarterBehavior __instance, ref BarterData args)
    {
      ref List<Barterable> _barterables = ref AccessTools.FieldRefAccess<BarterData, List<Barterable>>(args, "_barterables");

      List<int> removalIndices = new List<int>();
      for (int i = 0; i < _barterables.Count; ++i)
      {
        Barterable item = _barterables[i];
        if (item.Group is FiefBarterGroup)
        {
          FiefBarterable fief = (FiefBarterable)item;

          Settlement settlement = AccessTools.FieldRefAccess<FiefBarterable, Settlement>(fief, "_settlement");
          if (settlement.IsHideout())
            removalIndices.Add(i);
        }
      }

      //remove hideout fiefs from barterables list.
      //in reverse order to prevent issues with index updates as we remove them
      removalIndices.Reverse();
      foreach (int idx in removalIndices)
      {
        _barterables.RemoveAt(idx);
      }
    }
  }

  //have owned bandit parties get food from the food store
  [HarmonyPatch(typeof(SettlementComponent), "OnPartyEntered")]
  public class EnterSettlementPatch
  {
    static void Postfix(SettlementComponent __instance, MobileParty mobileParty)
    {
      if (mobileParty == null)
        return;

      Hideout hideout = null;
      if (__instance.IsHideout())
        hideout = __instance.Settlement.Hideout;
      if (hideout == null)
        return;

      //only select owned bandit parties
      if (!Common.IsOwnedBanditParty(mobileParty))
        return;

      //now take food from the hideout's food store. Prioritize lowest value food items
      ItemRoster foodStore = hideout.Settlement.Party.ItemRoster;
      ItemRoster partyInventory = mobileParty.Party.ItemRoster;

      int desiredPartyFoodCount = mobileParty.Party.NumberOfRegularMembers;
      int partyFoodCount = Common.GetFoodCount(partyInventory);

      if (partyFoodCount >= desiredPartyFoodCount)
        return;

      int hideoutFoodCount = Common.GetFoodCount(foodStore);
      if (hideoutFoodCount <= 0) //no food
        return;
      do
      {
        hideoutFoodCount = Common.GetFoodCount(foodStore);
        partyFoodCount = Common.GetFoodCount(partyInventory);

        int foodDifference = desiredPartyFoodCount - partyFoodCount;

        int cheapestFoodIdx = Common.GetCheapestFoodIdx(foodStore);
        if (cheapestFoodIdx < 0) //indicates couldn't find food. shouldn't happen, but...
          return;

        ItemObject cheapestFood = foodStore.GetItemAtIndex(cheapestFoodIdx);

        //compute how much food to transfer, either how much they need or how much we have
        int foodToTransfer = Math.Min(foodDifference, foodStore[cheapestFoodIdx].Amount);

        //transfer food
        foodStore.AddToCounts(cheapestFood, -foodToTransfer);
        partyInventory.AddToCounts(cheapestFood, foodToTransfer);

      } while ((hideoutFoodCount > 0) && (partyFoodCount < desiredPartyFoodCount));
    }
  }

}
