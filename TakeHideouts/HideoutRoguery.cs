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
  class HideoutRogueryBehavior : CampaignBehaviorBase
  {

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
    }

    //give the main hero roguery exp for each owned hideout hourly
    public void HourlyTickSettlement(Settlement settlement)
    {
      if (!Common.IsOwnedHideout(settlement.Hideout))
        return;

      //~120 roguery exp a day per hideout
      float exp_amount = MBRandom.RandomInt(4, 6) * TakeHideoutsSettings.Instance.HideoutOwnershipExpMultipier;
      Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, exp_amount);
    }

    //give the hero roguery exp for taking a hideout
    public void OnSettlementOwnerChanged(Settlement settlement,
      bool openToClaim,
      Hero newOwner,
      Hero oldOwner,
      Hero capturerHero,
      ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
      if (!settlement.IsHideout)
        return;
      if (!Common.IsOwnedHideout(settlement.Hideout))
        return;

      //~1k roguery exp for taking a hideout
      float exp_gain = MBRandom.RandomInt(750, 1250) * TakeHideoutsSettings.Instance.TakeHideoutExpMultiplier;
      newOwner.AddSkillXp(DefaultSkills.Roguery, exp_gain);
    }

    public override void RegisterEvents()
    {
      CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener((object)this, new Action<Settlement>(this.HourlyTickSettlement));
      CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
    }
    public override void SyncData(IDataStore dataStore)
    {
    }
  }


}
