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
  class WaitInHideout : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
//      campaignGameStarter.AddWaitGameMenu("town_wait_menus", "{=ydbVysqv}You are waiting in {CURRENT_SETTLEMENT}.", new OnInitDelegate(this.game_menu_settlement_wait_on_init), new TaleWorlds.CampaignSystem.GameMenus.OnConditionDelegate(PlayerTownVisitCampaignBehavior.game_menu_town_wait_on_condition), (TaleWorlds.CampaignSystem.GameMenus.OnConsequenceDelegate)null, new OnTickDelegate(this.waiting_in_settlement_menu_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.SettlementWithBoth);
//      campaignGameStarter.AddGameMenuOption("town_wait_menus", "wait_leave", "{=UqDNAZqM}Stop waiting", new GameMenuOption.OnConditionDelegate(PlayerTownVisitCampaignBehavior.game_menu_stop_waiting_at_town_on_condition), (GameMenuOption.OnConsequenceDelegate)(args => PlayerEncounter.Current.IsPlayerWaiting = false), true);
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
