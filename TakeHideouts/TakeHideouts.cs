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
      
      InformationManager.DisplayMessage(new InformationMessage($"TakeHideouts {TakeHideoutsSettings.Instance.version}, tested for Bannerlord 1.5.1"));

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
    }
  }
}
