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

      PartyScreenAdditions.OpenPartyScreenAsBuyTroops(party.Party);
    }

    private void inquiry_do_nothing(List<InquiryElement> elements)
    {
      return;
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
