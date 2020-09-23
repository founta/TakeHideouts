using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TakeHideouts
{
  class HideoutItemMemberManagementBehavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "stash", "Access Stash", hideout_stash_access_condition, hideout_stash_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "troops", "Manage Troops", hideout_management_access_condition, hideout_troops_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "prison", "Manage Prisoners", hideout_management_access_condition, hideout_prison_consequence);
    }

    private bool hideout_stash_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Trade;

      return Settlement.CurrentSettlement.Hideout.IsTaken;
    }

    private void hideout_stash_consequence(MenuCallbackArgs args)
    {
      InventoryManager.OpenScreenAsStash(Settlement.CurrentSettlement.Stash);
      return;
    }

    private bool hideout_management_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Manage;

      return Settlement.CurrentSettlement.Hideout.IsTaken;
    }

    private void hideout_troops_consequence(MenuCallbackArgs args)
    {
      ref Hideout hideout = ref Settlement.CurrentSettlement.Hideout;

      //Allows putting troops in the settlement's party object. Appears to persist across saving/loading
      //can also see the prisoners you stick in the hideout here. that's fine
      PartyScreenManager.OpenScreenAsLoot(hideout.Settlement.Party);
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
