using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using SandBox.ViewModelCollection.Map;
using TaleWorlds.Localization;
using TaleWorlds.Library;

using System.Xml;

using HarmonyLib;

namespace TakeHideouts
{

  public class TakeHideouts : MBSubModuleBase
  {
    internal static Harmony harmony = null;
    protected override void OnSubModuleLoad()
    {
      base.OnSubModuleLoad();
      Harmony.DEBUG = false;
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
      base.OnBeforeInitialModuleScreenSetAsRoot();
      
      //MBInformationManager.AddQuickInformation(new TextObject($"TakeHideouts {TakeHideoutsSettings.Instance.version}, for Bannerlord 1.9.0"));

      if (harmony == null)
      {
        harmony = new Harmony("TakeHideouts"); //TODO change this
        harmony.PatchAll();
      }
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
      if (!(game.GameType is Campaign))
        return;
      CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;

      gameInitializer.AddBehavior(new RecruitFromHideoutBehavior());
      gameInitializer.AddBehavior(new HideoutItemMemberManagementBehavior());
      gameInitializer.AddBehavior(new HideoutPatrolsBehavior());
      gameInitializer.AddBehavior(new HideoutOwnershipBehavior());
      gameInitializer.AddBehavior(new AiOwnedBanditBehavior());
      gameInitializer.AddBehavior(new WaitInHideoutBehavior());
      gameInitializer.AddBehavior(new HideoutRogueryBehavior());
    }
  }
}
