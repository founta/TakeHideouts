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
using TaleWorlds.Localization;

using HarmonyLib;

namespace TakeHideouts
{
  class InventoryScreenAdditions
  {
    public static void OpenScreenAsManageInventory(ItemRoster roster) //same as open as stash, but says manage inventory
    {
      //get access to private _currentMode and _inventoryLogic from InventoryManager
      ref InventoryMode currentMode = ref AccessTools.FieldRefAccess<InventoryManager, InventoryMode>(InventoryManager.Instance, "_currentMode");
      ref InventoryLogic inventoryLogic = ref AccessTools.FieldRefAccess<InventoryManager, InventoryLogic>(InventoryManager.Instance, "_inventoryLogic");

      currentMode = InventoryMode.Stash;
      inventoryLogic = new InventoryLogic(Campaign.Current, (PartyBase)null);
      inventoryLogic.Initialize(roster, MobileParty.MainParty, false, false, CharacterObject.PlayerCharacter, InventoryManager.InventoryCategoryType.None, ExposeInternals.GetCurrentMarketData(InventoryManager.Instance), false, new TextObject("Manage Inventory"));
      InventoryState state = Game.Current.GameStateManager.CreateState<InventoryState>();
      state.InitializeLogic(inventoryLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }
  }


}
