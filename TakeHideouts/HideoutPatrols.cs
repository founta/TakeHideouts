﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using SandBox.ViewModelCollection.Map;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using Helpers;

using HarmonyLib;

namespace TakeHideouts
{
  class HideoutPatrolsBehavior : CampaignBehaviorBase
  {
    public const string submenu_id = "takehideouts_patrols";

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      string gameMenuTarget = "hideout_place";
      if (TakeHideoutsSettings.Instance.PatrolSubmenuEnabled)
      {
        campaignGameStarter.AddGameMenu(submenu_id, "You plan out bandit patrols with the help of the bandit leader", stash_submenu_on_init, GameOverlays.MenuOverlayType.None);
        campaignGameStarter.AddGameMenuOption(submenu_id, "takehideouts_patrol_leave", "{=3sRdGQou}Leave", patrol_submenu_leave_condition, x => GameMenu.SwitchToMenu("hideout_place"));
        campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_patrol_submenu", "Manage Patrol Parties", patrol_submenu_access_condition, x => GameMenu.SwitchToMenu(submenu_id));
        gameMenuTarget = submenu_id;
      }
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_patrol_create", "Create Patrol Party", patrol_create_condition, patrol_create_consequence);
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_patrol_recall", "Recall Patrol Parties", patrol_recall_condition, patrol_recall_consequence); //doesn't work
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_patrol_send", "Dispatch Patrol Parties", patrol_send_condition, patrol_send_consequence);
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_patrol_inventory", "Manage Patrol Party Inventory", patrol_manage_inventory_condition, patrol_manage_inventory_consequence);
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_patrol_food_store", "Manage Patrol Party Food Store", patrol_manage_food_store_condition, patrol_manage_food_store_consequence);
    }

    [GameMenuInitializationHandler(submenu_id)]
    public static void stash_submenu_bkg_init(MenuCallbackArgs args)
    {
      args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
    }

    private void stash_submenu_on_init(MenuCallbackArgs args)
    {
      args.MenuTitle = new TaleWorlds.Localization.TextObject($"Manage the patrols of {Settlement.CurrentSettlement.Name}");
    }

    private bool patrol_submenu_leave_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Leave;

      return true;
    }

    private bool patrol_submenu_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout);
    }

    private bool patrol_manage_food_store_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Trade;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout) && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    private void patrol_manage_food_store_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      InventoryScreenAdditions.OpenScreenAsManageInventory(hideout.Settlement.Party.ItemRoster, header: "Manage Food Store", focus:InventoryManager.InventoryCategoryType.Goods);
      return;
    }


    private bool patrol_manage_inventory_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Trade;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout) && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    //show list of parties from which to recruit
    private void patrol_manage_inventory_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      List<InquiryElement> elements = Common.GetHideoutPartyInquiryElements(hideout, true);

      string inquiryHeader = "Manage the inventory of the chosen party";
      string affirmativeLabel = "Manage Party Inventory";

      Common.OpenSingleSelectInquiry(inquiryHeader, elements, affirmativeLabel, this.inquiry_manage_inventory);

      return;
    }

    private void inquiry_manage_inventory(List<InquiryElement> party_list)
    {
      if (party_list.Count == 0)
        return;
      MobileParty party = (MobileParty)party_list[0].Identifier; //only one element

      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      InventoryScreenAdditions.OpenScreenAsManageInventory(party.Party.ItemRoster);
    }


    private bool patrol_create_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout) && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    private void patrol_create_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      //make a bandit party in the hideout
      BanditsCampaignBehavior banditBehavior = Campaign.Current.GetCampaignBehavior<BanditsCampaignBehavior>();
      MobileParty banditParty = Common.CreateOwnedBanditPartyInHideout(hideout, 60); 

      //wipe out the bandit party
      banditParty.Party.MemberRoster.Clear();

      //InformationManager.DisplayMessage(new InformationMessage($"Party limit {banditParty.Party.PartySizeLimit}"));

      //set main hero as owner of party
      Common.SetAsOwnedHideoutParty(banditParty, hideout);

      //allow user to add their own troops
      PartyScreenAdditions.OpenPartyScreenAsNewParty(banditParty);
    }

    public static bool patrol_recall_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout) && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    public static void patrol_recall_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      foreach (MobileParty party in MobileParty.All)
      {
        if (!Common.IsOwnedBanditParty(party))
          continue;
        if (party.HomeSettlement == hideout.Settlement && !party.IsBanditBossParty)
        {
          //InformationManager.DisplayMessage(new InformationMessage($"bandit ai state {party.Ai.AiState} new decisions {party.Ai.DoNotMakeNewDecisions} is bandit {party.IsBandit}"));
          party.SetMoveGoToSettlement(hideout.Settlement);
          party.Ai.SetAIState(AIState.VisitingHideout);
          party.Ai.SetDoNotMakeNewDecisions(true); //TODO this makes it so they can't run away from stronger parties..
          //InformationManager.DisplayMessage(new InformationMessage($"Moving to hideout..."));

          //party.Ai.RethinkAtNextHourlyTick = true; //no
          //EnterSettlementAction.ApplyForParty()
        }
      }
    }

    public static bool patrol_send_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout) && TakeHideoutsSettings.Instance.HideoutPatrolsEnabled;
    }

    public static void patrol_send_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      List<MobileParty> partiesToRemove = new List<MobileParty>();
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (party.MemberRoster.Count == 0)
        {
          partiesToRemove.Add(party);
        }
        else if (party != MobileParty.MainParty && !party.IsBanditBossParty)
        {
          party.SetMovePatrolAroundSettlement(hideout.Settlement);
          //party.Ai.SetAIState(AIState.PatrollingAroundLocation);
          party.Ai.SetDoNotMakeNewDecisions(false);
          //party.Ai.SetDoNotMakeNewDecisions(true);
        }
      }

      foreach (MobileParty party in partiesToRemove)
        party.RemoveParty();
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
