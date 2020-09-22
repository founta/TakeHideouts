using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.AiBehaviors;
using SandBox.View.Map;


using System.Xml;

using HarmonyLib;

namespace TakeHideouts
{
  //patch Hideout Mapfaction getter to correctly set hideout's mapfaction
  //after we take the hideout
  [HarmonyPatch(typeof(Hideout), "MapFaction")]
  [HarmonyPatch(MethodType.Getter)]
  public class TakeHideoutsHideoutPatch
  {
    static void Postfix(Hideout __instance, ref IFaction __result)
    {
      if (__instance.IsTaken) //if we've taken the hideout, ignore the ifBandit check
      {
        bool factionSet = false;
        foreach (MobileParty party in __instance.Settlement.Parties)
        {
          __result = (IFaction)party.ActualClan;
          factionSet = true;
          break;
        }
        if (!factionSet)
        {
          __result = (IFaction)Clan.BanditFactions.FirstOrDefault<Clan>(); //sets it to some default bandit clan if no parties
        }
      }
    }
  }

  //patch 'Wait until nightfall' on_condition to disable attacking the hideout
  //if we own the hideout
  [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_wait_until_nightfall_on_condition")]
  public class TakeHideoutsHideoutBehaviorPatch
  {
    static void Postfix(HideoutCampaignBehavior __instance, ref bool __result)
    {
      //if we've taken the hideout and we can attack it, disable attacking it
      if (__result && Settlement.CurrentSettlement.IsHideout())
      {
        if (Settlement.CurrentSettlement.Hideout.IsTaken)
        {
          __result = false;
        }
      }
    }
  }

  [HarmonyPatch]
  public class ExposeInternals
  {

    //expose internal function Clan.RemoveWarPartyInternal
    //this shouldn't cause any problems to use
    //(war parties is only to calculate clan strength and take up
    // clan party limit, I think)
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Clan), "RemoveWarPartyInternal")]
    public static void RemoveWarPartyInternal(Clan instance, MobileParty warparty)
    {
      return;
    }
    /*
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PartyAi), "SetDefaultBehavior")]
    public static void SetDefaultBehavior(PartyAi instance, AiBehavior newAiState, PartyBase targetParty)
    {
      return;
    }
    */
  }

  public class TakeHideoutsMain : MBSubModuleBase
  {
    private static Harmony harmony = null;
    protected override void OnSubModuleLoad()
    {
      base.OnSubModuleLoad();
      Harmony.DEBUG = false;
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
      base.OnBeforeInitialModuleScreenSetAsRoot();

      //read version from xml
      XmlReader reader = XmlReader.Create("../../Modules/TakeHideouts/SubModule.xml"); //easier way to get at version?
      reader.ReadToFollowing("Version");
      reader.MoveToFirstAttribute();
      string version = reader.Value;
      
      InformationManager.DisplayMessage(new InformationMessage($"TakeHideouts {version}, tested for Bannerlord 1.5.1"));

      if (harmony == null)
      {
        harmony = new Harmony("TakeHideouts");
        harmony.PatchAll();
      }
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
      if (!(game.GameType is Campaign))
        return;
      CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;

      gameInitializer.AddBehavior(new ClaimHideout_behavior());
    }
  }

  //TODO break this out into more logical, separate classes
  public class ClaimHideout_behavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "recruit", "Recruit Troops", hideout_recruit_access_condition, hideout_recruit_consequence);

      campaignGameStarter.AddGameMenuOption("hideout_place", "claim", "Claim Hideout", hideout_claim_access_condition, hideout_claim_consequence, true);

      campaignGameStarter.AddGameMenuOption("hideout_place", "stash", "Access Stash", hideout_stash_access_condition, hideout_stash_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "troops", "Manage Troops", hideout_management_access_condition, hideout_troops_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "prison", "Manage Prisoners", hideout_management_access_condition, hideout_prison_consequence);

      campaignGameStarter.AddGameMenuOption("hideout_place", "abandon", "Abandon Hideout", hideout_abandon_access_condition, hideout_abandon_consequence, true);
    }

    private bool hideout_recruit_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Recruit;

      return true;
    }

    private void hideout_recruit_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;
      List<InquiryElement> elements = new List<InquiryElement>();

      int banditPartyCounter = 1;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (!party.IsBanditBossParty && (party != MobileParty.MainParty))
        {
          elements.Add(
            new InquiryElement(
              (object)party,
              $"Bandit party {banditPartyCounter++}",
              new ImageIdentifier(CharacterCode.CreateFrom(party.Party.MemberRoster.ElementAt(0).Character)) //show sweet image of first troop
              )
            );
        }
      }

      InformationManager.ShowMultiSelectionInquiry(
        new MultiSelectionInquiryData("Choose bandit party to recruit from", "", elements, true, 1, 
                                      "Select Troops", "Leave", this.inquiry_recruit_troops, this.inquiry_do_nothing));

      return;
    }

    private bool hideout_claim_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;

      return !Settlement.CurrentSettlement.Hideout.IsTaken; //can only claim it if it is not already taken
    }

    private void hideout_claim_consequence(MenuCallbackArgs args)
    {
      Hideout hideout = Settlement.CurrentSettlement.Hideout;

      //compute cost of taking the hideout (one month's worth of the inhabitants' wages)
      //TODO make this configurable. Modlib or something
      int totalWages = 0;
      foreach (MobileParty party in hideout.Settlement.Parties)
      {
        if (party.IsBandit || party.IsBanditBossParty)
          totalWages += party.GetTotalWage();
      }
      int hideoutCost = 30 * totalWages + 1000; //min cost is 1000

      //truncate cost to the nearest thousand
      // -= hideoutCost % 1000 would be better maybe but who cares
      hideoutCost = (int) (hideoutCost / 1000);
      hideoutCost *= 1000;

      bool canPurchase = Hero.MainHero.Gold >= hideoutCost;

      string inquiryText = $"You ask the bandit leader how much it would cost to employ his camp's services. He considers the benefit of allying " +
        $"with {Hero.MainHero.Clan.Name.ToString()} and names his price -- {hideoutCost} Denars"; //TODO use the cool denar image thing
      if (!canPurchase)
        inquiryText += "\n\nYou cannot afford this hideout";

      string inquiryTitle = "Purchase Hideout";

      //set our main hero as the new owener of the hideout (not needed)
      //hideout.Settlement.Party.Owner = Hero.MainHero;

      Action inquiryAffirmative = () => 
      {
        Hero.MainHero.ChangeHeroGold(-hideoutCost); //TODO message like "you paid $(cost)"

        //This appears to default false for hideouts and doesn't appear to change anything
        //for the hideouts. hopefully changing it doesn't break anything
        //It looks like it is used for when towns or castles get taken. Should be ok to re-use for hideouts
        hideout.IsTaken = true;

        foreach (MobileParty party in hideout.Settlement.Parties)
        {
          if (party.IsBandit) //don't change the main party
          {
            //InformationManager.DisplayMessage(new InformationMessage($"Clan leader {(party.ActualClan.Leader == null ? "null" : "not null")}"));
            //InformationManager.DisplayMessage(new InformationMessage($"Leader {party.ActualClan.Leader.Name.ToString()}"));

            party.ActualClan = Hero.MainHero.Clan; //convert bandits in the hideout to our cause (is this the right way to do this?)
            //party.Party.Owner = Hero.MainHero; //this makes it so that you can see them in the clan menu
            party.HomeSettlement = hideout.Settlement; //likely already set to this, doesn't seem to do anything

            //Tell everyone except the bandit boss to patrol around the hideout
            if (!party.IsBanditBossParty)
            {
              //party.SetMoveDefendSettlement(party.HomeSettlement);
              party.SetMovePatrolAroundSettlement(party.HomeSettlement);
            }

            //remove from player's war party list so that these bandits don't use up party slots
            //is there an actual way to do this (that's not an internal method)?? Users report
            //that bandit groups still use up slots sometimes but the issue is fixed on 
            //abandoning/re-claiming hideout.
            ExposeInternals.RemoveWarPartyInternal(party.ActualClan, party);
          }

        }

        //re-open hideout menu
        //actually closes the game menu but I don't make the main party leave the settlement so it just re-opens
        Campaign.Current.GameMenuManager.ExitToLast(); //leaves inquiry?
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

    private void inquiry_recruit_troops(List<InquiryElement> party_list)
    {
      if (party_list.Count == 0)
        return;
      MobileParty party = (MobileParty)party_list[0].Identifier;

      //get cost of party before stripping by player
      //int party_wages = party.GetTotalWage();

      //PartyScreenMode.Loot;
      //PartyScreenLogic.PartyRosterSide
      //PartyScreenManager.OpenScreenWithCondition(); //TODO figure out what this is? Won't work, uses dummy roster as target
      this.OpenPartyScreenAsBuyTroops(party.Party);
      //PartyScreenManager.OpenScreenAsLoot(party.Party);
      //get cost of party afterwards?? if it still exists... hopefully works, we'll see
      //int new_wages = party.GetTotalWage();

      //charge the player. just 5 days of troop wages, probably balanced. TODO configure
      //uhh TODO check if player has money for this. for now just kind of assume they do
      //what if they dont? how do I put the troops back...?
      //int player_cost = (party_wages - new_wages) * 5;
      //Hero.MainHero.ChangeHeroGold(-player_cost);



      //charge player in the recruit troops consequence?
      //TODO tell player how much these suckers are going to cost?
    }

      private void inquiry_do_nothing(List<InquiryElement> elements)
    {
      return;
    }

    //This is horrifying
    private void OpenPartyScreenAsBuyTroops(PartyBase partyToBuyFrom)
    {
      //get access to private _currentMode and _partyScreenLogic from PartyScreenManager
      ref PartyScreenMode currentMode = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenMode>(PartyScreenManager.Instance, "_currentMode");
      ref PartyScreenLogic partyScreenLogic = ref AccessTools.FieldRefAccess<PartyScreenManager, PartyScreenLogic>(PartyScreenManager.Instance, "_partyScreenLogic");

      currentMode = PartyScreenMode.Ransom; //please enable troop buying and selling. Yeah it doesn't. Neither does transferable with trade. Figure out later.
      partyScreenLogic = new PartyScreenLogic();
      partyScreenLogic.Initialize(partyToBuyFrom, MobileParty.MainParty, new TaleWorlds.Localization.TextObject("Recruit Bandits"));
      partyScreenLogic.InitializeTrade(PartyScreenLogic.TransferState.TransferableWithTrade, PartyScreenLogic.TransferState.NotTransferable, PartyScreenLogic.TransferState.NotTransferable);
      partyScreenLogic.SetTroopTransferableDelegate(new PartyScreenLogic.IsTroopTransferableDelegate(PartyScreenManager.TroopTransferableDelegate));
      partyScreenLogic.SetDoneHandler(new PartyPresentationDoneButtonDelegate(this.recruitDoneHandler));
      partyScreenLogic.Parties[0].Add(partyToBuyFrom.MobileParty); //this is really all I needed to do?
      PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
      state.InitializeLogic(partyScreenLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }

    //kill the party if no more troops (if buy troops screen doesn't...)
    //probably doesn't, since it takes a PartyBase
    private bool recruitDoneHandler(
      TroopRoster leftMemberRoster,
      TroopRoster leftPrisonRoster,
      TroopRoster rightMemberRoster,
      TroopRoster rightPrisonRoster,
      bool isForced,
      List<MobileParty> leftParties = null,
      List<MobileParty> rigthParties = null)
    {
      //assuming only one left party
      if (leftParties != null)
      {
        if (leftParties.Count != 0)
        {
          if (leftParties[0].MemberRoster.Count == 0)
            leftParties[0].RemoveParty();
        }
        else
        {
          InformationManager.DisplayMessage(new InformationMessage($"left party count zero. whyyy"));
        }
      }
      return true;
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
