using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using SandBox.View.Map;

using HarmonyLib;

namespace TakeHideouts
{
  class Common
  {
    public static void SetAsOwnedHideoutParty(MobileParty party, Hideout hideout)
    {
      party.ActualClan = Hero.MainHero.Clan; //convert bandits in the hideout to our cause (is this the right way to do this?)
      party.Party.SetCustomOwner(Hero.MainHero); //this makes it so that you can see them in the clan menu, and also so that you can get access to them later.
      party.SetCustomHomeSettlement(hideout.Settlement); //likely already set to this, doesn't seem to do anything

      party.SetMoveGoToSettlement(party.HomeSettlement); //don't disperse when taking hideout

      //remove from player's war party list so that these bandits don't use up party slots
      //is there an actual way to do this (that's not an internal method)?? Users report
      //that bandit groups still use up slots sometimes but the issue is fixed on 
      //abandoning/re-claiming hideout.
      if (Hero.MainHero.Clan.WarPartyComponents.Contains(party.WarPartyComponent))
        ExposeInternals.OnWarPartyRemoved(Hero.MainHero.Clan, party.WarPartyComponent);
      //Hero.MainHero.Clan.RemovePartyInternal(party.Party);
    }

    public static void SetAsOwnedHideout(Hideout hideout, bool barter=true)
    {
      //re-initialize the owned hideout list when next requested
      Common.playerHideoutListDirty = true;
      //InformationManager.DisplayMessage(new InformationMessage($"{hideout.Settlement.Parties.Count} parties"));
      List<MobileParty> parties_to_replace = new List<MobileParty>();
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (party.IsBanditBossParty || party.IsBandit) //convert the bandit leader
        {
          //InformationManager.DisplayMessage(new InformationMessage($"Clan leader {(party.ActualClan.Leader == null ? "null" : "not null")}"));
          //InformationManager.DisplayMessage(new InformationMessage($"Leader {party.ActualClan.Leader.Name.ToString()}"));
          Common.SetAsOwnedHideoutParty(party, hideout);
        }
        else
        {
         // InformationManager.DisplayMessage(new InformationMessage($"isbandit {party.IsBandit} isownedbandit {IsOwnedBanditParty(party)}"));
        }
      }

      /*List<MobileParty> parties_to_remove = new List<MobileParty>();
      foreach (MobileParty party in parties_to_replace)
      {
        MobileParty new_party = Common.CreateOwnedBanditPartyInHideout(hideout, 60);
        new_party.MemberRoster.Clear();
        foreach (TroopRosterElement member in party.MemberRoster.GetTroopRoster())
        {
          new_party.AddElementToMemberRoster(member.Character, member.Number);
        }
        parties_to_remove.Add(party);
        InformationManager.DisplayMessage(new InformationMessage($"Added new party and transferred troops"));
      }
      foreach (MobileParty party in parties_to_remove)
      {
        InformationManager.DisplayMessage(new InformationMessage($"Removed party"));
        party.RemoveParty();
      }*/

      //This appears to default false for hideouts and doesn't appear to change anything
      //for the hideouts. hopefully changing it doesn't break anything
      //It looks like it is used for when towns or castles get taken. Should be ok to re-use for hideouts
      //hideout.Settlement.SettlementTaken = true;

      if (barter)
      {
        //update hideout's appearance on map. Also gives a notification that you're the new owner
        ChangeOwnerOfSettlementAction.ApplyByBarter(Hero.MainHero, hideout.Settlement);
      }
      else
      {
        ChangeOwnerOfSettlementAction.ApplyByDefault(Hero.MainHero, hideout.Settlement);
      }
    }

    public static bool IsOwnedBanditParty(MobileParty party)
    {
      if (party == null)
        return false;
      if (party.ActualClan == null)
        return false;

      //catches corrupted bandit patrols
      if (party.BanditPartyComponent != null) //then it's a bandit party (we patch IsBandit so we have to use this)
        if (party.ActualClan == Hero.MainHero.Clan)
          return true;
      /*
      Settlement home = party.HomeSettlement;
      if (home == null)
        return false;

      //we know if it's our bandit party based on its home settlement
      //could also look in main party clan's party list, but this is easier
      if (home.IsHideout())
        return IsOwnedHideout(home.Hideout);
      */
      return false;
    }

