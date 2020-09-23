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
  class RecruitFromHideoutBehavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "recruit", "Recruit Troops", hideout_recruit_access_condition, hideout_recruit_consequence);
    }

    private bool hideout_recruit_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Recruit;

      return true && TakeHideoutsSettings.Instance.RecruitingBanditsEnabled;
    }

    //show list of parties from which to recruit
    private void hideout_recruit_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      List<InquiryElement> elements = new List<InquiryElement>();

      int banditPartyCounter = 1;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (!party.IsBanditBossParty && (party != MobileParty.MainParty))
        {
          //TODO show random troop instead of first one?
          elements.Add(
            new InquiryElement(
              (object)party,
              $"Bandit party {banditPartyCounter++} ({party.Party.MemberRoster.TotalManCount} troops)",
              new ImageIdentifier(CharacterCode.CreateFrom(party.Party.MemberRoster.ElementAt(0).Character)) //show sweet image of first troop
              )
            );
        }
      }

      InformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData("Choose bandit party to recruit from", "", elements, true, 1,
                                      "Select Troops", "Leave", this.inquiry_recruit_troops, this.inquiry_do_nothing));

      return;
    }


    private void inquiry_recruit_troops(List<InquiryElement> party_list)
    {
      if (party_list.Count == 0)
        return;
      MobileParty party = (MobileParty)party_list[0].Identifier; //only one element

      RecruitFromHideoutBehavior.OpenPartyScreenAsBuyTroops(party.Party);
    }

    private void inquiry_do_nothing(List<InquiryElement> elements)
    {
      return;
    }

    //This is horrifying
    public static void OpenPartyScreenAsBuyTroops(PartyBase partyToBuyFrom)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      //harmony patch in TakeHideouts is what allows buying troops with TransferableWithTrade set as the transfer set
      currentMode = PartyScreenMode.Ransom;
      partyScreenLogic = new PartyScreenLogic();
      partyScreenLogic.Initialize(partyToBuyFrom, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Recruit Bandits"));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.TransferableWithTrade, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(RecruitFromHideoutBehavior.TroopTransferableDelegate));
      partyScreenLogic.SetDoneHandler(new PartyPresentationDoneButtonDelegate(RecruitFromHideoutBehavior.recruitDoneHandler));
      partyScreenLogic.Parties[0].Add(partyToBuyFrom.MobileParty);
      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    //mostly copied/pasted from PartyScreenManager class from bannerlord dlls
    public static bool TroopTransferableDelegate(
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
    private static bool recruitDoneHandler(
      TroopRoster leftMemberRoster,
      TroopRoster leftPrisonRoster,
      TroopRoster rightMemberRoster,
      TroopRoster rightPrisonRoster,
      bool isForced,
      List<MobileParty> leftParties = null,
      List<MobileParty> rigthParties = null)
    {
      //assuming only one left party
      if (leftParties != null)
      {
        if (leftParties.Count != 0)
        {
          if (leftParties[0].MemberRoster.Count == 0)
            leftParties[0].RemoveParty();
        }
        else
        {
          InformationManager.DisplayMessage(new InformationMessage($"left party count zero. whyyy"));
        }
      }
      return true;
    }

    //dunno what these do
    public override void RegisterEvents()
    {
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
    }
    public override void SyncData(IDataStore dataStore)
    {
    }

  }
}
