﻿using System;
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
  public class HideoutOwnershipBehavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_purchase", "Purchase Hideout", hideout_claim_access_condition, hideout_claim_consequence, true);
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_abandon", "Abandon Hideout", hideout_abandon_access_condition, hideout_abandon_consequence, true);
    }

    private bool hideout_claim_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
      
      //TODO find a better way to figure out who owns the hideout rather than
      //IsTaken. MapFaction won't work because IsTaken allows mapfaction to be set right in a harmony patch
      //Maybe set hideout.Settlement.Party.Owner to the main hero? Seems to be null

      //bool ours = Settlement.CurrentSettlement.Hideout.MapFaction == (IFaction) Hero.MainHero.Clan;
      return (!Settlement.CurrentSettlement.Hideout.IsTaken) && TakeHideoutsSettings.Instance.TakingHideoutsEnabled; //can only claim it if it is not already taken
    }

    private void hideout_claim_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      //compute cost of taking the hideout (base cost + cost to hire all inhabitants)
      int totalTroopCost = 0;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (party.IsBandit || party.IsBanditBossParty)
        {
          foreach (TroopRosterElement member in party.MemberRoster)
          {
            totalTroopCost += member.Number * member.Character.PrisonerRansomValue(Hero.MainHero);
          }
        }
      }
      int hideoutCost = (int) (5000 * TakeHideoutsSettings.Instance.HideoutCostMultiplier + totalTroopCost);

      //ceil(ish) to the nearest thousand
      hideoutCost = (int)(hideoutCost / 1000.0);
      hideoutCost *= 1000;
      hideoutCost += 1000;

      bool canPurchase = Hero.MainHero.Gold >= hideoutCost;

      string inquiryText = $"You ask the bandit leader how much it would cost to employ his camp's services. He considers the benefit of allying " +
        $"with {Hero.MainHero.Clan.Name.ToString()} and names his price -- {hideoutCost} Denars"; //TODO use the cool denar image thing
      if (!canPurchase)
        inquiryText += "\n\nYou cannot afford this hideout";

      string inquiryTitle = "Purchase Hideout";

      //set our main hero as the new owner of the hideout (not needed)
      //hideout.Settlement.Party.Owner = Hero.MainHero;

      Action inquiryAffirmative = () =>
      {
        Hero.MainHero.ChangeHeroGold(-hideoutCost); //TODO message like "you paid $(cost)"

        //This appears to default false for hideouts and doesn't appear to change anything
        //for the hideouts. hopefully changing it doesn't break anything
        //It looks like it is used for when towns or castles get taken. Should be ok to re-use for hideouts
        hideout.IsTaken = true;
        Common.playerHideoutListDirty = true;

        foreach (MobileParty party in hideout.Settlement.Parties)
        {
          if (party.IsBandit || party.IsBanditBossParty) //don't change the main party
          {
            //InformationManager.DisplayMessage(new InformationMessage($"Clan leader {(party.ActualClan.Leader == null ? "null" : "not null")}"));
            //InformationManager.DisplayMessage(new InformationMessage($"Leader {party.ActualClan.Leader.Name.ToString()}"));
            Common.SetAsOwnedHideoutParty(party, hideout);
          }

        }

        //re-open hideout menu
        //actually closes the game menu but I don't make the main party leave the settlement so it just re-opens
        Campaign.Current.GameMenuManager.ExitToLast(); //re-opens hideout menu

        //found this in one of the DLLs should update map color?
        //doesnt really do anything
        //hideout.Settlement.Party.Visuals.SetMapIconAsDirty();

        //update hideout's appearance on map. Also gives a notification that you're the new owner
        ChangeOwnerOfSettlementAction.ApplyByBarter(Hero.MainHero, hideout.Settlement);
      };

      //shows the option to buy the hideout
      InformationManager.ShowInquiry(new InquiryData(inquiryTitle, inquiryText, canPurchase, true, "Purchase", "Leave", inquiryAffirmative, null));

      return;
    }

    private bool hideout_abandon_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Escape;

      return Settlement.CurrentSettlement.Hideout.IsTaken;
    }

    private void hideout_abandon_consequence(MenuCallbackArgs args)
    {
      ref Hideout hideout = ref Settlement.CurrentSettlement.Hideout;
      hideout.IsTaken = false;
      Common.playerHideoutListDirty = true;

      //get correct original bandit clan
      Clan originalBanditClan = Clan.BanditFactions.FirstOrDefault<Clan>();
      foreach (Clan banditClan in Clan.BanditFactions)
      {
        if (banditClan.Culture == hideout.Settlement.Culture)
        {
          originalBanditClan = banditClan;
          break;
        }
      }

      //set each party clan back to the correct original bandit clan 
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        //don't set the main party's clan. Holy crap how did this not cause problems
        if (party != MobileParty.MainParty)
        {
          party.ActualClan = originalBanditClan;
        }
      }

      //remove ownership of hideout
      //by setting it to the hero of that bandit clan
      //hopefully doesn't blow anything up (doesn't appear to)
      ChangeOwnerOfSettlementAction.ApplyByDefault(originalBanditClan.Leader, hideout.Settlement);

      //re-opens hideout menu
      Campaign.Current.GameMenuManager.ExitToLast();
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
