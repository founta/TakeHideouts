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
using TaleWorlds.ObjectSystem;
using TaleWorlds.Localization;

using HarmonyLib;

namespace TakeHideouts
{
  class Common
  {
    public static void SetAsOwnedHideoutParty(MobileParty party, Hideout hideout)
    {
      party.ActualClan = Hero.MainHero.Clan; //convert bandits in the hideout to our cause (is this the right way to do this?)
      party.Party.Owner = Hero.MainHero; //this makes it so that you can see them in the clan menu, and also so that you can get access to them later..
      party.HomeSettlement = hideout.Settlement; //likely already set to this, doesn't seem to do anything

      //remove from player's war party list so that these bandits don't use up party slots
      //is there an actual way to do this (that's not an internal method)?? Users report
      //that bandit groups still use up slots sometimes but the issue is fixed on 
      //abandoning/re-claiming hideout.
      ExposeInternals.RemoveWarPartyInternal(Hero.MainHero.Clan, party);
    }

    public static void GivePartyGrain(MobileParty party, int howMuch)
    {
      /*
      foreach (ItemObject obj in ItemObject.All)
      {
        if (obj.IsFood)
        {
          InformationManager.DisplayMessage(new InformationMessage($"{obj.Name} {obj.Name.GetID()} {obj.Id}"));
          //MBObjectManager.Instance.GetObject<ItemObject>("Grain");
        }
      }
      */
      party.ItemRoster.AddToCounts(DefaultItems.Grain, howMuch);
    }

    public static MobileParty CreateOwnedBanditPartyInHideout(Hideout hideout, int initialGold = 300)
    {
      Clan banditClan = null;
      foreach (Clan clan in Clan.BanditFactions)
      {
        if (hideout.Settlement.Culture == clan.Culture)
        {
          banditClan = clan;
          break;
        }
      }
      MobileParty banditParty = MBObjectManager.Instance.CreateObject<MobileParty>();
      banditParty.InitializeMobileParty(banditClan.Name, hideout.Settlement.Culture.BanditBossPartyTemplate,
        hideout.Settlement.Position2D, 0.0f, 0.0f);

      banditParty.InitializePartyTrade(initialGold);
      Common.GivePartyGrain(banditParty, 40);

      Common.SetAsOwnedHideoutParty(banditParty, hideout);
      banditParty.Party.Visuals.SetMapIconAsDirty();

      banditParty.SetMoveGoToSettlement(hideout.Settlement);
      EnterSettlementAction.ApplyForParty(banditParty, hideout.Settlement);
      return banditParty;
    }

  }
}
