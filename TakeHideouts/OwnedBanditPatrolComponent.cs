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

using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;

using HarmonyLib;

namespace TakeHideouts
{
  class OwnedBanditPatrolComponent : WarPartyComponent
  {

    [SaveableProperty(1)]
    public Hideout hideout { get; private set; }

    public OwnedBanditPatrolComponent(Hideout hideout)
    {
      this.hideout = hideout;
    }

    public override Settlement HomeSettlement => this.hideout.Settlement;
    public override Hero PartyOwner => this.MobileParty.ActualClan?.Leader;
    public override TextObject Name
    {
      get
      {
        return this.hideout.MapFaction.Name;
      }
    }

    public static MobileParty CreatePatrolParty(
      string stringId,
      Clan clan,
      Hideout hideout)
    {
      return MobileParty.CreateParty(stringId, (PartyComponent)new OwnedBanditPatrolComponent(hideout), (PartyComponent.OnPartyComponentCreatedDelegate)(mobileParty => mobileParty.ActualClan = clan));
    }

  }


}
