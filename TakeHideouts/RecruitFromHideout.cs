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
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_recruit", "Recruit Bandit Troops", hideout_recruit_access_condition, hideout_get_troops_consequence);

      string manageGameMenuTarget = "hideout_place";
      if (TakeHideoutsSettings.Instance.PatrolSubmenuEnabled)
        manageGameMenuTarget = HideoutPatrolsBehavior.submenu_id;
      
      campaignGameStarter.AddGameMenuOption(manageGameMenuTarget, "takehideouts_manage", "Manage Patrol Party Troops", hideout_manage_access_condition, hideout_get_troops_consequence);
    }

    private bool hideout_recruit_access_condition(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      args.optionLeaveType = GameMenuOption.LeaveType.Recruit;

      return !Common.IsOwnedHideout(hideout) && TakeHideoutsSettings.Instance.RecruitingBanditsEnabled;
    }
    private bool hideout_manage_access_condition(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      args.optionLeaveType = GameMenuOption.LeaveType.Recruit;

      return Common.IsOwnedHideout(hideout);// && TakeHideoutsSettings.Instance.RecruitingBanditsEnabled;
    }

    //show list of parties from which to recruit
    private void hideout_get_troops_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      List<InquiryElement> elements = Common.GetHideoutPartyInquiryElements(hideout);

      string inquiryHeader = "";
      string affirmativeLabel = "";
      if (Common.IsOwnedHideout(hideout))
      {
        inquiryHeader = "Choose bandit party to manage";
        affirmativeLabel = "Manage Troops";
      }
      else
      {
        inquiryHeader = "Choose bandit party to recruit from";
        affirmativeLabel = "Select Troops";
      }

      Common.OpenSingleSelectInquiry(inquiryHeader, elements, affirmativeLabel, RecruitFromHideoutBehavior.inquiry_recruit_troops);

      return;
    }


    public static void inquiry_recruit_troops(List<InquiryElement> party_list)
    {
      if (party_list.Count == 0)
        return;
      MobileParty party = (MobileParty)party_list[0].Identifier; //only one element

      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      if (Common.IsOwnedHideout(hideout))
      {
        PartyScreenAdditions.OpenPartyScreenAsManageParty(party);
        //InformationManager.DisplayMessage(new InformationMessage($"Manage owned hideout troops"));
      }
      else
      {
        PartyScreenAdditions.OpenPartyScreenAsBuyTroops(party);
        //InformationManager.DisplayMessage(new InformationMessage($"Purchase hideout troops"));
      }
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
