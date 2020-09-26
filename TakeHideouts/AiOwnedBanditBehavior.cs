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
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;

using HarmonyLib;

namespace TakeHideouts
{
  class AiOwnedBanditBehavior : CampaignBehaviorBase
  {
    public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
    {
      //exclude all parties, other than owned bandit patrols
      if (mobileParty.HomeSettlement == null)
        return;
      if (!mobileParty.HomeSettlement.IsHideout())
        return;
      if (mobileParty.IsVillager || mobileParty.IsMilitia || mobileParty.IsBandit || mobileParty.IsBanditBossParty || mobileParty.IsCaravan || mobileParty.IsGarrison)
        return;

      Hideout hideout = mobileParty.HomeSettlement.Hideout;
      if (!hideout.IsTaken) //parties with taken home hideout == owned hideout bandit patrols
        return;

      AIBehaviorTuple patrolKey = new AIBehaviorTuple((IMapPoint)mobileParty.HomeSettlement, AiBehavior.PatrolAroundPoint);
      AIBehaviorTuple returnToHideoutKey = new AIBehaviorTuple((IMapPoint)mobileParty.HomeSettlement, AiBehavior.GoToSettlement);

      float returnToHideoutScore = 10;
      float patrolScore = 10;

      if (mobileParty.DefaultBehavior == AiBehavior.GoToSettlement) //when we recall bandit parties
        returnToHideoutScore += 50;
      if (mobileParty.DefaultBehavior == AiBehavior.PatrolAroundPoint) //when we have the patrols dispatched
      {
        patrolScore += 50;

        //want to return to hideout if low on food, keep patrolling if fine on food
        //TODO add some random element?

        //check food
        int foodDays = mobileParty.GetNumDaysForFoodToLast();

        float foodImportance = (float)Math.Exp(-0.25 * foodDays);

        returnToHideoutScore *= foodImportance;
        patrolScore *= (1 - foodImportance);
      }

      //replace keys
      if (p.AIBehaviorScores.ContainsKey(patrolKey))
        p.AIBehaviorScores.Remove(patrolKey);
      if (p.AIBehaviorScores.ContainsKey(returnToHideoutKey))
        p.AIBehaviorScores.Remove(returnToHideoutKey);

      p.AIBehaviorScores.Add(patrolKey, patrolScore);
      p.AIBehaviorScores.Add(returnToHideoutKey, returnToHideoutScore);
    }

    public override void RegisterEvents()
    {
      CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener((object)this, new Action<MobileParty, PartyThinkParams>(this.AiHourlyTick));
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
  }


}
