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

  public class ClaimHideout_behavior : CampaignBehaviorBase
  {
    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      campaignGameStarter.AddGameMenuOption("hideout_place", "claim", "Claim Hideout", hideout_claim_access_condition, hideout_claim_consequence, true);

      campaignGameStarter.AddGameMenuOption("hideout_place", "stash", "Access Stash", hideout_stash_access_condition, hideout_stash_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "troops", "Manage Troops", hideout_management_access_condition, hideout_troops_consequence);
      campaignGameStarter.AddGameMenuOption("hideout_place", "prison", "Manage Prisoners", hideout_management_access_condition, hideout_prison_consequence);

      campaignGameStarter.AddGameMenuOption("hideout_place", "abandon", "Abandon Hideout", hideout_abandon_access_condition, hideout_abandon_consequence, true);
    }

    private bool hideout_claim_access_condition(MenuCallbackArgs args)
    {
      args.optionLeaveType = GameMenuOption.LeaveType.Recruit;

      return !Settlement.CurrentSettlement.Hideout.IsTaken; //can only claim it if it is not already taken
    }

    private void hideout_claim_consequence(MenuCallbackArgs args)
    {
      ref Hideout hideout = ref Settlement.CurrentSettlement.Hideout;

      //set our main hero as the new owener of the hideout (not needed)
      //hideout.Settlement.Party.Owner = Hero.MainHero;

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

          party.ActualClan = Hero.MainHero.Clan; //convert bandits in the hideout to our cause
          //party.Party.Owner = Hero.MainHero; //this makes it so that you can see them in the clan menu
          party.HomeSettlement = hideout.Settlement; //likely already set to this, doesn't seem to do anything

          //Tell everyone except the bandit boss to patrol around the hideout
          if (!party.IsBanditBossParty)
          {
            //party.SetMoveDefendSettlement(party.HomeSettlement);
            party.SetMovePatrolAroundSettlement(party.HomeSettlement);
          }

          //remove from war party list so that these bandits don't use up party slots
          ExposeInternals.RemoveWarPartyInternal(party.ActualClan, party);
        }

      }

      //re-open hideout menu
      //actually closes the game menu but I don't make the main hero leave the settlement so it just re-opens
      Campaign.Current.GameMenuManager.ExitToLast(); //re-opens hideout menu

      //found this in one of the DLLs should update map color?
      //doesnt really do anything
      //hideout.Settlement.Party.Visuals.SetMapIconAsDirty();

      //update hideout's appearance on map. Also gives a notification that you're the new owner
      ChangeOwnerOfSettlementAction.ApplyByBarter(Hero.MainHero, hideout.Settlement);

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
