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
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameState;

using SandBox.View.Map;
using TaleWorlds.Localization;

using HarmonyLib;

namespace TakeHideouts
{
  class InventoryScreenAdditions
  {
    public static void OpenScreenAsManageInventory(ItemRoster roster, string header="Manage Inventory", InventoryManager.InventoryCategoryType focus = InventoryManager.InventoryCategoryType.None) //same as open as stash, but says manage inventory
    {
      //get access to private _currentMode and _inventoryLogic from InventoryManager
      ref InventoryMode currentMode = ref AccessTools.FieldRefAccess<InventoryManager, InventoryMode>(InventoryManager.Instance, "_currentMode");
      ref InventoryLogic inventoryLogic = ref AccessTools.FieldRefAccess<InventoryManager, InventoryLogic>(InventoryManager.Instance, "_inventoryLogic");

      currentMode = InventoryMode.Stash;
      inventoryLogic = new InventoryLogic((PartyBase)null);
      inventoryLogic.Initialize(roster, MobileParty.MainParty, false, false, CharacterObject.PlayerCharacter, focus, 
        ExposeInternals.GetCurrentMarketData(InventoryManager.Instance), false, new TextObject(header));
      InventoryState state = Game.Current.GameStateManager.CreateState<InventoryState>();
      state.InitializeLogic(inventoryLogic);
      Game.Current.GameStateManager.PushState((GameState)state);
    }
  }


}
