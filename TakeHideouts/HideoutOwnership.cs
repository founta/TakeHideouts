using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
using StoryMode.StoryModeObjects;
using Helpers;

using HarmonyLib;

/* Things needed to make hideouts anywhere
 * 
 * CREATION:::
  <write new xml node>
  hideout = MBObjectManager.Instance.CreateObjectFromXmlNode(<XML save companion node>)
  ExposeInternals.InitializeTypes(Campaign.Current); //should add it to the settlement list

  //initialize hideout
  //order?
  hideout.Settlement.OnGameCreated()
  hideout.Settlement.OnGameInitialized()
  hideout.Settlement.OnSessionStart()

  hideout.OnInit()
  hideout.Settlement.IsVisible = true;
  ExposeInternals.OnLoad(hideout, new TaleWorlds.SaveSystem.MetaData());

  //make visible on map??
  Campaign.Current.MapSceneWrapper.AddNewEntityToMapScene(settlement.StringId, settlement.Position2D);

  ON GAME SAVE:::
  create XML save companion file containing all player-owned hideouts

  ON GAME LOAD:::
  load XML save companion file, too

  DESTRUCTION:::
  hideout.IsVisible = false //hide because it might stick around on the map after destruction
  hideout.OnSessionStart() //set map visual dirty
  // destroy all parties in hideout //
  MBObjectManager.Instance.UnregisterObject(hideout); //should make it so it isn't saved anymore
  ExposeInternals.InitializeTypes(Campaign.Current); //remove from settlements.all
  (remove from XML save companion)

 * 
 */

namespace TakeHideouts
{
  public class HideoutOwnershipBehavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_purchase", "Purchase Hideout", hideout_claim_access_condition, hideout_claim_consequence, true);
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_abandon", "Abandon Hideout", hideout_abandon_access_condition, hideout_abandon_consequence, true);
      //campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_test_create", "Create Hideout", hideout_abandon_access_condition, (x=> HideoutsAnywhere.CreateHideout()));

