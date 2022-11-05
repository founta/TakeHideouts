using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;

using HarmonyLib;

namespace TakeHideouts
{
  class PartyScreenAdditions
  {
    //This is horrifying
    public static void OpenPartyScreenAsBuyTroops(MobileParty partyToBuyFrom)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.TroopsManage;
      partyScreenLogic = new PartyScreenLogic();
      ExposeInternals.SetIsDonating(PartyScreenManager.Instance, false);

      IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenAdditions.TroopOnlyLeftTransferableDelegate);
      PartyPresentationDoneButtonDelegate doneButtonDelegate = new PartyPresentationDoneButtonDelegate(partyEmptyDoneHandler);
      doneButtonDelegate += HideoutRogueryBehavior.PartyScreenDoneDelegate;

      partyScreenLogic.Initialize(PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(partyToBuyFrom, PartyScreenLogic.TransferState.TransferableWithTrade, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable,
        troopTransferableDelegate, new TaleWorlds.Localization.TextObject("Manage Party"), doneButtonDelegate));

      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    public static void OpenPartyScreenAsNewParty(MobileParty newParty, int partyLimitOverride = -1, PartyPresentationDoneButtonDelegate doneDelegateOverride = null)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      int partyLimit = newParty.Party.PartySizeLimit;
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
      ExposeInternals.SetIsDonating(PartyScreenManager.Instance, false);

      IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate);
      PartyPresentationDoneButtonDelegate doneButtonDelegate = new PartyPresentationDoneButtonDelegate(partyEmptyDoneHandler);
      partyScreenLogic.Initialize(PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(newParty, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable,
        troopTransferableDelegate, new TaleWorlds.Localization.TextObject("Create New Bandit Party"), doneDelegate));

      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    public static void OpenPartyScreenAsManageParty(MobileParty party)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      //harmony patch in PartyScreenLogic is what allows buying troops with TransferableWithTrade set as the transfer type
      currentMode = PartyScreenMode.TroopsManage;
      partyScreenLogic = new PartyScreenLogic();
      ExposeInternals.SetIsDonating(PartyScreenManager.Instance, false);

      IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate);
      PartyPresentationDoneButtonDelegate doneButtonDelegate = new PartyPresentationDoneButtonDelegate(partyEmptyDoneHandler);
      partyScreenLogic.Initialize(PartyScreenLogicInitializationData.CreateBasicInitDataWithMainPartyAndOther(party, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable,
        troopTransferableDelegate, new TaleWorlds.Localization.TextObject("Manage Party"), doneButtonDelegate));

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
      ExposeInternals.SetIsDonating(PartyScreenManager.Instance, false);

      IsTroopTransferableDelegate troopTransferableDelegate = new IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate);
      PartyPresentationDoneButtonDelegate doneButtonDelegate = new PartyPresentationDoneButtonDelegate(doNothingDoneHandler);
      partyScreenLogic.Initialize(PartyScreenLogicInitializationData.CreateBasicInitDataWithMainParty(party.MemberRoster, party.PrisonRoster, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.NotTransferable,
        troopTransferableDelegate, party, header: new TaleWorlds.Localization.TextObject("Manage Party"), partyPresentationDoneButtonDelegate: doneButtonDelegate));

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
      Hero leader = leftOwnerParty.LeaderHero;
      bool flag = leader != null && leader.Clan == Clan.PlayerClan || leftOwnerParty != null && leftOwnerParty.IsMobile && (leftOwnerParty.MobileParty.IsCaravan && leftOwnerParty.Owner == Hero.MainHero) || leftOwnerParty != null && leftOwnerParty.IsMobile && leftOwnerParty.MobileParty.IsGarrison && leftOwnerParty.MobileParty.CurrentSettlement?.OwnerClan == Clan.PlayerClan;
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
      PartyBase leftParty = null,
      PartyBase rigthParty = null)
    {
      if (leftParty != null && leftParty.MobileParty != null)
        if (leftParty.MobileParty.MemberRoster.Count == 0)
          leftParty.MobileParty.RemoveParty();
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
      PartyBase leftParty = null,
      PartyBase rigthParty = null)
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
