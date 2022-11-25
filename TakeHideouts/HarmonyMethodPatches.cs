using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using SandBox.Missions.MissionLogics;
using SandBox.ViewModelCollection.Map;
using SandBox.ViewModelCollection.Nameplate;

using Helpers;

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

  //patch AddBanditToHideout to never allow adding bandits to player owned hideouts
  [HarmonyPatch(typeof(BanditsCampaignBehavior), "AddBanditToHideout")]
  public class AddBanditToHideoutPatch
  {
    static bool Prefix(BanditsCampaignBehavior __instance, Hideout hideoutComponent, PartyTemplateObject overridenPartyTemplate, bool isBanditBossParty,
      ref MobileParty __result)
    {
      if (Common.IsOwnedHideout(hideoutComponent)) //then it's a player-owned hideout
      {
        __result = (MobileParty)null;
        return false;
      }
      return true;
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
        int troop_price = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero) * command.TotalNumber;
        if (command.Character.Occupation == Occupation.Bandit)
          troop_price *= TakeHideoutsSettings.Instance.BanditRecruitmentCostMultiplier;
        if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.CurrentData.PartyGoldChangeAmount + troop_price);
        else
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.CurrentData.PartyGoldChangeAmount - troop_price);
        //InformationManager.DisplayMessage(new InformationMessage($"Transferrable with trade! changed gold amount to {__instance.CurrentData.PartyGoldChangeAmount}... ransom value {Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(command.Character, Hero.MainHero)}, number {command.TotalNumber}"));
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
      if (__result && Settlement.CurrentSettlement.IsHideout)
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
      if (!Settlement.CurrentSettlement.IsHideout)
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
      newBoss.IsActive = false;

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
      {
        if (Hero.MainHero.Clan.WarPartyComponents.Contains(__instance.WarPartyComponent))
          ExposeInternals.OnWarPartyRemoved(Hero.MainHero.Clan, __instance.WarPartyComponent);
        //ExposeInternals.RemovePartyInternal(Hero.MainHero.Clan, __instance.Party);
        //ExposeInternals.RemoveWarPartyInternal(__instance.ActualClan, __instance);
      }
    }
  }

  //Patch 

  //patch mobile tracker VM so that we can't see bandit parties on the map, if the user wants
  [HarmonyPatch(typeof(MapMobilePartyTrackerVM), "InitList")]
  public class PartyTrackerPatch
  {
    static void Postfix(MapMobilePartyTrackerVM __instance)
    {
      foreach (WarPartyComponent party in Clan.PlayerClan.WarPartyComponents)
      {
        if (Common.IsOwnedBanditParty(party.MobileParty))
          ExposeInternals.RemoveIfExists(__instance, party.MobileParty);
      }
    }
    static bool Prepare()
    {
      return !TakeHideoutsSettings.Instance.ShowBanditPatrolMobilePartyTracker;
    }
  }

  //remove newly created patrol parties from the map tracker, as well
  [HarmonyPatch(typeof(MapMobilePartyTrackerVM), "OnMobilePartyCreated")]
  public class PartyCreatedTrackerPatch
  {
    static void Postfix(MapMobilePartyTrackerVM __instance, MobileParty party)
    {
        if (Common.IsOwnedBanditParty(party))
          ExposeInternals.RemoveIfExists(__instance, party);
    }
    static bool Prepare()
    {
      return !TakeHideoutsSettings.Instance.ShowBanditPatrolMobilePartyTracker;
    }
  }

  //patch mobile tracker VM so that we can't see bandit boss parties on the map
  [HarmonyPatch(typeof(MapMobilePartyTrackerVM), "InitList")]
  public class PartyTrackerBanditBossPatch
  {
    static void Postfix(MapMobilePartyTrackerVM __instance)
    {
      foreach (WarPartyComponent party in Clan.PlayerClan.WarPartyComponents)
      {
        if (Common.IsOwnedBanditParty(party.MobileParty) && party.MobileParty.IsBanditBossParty)
            ExposeInternals.RemoveIfExists(__instance, party.MobileParty);
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
          if (settlement.IsHideout)
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
  public class EnterSettlementFoodPatch
  {
    static void Postfix(SettlementComponent __instance, MobileParty mobileParty)
    {
      if (mobileParty == null)
        return;

      Hideout hideout = null;
      if (__instance.Settlement.IsHideout)
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


  //have owned bandit parties drop off loot and prisoners
  [HarmonyPatch(typeof(SettlementComponent), "OnPartyEntered")]
  public class EnterSettlementLootPatch
  {
    static void Postfix(SettlementComponent __instance, MobileParty mobileParty)
    {
      if (mobileParty == null)
        return;

      Hideout hideout = null;
      if (__instance.Settlement.IsHideout)
        hideout = __instance.Settlement.Hideout;
      if (hideout == null)
        return;

      //only select owned bandit parties
      if (!Common.IsOwnedBanditParty(mobileParty))
        return;

      ItemRoster partyInventory = mobileParty.Party.ItemRoster;

      //transfer any prisoners to the hideout
      if (mobileParty.PrisonRoster.Count > 0)
      {
        hideout.Settlement.Party.PrisonRoster.Add(mobileParty.PrisonRoster.ToFlattenedRoster());
        mobileParty.PrisonRoster.Clear();
      }

      //transfer any loot to the hideout stash
      bool partyHasLoot = true;
      while (partyHasLoot)
      {
        List<ItemObject> lootItems = new List<ItemObject>();

        //loop through party inventory to look for loot
        bool lootFound = false;
        for (int i = 0; i < partyInventory.Count; ++i)
        {
          ItemObject item = partyInventory.GetItemAtIndex(i);
          if (!item.IsFood)
          {
            lootItems.Add(item);
            lootFound = true;
          }
        }

        if (!lootFound)
          partyHasLoot = false;
        else
        {
          for (int i = 0; i < lootItems.Count; ++i) //add to the stash and remove from the party
          {
            ItemObject item = lootItems[i];
            int numItems = partyInventory.GetItemNumber(item);
            hideout.Settlement.Stash.AddToCounts(item, numItems);
            partyInventory.AddToCounts(item, -numItems);
          }
        }
      }
    }
  }

  //refresh nameplates on hideout abandon so that they turn red again
  [HarmonyPatch(typeof(SettlementNameplatesVM), "Initialize")]
  public class NameplateRegisterPatch
  {
    public class NameplateActionWrapper
    {
      SettlementNameplatesVM instance;
      public NameplateActionWrapper(SettlementNameplatesVM instance)
      {
        this.instance = instance;
      }
      public void callRefreshRelations()
      {
        ExposeInternals.RefreshRelationsOfNameplates(instance);
      }
    }
    static void Prefix(SettlementNameplatesVM __instance)
    {
      NameplateActionWrapper nameplateActioner = new NameplateActionWrapper(__instance);
      HideoutOwnershipBehavior.hideoutAbandonedEvent.AddNonSerializedListener((object)__instance, new Action(nameplateActioner.callRefreshRelations));
    }
  }

  [HarmonyPatch(typeof(SettlementNameplatesVM), "OnFinalize")]
  public class NameplateUnregisterPatch
  {
    static void Postfix(SettlementNameplatesVM __instance)
    {
      HideoutOwnershipBehavior.hideoutAbandonedEvent.ClearListeners((object)__instance);
    }
  }


  //override the party size for owned bandit patrol parties
  [HarmonyPatch(typeof(DefaultPartySizeLimitModel), "CalculateMobilePartyMemberSizeLimit")]
  public class CalculateMobilePartyMemberSizeLimitPatch
  {
    static void Postfix(DefaultPartySizeLimitModel __instance, MobileParty party, bool includeDescriptions, ref ExplainedNumber __result)
    {
      if (Common.IsOwnedBanditParty(party))
      {
        __result = new ExplainedNumber(TakeHideoutsSettings.Instance.HideoutPatrolSize, includeDescriptions);
      }
    }
  }

  //prevent bandit patrols from visiting settlements
  [HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
  public class PatrolSettlementVisitPatch
  {
    static bool Prefix(AiVisitSettlementBehavior __instance, MobileParty mobileParty, PartyThinkParams p)
    {
      if (Common.IsOwnedBanditParty(mobileParty))
        return false;

      return true;
    }
  }

  /*
  [HarmonyPatch(typeof(MobileParty), "GetBestInitiativeBehavior")]
  public class DebugPrintPatch
  {
    static void Postfix(MobileParty __instance, ref AiBehavior bestInitiativeBehavior, ref float bestInitiativeBehaviorScore)
    {
      if (Common.IsOwnedBanditParty(__instance))
      {
        //string text = $"tc {__instance.MemberRoster.TotalManCount} behavior {bestInitiativeBehavior.ToString()} score {bestInitiativeBehaviorScore}";
        //MBInformationManager.AddQuickInformation(new TextObject(text));
      }
    }
  }
  */

  //if the method selects an owned hideout, re-select a non-owned hideout.
  [HarmonyPatch(typeof(SettlementHelper), "FindNearestHideout")]
  public class FindNearestHideoutPatch
  {

    static void Postfix(Func<Settlement, bool> condition, IMapPoint toMapPoint, ref Settlement __result)
    {
      if (__result == null)
        return;
      if (__result.Hideout == null)
        return;

      if (Common.IsOwnedHideout(__result.Hideout))
      {
        Func<Settlement, bool> is_not_owned_hideout_delegate = set => (!Common.IsOwnedHideout(set.Hideout));

        if (condition != null)
        {
          is_not_owned_hideout_delegate = set => (!Common.IsOwnedHideout(set.Hideout)) && condition(set);
        }

        __result = ExposeInternals.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint)MobileParty.MainParty, 
          Hideout.All.Select<Hideout, Settlement>((Func<Hideout, Settlement>)(x => x.Settlement)), is_not_owned_hideout_delegate);
      }
    }
  }

  [HarmonyPatch(typeof(SettlementHelper), "FindRandomHideout")]
  public class FindRandomHideoutPatch
  {
    static void Postfix(Func<Settlement, bool> condition, ref Settlement __result)
    {
      if (__result == null)
        return;
      if (__result.Hideout == null)
        return;
      if (Common.IsOwnedHideout(__result.Hideout))
      {
        Func<Settlement, bool> is_not_owned_hideout_delegate = set => (!Common.IsOwnedHideout(set.Hideout));

        if (condition != null)
        {
          is_not_owned_hideout_delegate = set => (!Common.IsOwnedHideout(set.Hideout)) && condition(set);
        }

        __result = ExposeInternals.FindRandomInternal(is_not_owned_hideout_delegate,
          Hideout.All.Select<Hideout, Settlement>((Func<Hideout, Settlement>)(x => x.Settlement)));
      }
    }
  }

  [HarmonyPatch(typeof(CampaignEventDispatcher), "OnSettlementOwnerChanged")]
  public class OnSettlementOwnerChangedPatch
  {
    static void Prefix(CommentOnChangeSettlementOwnerBehavior __instance, Settlement settlement, ref Hero oldOwner)
    {
      if (Common.IsHideout(settlement))
      {
        if (oldOwner == null)
          oldOwner = Hero.MainHero;
      }
    }
  }

  [HarmonyPatch(typeof(BanditsCampaignBehavior), "OnSettlementEntered")]
  public class HideoutEnteredPatch
  {
    static bool Prefix(BanditsCampaignBehavior __instance, Settlement settlement)
    {
      if (Common.IsHideout(settlement))
      {
        if (Common.IsOwnedHideout(settlement.Hideout))
          return false;
      }
      return true;
    }
  }

}
