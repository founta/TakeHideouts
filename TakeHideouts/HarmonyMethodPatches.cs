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

using HarmonyLib;

namespace TakeHideouts
{
  //patch Hideout Mapfaction getter to correctly set hideout's mapfaction
  //after we take the hideout
  [HarmonyPatch(typeof(Hideout), "MapFaction")]
  [HarmonyPatch(MethodType.Getter)]
  public class HideoutMapFactionPatch
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

  //patch troop transfer method to allow trading of party members
  //used to only allow trading prisoners
  //This is mostly copy-pasted from decompiled DLL, modified slightly
  [HarmonyPatch(typeof(PartyScreenLogic), "TransferTroop")]
  public class TakeHideoutsTransferTroopPatch
  {
    static void Postfix(PartyScreenLogic __instance, PartyScreenLogic.PartyCommand command)
    {
      if (__instance.MemberTransferState == PartyScreenLogic.TransferState.TransferableWithTrade && command.Type == PartyScreenLogic.TroopType.Member)
      {
        if (command.RosterSide == PartyScreenLogic.PartyRosterSide.Right)
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount + command.Character.PrisonerRansomValue(Hero.MainHero) * command.TotalNumber);
        else
          ExposeInternals.SetPartyGoldChangeAmount(__instance, __instance.PartyGoldChangeAmount - command.Character.PrisonerRansomValue(Hero.MainHero) * command.TotalNumber);
      }
    }
  }

  //patch 'Wait until nightfall' on_condition to disable attacking the hideout
  //if we own the hideout
  //bonus -- this also disables the attack option if it's already night time
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
}
