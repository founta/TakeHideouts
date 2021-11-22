using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

using HarmonyLib;

namespace TakeHideouts
{
  class PartyScreenAdditions
  {
    //This is horrifying
    public static void OpenPartyScreenAsBuyTroops(PartyBase partyToBuyFrom)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");
      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.TroopsManage;
      partyScreenLogic = new PartyScreenLogic();
      partyScreenLogic.Initialize(partyToBuyFrom, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Recruit Bandits"), new PartyPresentationDoneButtonDelegate(PartyScreenAdditions.partyEmptyDoneHandler));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.TransferableWithTrade, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenAdditions.TroopOnlyLeftTransferableDelegate));
// partyScreenLogic.SetDoneHandler(new PartyPresentationDoneButtonDelegate(PartyScreenAdditions.partyEmptyDoneHandler));
      partyScreenLogic.Parties[0].Add(partyToBuyFrom.MobileParty);
      ExposeInternals.SetIsDonating(PartyScreenManager.Instance, false);

      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    public static void OpenPartyScreenAsNewParty(PartyBase newParty, int partyLimitOverride = -1, PartyPresentationDoneButtonDelegate doneDelegateOverride = null)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      int partyLimit = newParty.PartySizeLimit;
      if (partyLimitOverride > 0)
        partyLimit = partyLimitOverride;

      PartyPresentationDoneButtonDelegate doneDelegate;
      if (doneDelegateOverride == null)
        doneDelegate = PartyScreenAdditions.partyEmptyDoneHandler;
      else
        doneDelegate = doneDelegateOverride;

      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.TroopsManage;
      partyScreenLogic = new PartyScreenLogic();
      //partyScreenLogic.Initialize(newParty, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Create New Party"));
      partyScreenLogic.Initialize(newParty, MobileParty.MainParty, false, new TaleWorlds.Localization.TextObject("Create New Bandit Party"), partyLimit, new PartyPresentationDoneButtonDelegate(doneDelegate), new TaleWorlds.Localization.TextObject("Create New Party"));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));

      //partyScreenLogic.SetDoneHandler();
      partyScreenLogic.Parties[0].Add(newParty.MobileParty);
      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    public static void OpenPartyScreenAsManageParty(PartyBase party)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.Loot;
      partyScreenLogic = new PartyScreenLogic();
      partyScreenLogic.Initialize(party, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Manage Party"), new PartyPresentationDoneButtonDelegate(PartyScreenAdditions.partyEmptyDoneHandler));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
      partyScreenLogic.Parties[0].Add(party.MobileParty);
      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    public static void OpenPartyScreenAsManagePersistentParty(PartyBase party)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.TroopsManage;
      partyScreenLogic = new PartyScreenLogic();
      partyScreenLogic.Initialize(party, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Manage Party"), new PartyPresentationDoneButtonDelegate(doNothingDoneHandler));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
      partyScreenLogic.Parties[0].Add(party.MobileParty);
      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    //mostly copied/pasted from PartyScreenManager class from bannerlord dlls
    public static bool TroopOnlyLeftTransferableDelegate(
      CharacterObject character,
      PartyScreenLogic.TroopType type,
      PartyScreenLogic.PartyRosterSide side,
      PartyBase leftOwnerParty)
    {
      CharacterObject leader = leftOwnerParty?.Leader;
      bool flag = leader != null && leader.IsHero && leader.HeroObject.Clan == Clan.PlayerClan || leftOwnerParty != null && leftOwnerParty.IsMobile && (leftOwnerParty.MobileParty.IsCaravan && leftOwnerParty.Owner == Hero.MainHero) || leftOwnerParty != null && leftOwnerParty.IsMobile && leftOwnerParty.MobileParty.IsGarrison && leftOwnerParty.MobileParty.CurrentSettlement?.OwnerClan == Clan.PlayerClan;
      if (!character.IsHero && side == PartyScreenLogic.PartyRosterSide.Left) //only addition is here (side==left)
        return true;
      if (!character.IsHero || character.HeroObject.Clan == Clan.PlayerClan)
        return false;
      return !character.HeroObject.IsPlayerCompanion || character.HeroObject.IsPlayerCompanion & flag;
    }


    //kill the party if no more troops
    public static bool partyEmptyDoneHandler(
      TroopRoster leftMemberRoster,
      TroopRoster leftPrisonRoster,
      TroopRoster rightMemberRoster,
      TroopRoster rightPrisonRoster,
      FlattenedTroopRoster takenPrisonerRoster,
      FlattenedTroopRoster releasedPrisonerRoster,
      bool isForced,
      List<MobileParty> leftParties = null,
      List<MobileParty> rigthParties = null)
    {
      Common.RemoveEmptyParties(leftParties);
      return true;
    }

    public static bool doNothingDoneHandler(
      TroopRoster leftMemberRoster,
      TroopRoster leftPrisonRoster,
      TroopRoster rightMemberRoster,
      TroopRoster rightPrisonRoster,
      FlattenedTroopRoster takenPrisonerRoster,
      FlattenedTroopRoster releasedPrisonerRoster,
      bool isForced,
      List<MobileParty> leftParties = null,
      List<MobileParty> rigthParties = null)
    {
      return true;
    }

    public static bool partyEmptyCancelHandler()
    {
      //remove all empty parties in current settlement
      Settlement settlement = Settlement.CurrentSettlement;
      foreach (MobileParty party in settlement.Parties)
      {
        if (party.MemberRoster.Count == 0)
          party.RemoveParty();
      }
      return true;
    }
  }
}
