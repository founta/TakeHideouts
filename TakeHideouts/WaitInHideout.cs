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
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.CampaignSystem.Overlay;
using SandBox.View.Map;

using HarmonyLib;

namespace TakeHideouts
{
  class WaitInHideoutBehavior : CampaignBehaviorBase
  {
    public const string wait_menu_id = "takehideouts_wait_menu";

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddWaitGameMenu(wait_menu_id, "You are waiting in your hideout.", hideout_wait_init, hideout_wait_menu_condition, 
        (TaleWorlds.CampaignSystem.GameMenus.OnConsequenceDelegate)null, new OnTickDelegate(hideout_wait_menu_tick), 
        GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None);

      campaignGameStarter.AddGameMenuOption(wait_menu_id, "takehideouts_wait_menu_recall_patrols", "Recall Patrol Parties", HideoutPatrolsBehavior.patrol_recall_condition, 
        HideoutPatrolsBehavior.patrol_recall_consequence);
      campaignGameStarter.AddGameMenuOption(wait_menu_id, "takehideouts_wait_menu_send_patrols", "Dispatch Patrol Parties", HideoutPatrolsBehavior.patrol_send_condition,
        HideoutPatrolsBehavior.patrol_send_consequence);
      campaignGameStarter.AddGameMenuOption(wait_menu_id, "takehideouts_wait_menu_leave", "{=UqDNAZqM}Stop waiting", hideout_wait_leave_condition, (x => GameMenu.SwitchToMenu("hideout_place")));

      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_hideout_wait_here", "{=zEoHYEUS}Wait here for some time", 
        hideout_wait_condition, 
        (GameMenuOption.OnConsequenceDelegate)(x => GameMenu.SwitchToMenu(wait_menu_id)));
    }

    //same as PlayerTownVisitCampaignBehavior wait tick
    private void hideout_wait_menu_tick(MenuCallbackArgs args, CampaignTime dt)
    {
      //this just cancels waiting...? TODO figure out. Not high priority though
      //ExposeInternals.SwitchToMenuIfThereIsAnInterrupt(Campaign.Current.GetCampaignBehavior<PlayerTownVisitCampaignBehavior>(), args.MenuContext.GameMenu.StringId);
    }

    //same as PlayerTownVisitCampaignBehavior wait init
    private void hideout_wait_init(MenuCallbackArgs args)
    {
      ExposeInternals.game_menu_settlement_wait_on_init(Campaign.Current.GetCampaignBehavior<PlayerTownVisitCampaignBehavior>(), args);
    }

    //can wait here if you own the hideout
    private bool hideout_wait_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Wait;

      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      return hideout.IsTaken;      
    }

    private bool hideout_wait_menu_condition(MenuCallbackArgs args)
    {
      args.MenuContext.GameMenu.AllowWaitingAutomatically();
      return hideout_wait_condition(args);
    }

    private bool hideout_wait_leave_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Leave;

      return true;
    }

    public override void RegisterEvents()
    {
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
    }
    public override void SyncData(IDataStore dataStore)
    {
    }
  }


}
