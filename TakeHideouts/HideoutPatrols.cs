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

using HarmonyLib;

namespace TakeHideouts
{
  class HideoutPatrolsBehavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "patrol_create", "Create Patrol Party", patrol_create_condition, patrol_create_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "patrol_recall", "Recall Patrol Parties", patrol_recall_condition, patrol_recall_consequence); //doesn't work
      campaignGameStarter.AddGameMenuOption("hideout_place", "patrol_send", "Dispatch Patrol Parties", patrol_send_condition, patrol_send_consequence);
    }

    private bool patrol_create_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.ManageHideoutTroops;

      return Settlement.CurrentSettlement.Hideout.IsTaken && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    private void patrol_create_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      //make a bandit party in the hideout as an easy template
      BanditsCampaignBehavior banditBehavior = Campaign.Current.GetCampaignBehavior<BanditsCampaignBehavior>();
      MobileParty banditParty = Common.CreateOwnedBanditPartyInHideout(hideout, 60); 

        //banditBehavior.AddBanditToHideout(hideout, Hero.MainHero.Culture.BanditBossPartyTemplate);//Common.CreateBanditInHideout(hideout, Hero.MainHero.Culture.BanditBossPartyTemplate, 60);
        //Common.CreateBanditInHideout(hideout, Hero.MainHero.Culture.BanditBossPartyTemplate, -1);

      //wipe out the bandit party
      banditParty.Party.MemberRoster.Clear();

      //InformationManager.DisplayMessage(new InformationMessage($"Party limit {banditParty.Party.PartySizeLimit}"));

      //set main hero as owner of party
      Common.SetAsOwnedHideoutParty(banditParty, hideout);

      //allow user to add their own troops
      PartyScreenAdditions.OpenPartyScreenAsNewParty(banditParty.Party);
    }

    private bool patrol_recall_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;

      return Settlement.CurrentSettlement.Hideout.IsTaken && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    private void patrol_recall_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      foreach (MobileParty party in Hero.MainHero.Clan.AllParties)
      {
        if (party.HomeSettlement == hideout.Settlement)
        {
          party.SetMoveGoToSettlement(hideout.Settlement);
          //party.Ai.RethinkAtNextHourlyTick = true; //no
          //EnterSettlementAction.ApplyForParty()
        }
      }
    }

    private bool patrol_send_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

      return Settlement.CurrentSettlement.Hideout.IsTaken && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    private void patrol_send_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (party != MobileParty.MainParty && !party.IsBanditBossParty)
        {
          party.SetMovePatrolAroundSettlement(hideout.Settlement);
        }
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