    public static bool IsOwnedHideout(Hideout hideout)
    {
      if (hideout == null)
        return false;
      if (hideout.Settlement == null)
        return false;
      if (hideout.Settlement.Parties.Count == 0)
        return false;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if ( Common.IsBanditBossParty(party) && (party.ActualClan == Hero.MainHero.Clan))
        {
          return true;
        }
      }
      return false;
    }

    public static void GivePartyGrain(MobileParty party, int howMuch)
    {
      party.ItemRoster.AddToCounts(DefaultItems.Grain, howMuch);
    }

    public static int GetFoodCount(ItemRoster items)
    {
      int foodCount = 0;
      for (int i = 0; i < items.Count; ++i)
      {
        ItemObject item = items.GetItemAtIndex(i);
        if (item.IsFood)
          foodCount += items[i].Amount;
      }

      return foodCount;
    }

    public static int GetCheapestFoodIdx(ItemRoster items)
    {
      int cheapestFoodIdx = -1;
      int cheapestValue = -1;
      if (items.Count > 0)
      {
        for (int i = 0; i < items.Count; ++i)
        {
          ItemObject item = items.GetItemAtIndex(i);
          if (item.IsFood)
          {
            if (cheapestFoodIdx == -1)
            {
              cheapestFoodIdx = i;
              cheapestValue = item.Value;
            }
            else
            {
              if (item.Value < cheapestValue)
              {
                cheapestValue = item.Value;
                cheapestFoodIdx = i;
              }
            } //end if..else
          } //end if IsFood 
        } //end for
      } //end item count if
      return cheapestFoodIdx;
    }

    private static List<Hideout> playerHideouts = null;
    public static bool playerHideoutListDirty = true;
    public static List<Hideout> GetPlayerOwnedHideouts()
    {
      //cache list of player hideouts so we don't have to 
      //loop through all settlements each time
      if (playerHideoutListDirty)
      {
        playerHideouts = new List<Hideout>();
        foreach (Settlement s in Settlement.All)
          if (s.IsHideout)
            if (Common.IsOwnedHideout(s.Hideout))
              playerHideouts.Add(s.Hideout);
        playerHideoutListDirty = false;
      }

      return playerHideouts;
    }

    public static MobileParty CreateOwnedBanditPartyInHideout(Hideout hideout, int initialGold = 300, bool isBoss=false)
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
      MobileParty banditParty = BanditPartyComponent.CreateBanditParty("takehideouts_party", banditClan, hideout, isBoss); //MBObjectManager.Instance.CreateObject<MobileParty>();
      banditParty.InitializeMobilePartyAtPosition(hideout.Settlement.Culture.BanditBossPartyTemplate,
        hideout.Settlement.Position2D); //name? id?

      banditParty.InitializePartyTrade(initialGold);
//      Common.GivePartyGrain(banditParty, 20);

      Common.SetAsOwnedHideoutParty(banditParty, hideout);
      banditParty.Party.Visuals.SetMapIconAsDirty();

      banditParty.SetMoveGoToSettlement(hideout.Settlement);
      EnterSettlementAction.ApplyForParty(banditParty, hideout.Settlement);

      if (isBoss)
        banditParty.Ai.DisableAi();

      return banditParty;
    }

    public static void OpenSingleSelectInquiry(string headerText, List<InquiryElement> elements, string affirmativeLabel, Action<List<InquiryElement>> affirmativeAction)
    {
      MBInformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData(headerText, "", elements, true, 1,
        affirmativeLabel, "Leave",
        affirmativeAction, Common.inquiry_do_nothing));
    }

    public static bool IsBanditBossParty(MobileParty party)
    {
      if (party.BanditPartyComponent == null)
        return false;
      return party.BanditPartyComponent.IsBossParty;
    }

    public static List<InquiryElement> GetHideoutPartyInquiryElements(Hideout hideout, bool showFood = false)
    {
      List<InquiryElement> elements = new List<InquiryElement>();

      int banditPartyCounter = 1;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (!Common.IsBanditBossParty(party) && (party != MobileParty.MainParty))
        {
          //don't show parties with no troops, because those can exist apparently
          if (party.Party.MemberRoster.Count <= 0)
            continue;

          string foodAdd = "";
          if (showFood)
            foodAdd = $", {party.TotalFoodAtInventory} food";

          //TODO show random troop icon instead of first one?
          elements.Add(
            new InquiryElement(
              (object)party,
              $"Bandit party {banditPartyCounter++} ({party.Party.MemberRoster.TotalManCount} troops" + foodAdd + ")",
              new ImageIdentifier(CharacterCode.CreateFrom(party.Party.MemberRoster.GetCharacterAtIndex(0))) //show sweet image of first troop
              )
            );
        }
      }

      return elements;
    }

    public static void RemoveEmptyParties(List<MobileParty> parties)
    {
      if (parties != null)
      {
        List<int> removalIdxs = new List<int>();
        for (int i = 0; i < parties.Count; ++i)
        {
          MobileParty party = parties[i];
          if (party.MemberRoster.Count == 0)
            removalIdxs.Add(i);
        }

        removalIdxs.Reverse();
        for (int i = 0; i < removalIdxs.Count; ++i)
        {
          parties[removalIdxs[i]].RemoveParty();
        }
      }
    }

    public static void inquiry_do_nothing(List<InquiryElement> elements)
    {
      return;
    }

  }
}


