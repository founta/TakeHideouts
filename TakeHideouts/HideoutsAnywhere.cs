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
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Library;

using TaleWorlds.ObjectSystem;


using HarmonyLib;

namespace TakeHideouts
{
  class HideoutsAnywhere : CampaignBehaviorBase
  {
    public static XmlDocument save_companion = null;

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
      //      campaignGameStarter.AddWaitGameMenu("town_wait_menus", "{=ydbVysqv}You are waiting in {CURRENT_SETTLEMENT}.", new OnInitDelegate(this.game_menu_settlement_wait_on_init), new TaleWorlds.CampaignSystem.GameMenus.OnConditionDelegate(PlayerTownVisitCampaignBehavior.game_menu_town_wait_on_condition), (TaleWorlds.CampaignSystem.GameMenus.OnConsequenceDelegate)null, new OnTickDelegate(this.waiting_in_settlement_menu_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.SettlementWithBoth);
      //      campaignGameStarter.AddGameMenuOption("town_wait_menus", "wait_leave", "{=UqDNAZqM}Stop waiting", new GameMenuOption.OnConditionDelegate(PlayerTownVisitCampaignBehavior.game_menu_stop_waiting_at_town_on_condition), (GameMenuOption.OnConsequenceDelegate)(args => PlayerEncounter.Current.IsPlayerWaiting = false), true);
    }

    static private void InitSaveCompanion()
    {
      save_companion = new XmlDocument();
      XmlDeclaration xmlDeclaration = save_companion.CreateXmlDeclaration("1.0", "UTF-8", null);
      XmlElement root = save_companion.DocumentElement;
      save_companion.InsertBefore(xmlDeclaration, root);

      XmlElement xml_settlements = save_companion.CreateElement("", "Settlements", "");
      save_companion.AppendChild(xml_settlements);
    }
    static private XmlNode AddHideoutToSaveCompanion()
    {
      if (save_companion == null)
        return null;

      XmlNode xml_settlements = save_companion.LastChild;

      string new_hideout_id = "takehideouts_hideout_" + xml_settlements.ChildNodes.Count.ToString();

      XmlElement new_settlement = save_companion.CreateElement("", "Settlement", "");
      new_settlement.SetAttribute("id", new_hideout_id);
      new_settlement.SetAttribute("name", "Hideout");
      new_settlement.SetAttribute("type", "Hideout");
      new_settlement.SetAttribute("posX", (MobileParty.MainParty.Position2D.X + 3).ToString());
      new_settlement.SetAttribute("posY", (MobileParty.MainParty.Position2D.Y).ToString());
      new_settlement.SetAttribute("culture", "Culture.sea_raiders"); //TODO
      xml_settlements.AppendChild(new_settlement);

      XmlElement xml_components = save_companion.CreateElement("", "Components", "");
      new_settlement.AppendChild(xml_components);

      XmlElement xml_hideout_component = save_companion.CreateElement("", "Hideout", "");
      xml_hideout_component.SetAttribute("id", new_hideout_id);
      xml_hideout_component.SetAttribute("map_icon", "bandit_hideout_c");
      xml_hideout_component.SetAttribute("scene_name", "sea_bandit_a"); //TODO
      xml_hideout_component.SetAttribute("background_crop_position", "0.0");
      xml_hideout_component.SetAttribute("background_mesh", "empire_twn_scene_bg");
      xml_hideout_component.SetAttribute("wait_mesh", "wait_hideout_seaside"); //TODO
      xml_hideout_component.SetAttribute("gate_rotation", "0.0");
      xml_components.AppendChild(xml_hideout_component);

      return new_settlement;
    }

    static public void CreateHideout()
    {
      //if (save_companion == null)
       // InitSaveCompanion();

      //Call settlement ctor (make new settlement)
      //add component hideout
      //set hideout id wait mesh, background mesh, scene name, map icon?
      //set settlement culture, location, name, id
      //THEN make xml node
      /*XmlNode new_settlement = AddHideoutToSaveCompanion();
      if (new_settlement == null)
        return;
      InformationManager.DisplayMessage(new InformationMessage($"new created"));
      */
      Settlement settlement = Settlement.CurrentSettlement;

      InformationManager.DisplayMessage(new InformationMessage($"{settlement}"));

      if (settlement != null)
      {
        InformationManager.DisplayMessage(new InformationMessage($"{settlement.LocationComplex}"));

        if (settlement.LocationComplex != null)
        {
          foreach (Location location in settlement.LocationComplex.GetListOfLocations())
            InformationManager.DisplayMessage(new InformationMessage($"{location.StringId}"));
        }
      }



      //MBObjectManager.Instance.CreateObject<Settlement>()
      /*
      string id = "takehideouts_test_hideout_sos";

      Settlement settlement = new Settlement(new TextObject("Hideout"), new LocationComplex(), null);
      settlement.StringId = id;
      settlement.Culture = Clan.BanditFactions.FirstOrDefault<Clan>().Culture;
      ExposeInternals.SetSettlementPosition(settlement, new Vec2(MobileParty.MainParty.Position2D.X, MobileParty.MainParty.Position2D.Y + 2));

      

      Hideout hideout = settlement.AddComponent<Hideout>();
      ExposeInternals.SetHideoutBackgroundMeshName(hideout, "empire_twn_scene_bg");
      ExposeInternals.SetHideoutWaitMeshName(hideout, "wait_hideout_seaside");
      hideout.SetScene("sea_bandit_a");
      hideout.StringId = id;
      hideout.Settlement.IsVisible = true;

      ExposeInternals.InitializeTypes(Campaign.Current);

      settlement.OnGameInitialized();
      //settlement.OnGameCreated();
      settlement.OnSessionStart();

      InformationManager.DisplayMessage(new InformationMessage($"Created??"));
      */
      /*
      Settlement settlement = (Settlement)MBObjectManager.Instance.CreateObjectFromXmlNode(new_settlement);
      Hideout hideout = settlement.GetComponent<Hideout>();
      //Hideout hideout = settlement.Hideout; //this is null how to set?

      //should add hideout to Settlement.All (and hopefully init settlement.party; give it an _index)
      //nope how do you initialize settlement.Partys?? hard I guess because not meant to be done at runtime
      ExposeInternals.InitializeTypes(Campaign.Current);

      settlement.OnGameInitialized();
      settlement.Party.OnGameInitialized();

      InformationManager.DisplayMessage(new InformationMessage($"Party {settlement.Party}"));
      InformationManager.DisplayMessage(new InformationMessage($"Party {settlement.Party.IsSettlement}"));
      InformationManager.DisplayMessage(new InformationMessage($"Party {settlement.Party.Settlement}"));


      settlement.OnFinishLoadState();
      //ExposeInternals.OnFinishLoadState(settlement.Party);

      //initialize hideout
      //order?
      InformationManager.DisplayMessage(new InformationMessage($"Party {settlement.Party}"));
      InformationManager.DisplayMessage(new InformationMessage($"Party {settlement.Party.IsSettlement}"));
      //settlement.Party.OnGameInitialized();

      settlement.OnGameCreated();
      //settlement.OnGameInitialized();
      settlement.OnSessionStart();

      //ExposeInternals.OnLoad(hideout.Settlement, new TaleWorlds.SaveSystem.MetaData());

      //hideout.OnInit();
      settlement.IsVisible = true;

      //make visible on map??
      Campaign.Current.MapSceneWrapper.AddNewEntityToMapScene(settlement.StringId, settlement.Position2D);
      */
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
