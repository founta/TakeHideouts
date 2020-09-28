using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core.ViewModelCollection;
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
      if (__instance.IsTaken) //if we've taken the hideout, ignore the ifBandit check
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

  //patch SelectARandomHideout to never select player-owned hideouts
  [HarmonyPatch(typeof(BanditsCampaignBehavior), "SelectARandomHideout")]
  public class SelectHideoutPatch
  {
    static void Postfix(BanditsCampaignBehavior __instance, ref Hideout __result)
    {
      if (__result != null)
      {
        if (__result.IsTaken) //then it's a player-owned hideout
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
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount + command.Character.PrisonerRansomValue(Hero.MainHero) * command.TotalNumber);
        else
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount - command.Character.PrisonerRansomValue(Hero.MainHero) * command.TotalNumber);
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
        if (Settlement.CurrentSettlement.Hideout.IsTaken)
        {
          __result = false;
        }
      }
    }
  }

  //patch clan parties VM so that we can't see bandit parties, if the user wants
  [HarmonyPatch(typeof(ClanPartiesVM), "RefreshPartiesList")]
  public class ClanPartiesVmPartyListPatch
  {
    static void Postfix(ClanPartiesVM __instance)
    {
      List<int> removalIndices = new List<int>();
      for (int i = 0; i < __instance.Parties.Count; ++i)//ClanPartyItemVM item in __instance.Parties.)
      {
        ClanPartyItemVM item = __instance.Parties[i];
        Settlement home = item.Party.MobileParty.HomeSettlement;
        if (home != null)
        {
          if (home.IsHideout() && home.Hideout.IsTaken) //then it's one of our bandit parties
          {
            removalIndices.Add(i);
          }
        }
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
 
      //only select parties whose home settlement is this hideout and if it is an owned hideout
      if (mobileParty.HomeSettlement == null)
        return;
      if (mobileParty.HomeSettlement != hideout.Settlement)
        return;
      if (!hideout.IsTaken)
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
        foodStore.AddToCountsAtIndex(cheapestFoodIdx, -foodToTransfer);
        partyInventory.AddToCounts(cheapestFood, foodToTransfer);

      } while ((hideoutFoodCount > 0) && (partyFoodCount < desiredPartyFoodCount));
    }
  }

}
