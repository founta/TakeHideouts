using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Localization;

namespace TakeHideouts
{
  class HideoutItemMemberManagementBehavior : CampaignBehaviorBase
  {
    public const string submenu_id = "takehideouts_stash";
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      //add submenu
      string gameMenuTarget = "hideout_place"; //default to no submenu
      if (TakeHideoutsSettings.Instance.StashSubmenuEnabled)
      {
        campaignGameStarter.AddGameMenu(submenu_id, "You meet with the bandit boss, who shows you to the hideout's stash", stash_submenu_on_init, GameOverlays.MenuOverlayType.None);
        campaignGameStarter.AddGameMenuOption(submenu_id, "takehideouts_stash_leave", "{=3sRdGQou}Leave", stash_submenu_leave_condition, x => GameMenu.SwitchToMenu("hideout_place"));
        campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_stash_submenu", "Manage Hideout Stash", stash_submenu_access_condition, x => GameMenu.SwitchToMenu(submenu_id));
        gameMenuTarget = submenu_id;
      }

      //add submenu options
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_stash", "Access Item Stash", hideout_stash_access_condition, hideout_stash_consequence);
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_troops", "Manage Hideout Troops", hideout_management_access_condition, hideout_troops_consequence);
      campaignGameStarter.AddGameMenuOption(gameMenuTarget, "takehideouts_prison", "Manage Hideout Prisoners", hideout_management_access_condition, hideout_prison_consequence);
    }

    [GameMenuInitializationHandler(submenu_id)]
    public static void stash_submenu_bkg_init(MenuCallbackArgs args)
    {
      args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.GetComponent<SettlementComponent>().WaitMeshName);
    }

    private void stash_submenu_on_init(MenuCallbackArgs args)
    {
      args.MenuTitle = new TextObject($"Manage the stash of {Settlement.CurrentSettlement.Name}");
    }
    private bool stash_submenu_leave_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Leave;

      return true;
    }
    private bool stash_submenu_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout);
    }


    private bool hideout_stash_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Trade;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout);
    }

    private void hideout_stash_consequence(MenuCallbackArgs args)
    {
      InventoryScreenAdditions.OpenScreenAsManageInventory(Settlement.CurrentSettlement.Stash);
      //InventoryManager.OpenScreenAsStash(Settlement.CurrentSettlement.Stash);
      return;
    }

    private bool hideout_management_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Manage;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout);
    }

    private void hideout_troops_consequence(MenuCallbackArgs args)
    {
      ref Hideout hideout = ref Settlement.CurrentSettlement.Hideout;

      //Allows putting troops in the settlement's party object. Appears to persist across saving/loading
      //can also see the prisoners you stick in the hideout here. that's fine
      PartyScreenAdditions.OpenPartyScreenAsManagePersistentParty(hideout.Settlement.Party);
      return;
    }

    private void hideout_prison_consequence(MenuCallbackArgs args)
    {
      PartyScreenManager.OpenScreenAsManagePrisoners();
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
