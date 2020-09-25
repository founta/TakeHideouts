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

    //mostly copy-and-pasted from DLLs
    private static void InitBanditParty(MobileParty banditParty, TextObject name, Clan faction, Settlement home)
    {
      banditParty.Name = name;
      banditParty.Party.Owner = faction.Leader;
      banditParty.Party.Visuals.SetMapIconAsDirty();
      banditParty.HomeSettlement = home;
      banditParty.ActualClan = faction;

      double totalStrength = (double)banditParty.Party.TotalStrength;
      int initialGold = (int)(10.0 * (double)banditParty.Party.MemberRoster.TotalManCount * (0.5 + 1.0 * (double)MBRandom.RandomFloat));
      banditParty.InitializePartyTrade(initialGold);

      foreach (ItemObject itemObject in ItemObject.All)
      {
        if (itemObject.IsFood)
        {
          int num = 16;
          int number = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
          if (number > 0)
            banditParty.ItemRoster.AddToCounts(itemObject, number);
        }
      }
    }

    //mostly copy-and-pasted from DLLs
    public static MobileParty CreateBanditInHideout(Hideout hideout, PartyTemplateObject pt, int partySizeLimitOverride)
    {
      BanditsCampaignBehavior behavior = Campaign.Current.GetCampaignBehavior<BanditsCampaignBehavior>();
      MobileParty mobileParty = (MobileParty)null;
      if (hideout.Owner.Settlement.Culture.IsBandit)
      {
        Clan faction = (Clan)null;
        foreach (Clan banditFaction in Clan.BanditFactions)
        {
          if (hideout.Owner.Settlement.Culture == banditFaction.Culture)
            faction = banditFaction;
        }
        mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(faction.StringId + "_1");
        TextObject name = faction.Name;
        mobileParty.InitializeMobileParty(name, pt, hideout.Owner.Settlement.Position2D, 0.0f, type: MobileParty.PartyTypeEnum.Bandit, troopNumberLimit: partySizeLimitOverride);
        mobileParty.IsBanditBossParty = false;
        Common.InitBanditParty(mobileParty, name, faction, hideout.Owner.Settlement);
        mobileParty.SetMoveGoToSettlement(hideout.Owner.Settlement);
        EnterSettlementAction.ApplyForParty(mobileParty, hideout.Owner.Settlement);
      }
      return mobileParty;
    }
  }
}