      //take hideout by force
      campaignGameStarter.AddGameMenuOption("hideout_place", "takehideouts_take_hideout", "Take hideout by force", new GameMenuOption.OnConditionDelegate(this.take_hideout_condition), new GameMenuOption.OnConsequenceDelegate(this.take_hideout_consequence));
      campaignGameStarter.AddGameMenuOption("hideout_after_wait", "takehideouts_take_hideout", "Take hideout by force", new GameMenuOption.OnConditionDelegate(this.take_hideout_condition), new GameMenuOption.OnConsequenceDelegate(this.take_hideout_consequence));
    }

    private bool take_hideout_condition(MenuCallbackArgs args)
    {
      HideoutCampaignBehavior bhv = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();
      return ExposeInternals.AttackHideoutCondition(bhv, args) && CanTakeHideout();
    }

    private bool CanChangeStatusOfTroop(CharacterObject character)
    {
      HideoutCampaignBehavior bhv = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();
      return ExposeInternals.CanChangeStatusOfTroop(bhv, character);
    }

    private void OnTroopRosterManageDone(TroopRoster hideoutTroops)
    {
      HideoutCampaignBehavior bhv = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();
      Settlement.CurrentSettlement.Hideout.IsTaken = true; //mark the hideout for taking if we win
      ExposeInternals.OnTroopRosterManageDone(bhv, hideoutTroops);
    }

    private void take_hideout_consequence(MenuCallbackArgs args)
    {
      HideoutCampaignBehavior bhv = Campaign.Current.GetCampaignBehavior<HideoutCampaignBehavior>();

      //copy and pasted from decompiled DLL, mostly
      //does the same thing as normal attack hideout but with modified ontrooprostermanagedone
      int forHideoutMission = Campaign.Current.Models.BanditDensityModel.GetPlayerMaximumTroopCountForHideoutMission(MobileParty.MainParty);
      TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
      FlattenedTroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, forHideoutMission, true);
      dummyTroopRoster.Add((IEnumerable<FlattenedTroopRosterElement>)strongestAndPriorTroops);
      args.MenuContext.OpenManageHideoutTroops(dummyTroopRoster, new Func<CharacterObject, bool>(this.CanChangeStatusOfTroop), new Action<TroopRoster>(this.OnTroopRosterManageDone));
    }

    private bool CanTakeHideout()
    {
      if (Settlement.CurrentSettlement == null)
        return false;

      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      if (hideout == null)
        return false;

      Settlement settlement = hideout.Settlement;

      bool isStoryHideout = false;
      if (StoryMode.StoryMode.Current != null)
        isStoryHideout = StoryMode.StoryMode.Current.MainStoryLine.BusyHideouts.Contains(hideout);


      bool canTake = !Common.IsOwnedHideout(hideout);
      if (isStoryHideout || !TakeHideoutsSettings.Instance.TakingHideoutsEnabled)
        canTake = false;

      return canTake;
    }

    private bool hideout_claim_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
      
      //TODO find a better way to figure out who owns the hideout rather than
      //IsTaken. MapFaction won't work because IsTaken allows mapfaction to be set right in a harmony patch
      //Maybe set hideout.Settlement.Party.Owner to the main hero? Seems to be null

      //bool ours = Settlement.CurrentSettlement.Hideout.MapFaction == (IFaction) Hero.MainHero.Clan;
      return CanTakeHideout(); //can only claim it if it is not already taken
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
          foreach (TroopRosterElement member in party.MemberRoster.GetTroopRoster())
          {
            totalTroopCost += member.Number * Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(member.Character, Hero.MainHero);
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
        $"with {Hero.MainHero.Clan.Name.ToString()} and names his price -- {hideoutCost}" + "{GOLD_ICON}"; //TODO use the cool denar image thing
      if (!canPurchase)
        inquiryText += "\n\nYou cannot afford this hideout";

      TextObject inquiryTextObject = new TextObject(inquiryText);
      inquiryTextObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"Icons\\Coin@2x\">");

      string inquiryTitle = "Purchase Hideout";

      //set our main hero as the new owner of the hideout (not needed)
      //hideout.Settlement.Party.Owner = Hero.MainHero;

      Action inquiryAffirmative = () =>
      {
        //xml_test.Save("test.xml");


        Hero.MainHero.ChangeHeroGold(-hideoutCost); //TODO message like "you paid $(cost)"
        Common.SetAsOwnedHideout(hideout, true);

        //re-open hideout menu
        //actually closes the game menu but I don't make the main party leave the settlement so it just re-opens
        Campaign.Current.GameMenuManager.ExitToLast(); //re-opens hideout menu

        //found this in one of the DLLs should update map color?
        //doesnt really do anything
        //hideout.Settlement.Party.Visuals.SetMapIconAsDirty();

      };

      //shows the option to buy the hideout
      InformationManager.ShowInquiry(new InquiryData(inquiryTitle, inquiryTextObject.ToString(), canPurchase, true, "Purchase", "Leave", inquiryAffirmative, null));
      return;
    }

    private bool hideout_abandon_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Escape;

      return Common.IsOwnedHideout(Settlement.CurrentSettlement.Hideout);
    }

    private void hideout_abandon_consequence(MenuCallbackArgs args)
    {

      string inquiryText = $"You prepare to part ways with your hideout.\n"
        + "Are you sure you want to abandon the hideout? Ownership will revert back to the original bandit owners.";
      string inquiryTitle = "Abandon Hideout";


      Action inquiryAffirmative = () =>
      {
        ref Hideout hideout = ref Settlement.CurrentSettlement.Hideout;
        hideout.IsTaken = false;
        hideout.Settlement.SettlementTaken = false;
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
            //party.Party.Owner = null;
          }
        }

        //re-opens hideout menu
        Campaign.Current.GameMenuManager.ExitToLast();

        //remove ownership of hideout
        //by setting it to the hero of that bandit clan
        //hopefully doesn't blow anything up (doesn't appear to)
        ChangeOwnerOfSettlementAction.ApplyByDefault(originalBanditClan.Leader, hideout.Settlement);
      };

      //shows the confirmation option to abandon hideout
      InformationManager.ShowInquiry(new InquiryData(inquiryTitle, inquiryText, true, true, "Abandon Hideout", "Cancel", inquiryAffirmative, null));

      return;
    }

    static public Hideout hideoutToTake = null;

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
