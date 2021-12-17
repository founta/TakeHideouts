using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;

using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base.Global;

using HarmonyLib;


namespace TakeHideouts
{
  public class TakeHideoutsSettings : AttributeGlobalSettings<TakeHideoutsSettings>
  {
    public TakeHideoutsSettings() : base()
    {
      //read version from xml
      XmlReader reader = XmlReader.Create("../../Modules/TakeHideouts/SubModule.xml"); //easier way to get at version?
      reader.ReadToFollowing("Version");
      reader.MoveToFirstAttribute();

      this._version = reader.Value;
    }

    private string _version;
    public string version { get => _version; }

    public override string Id => "TakeHideoutsSettings";
    public override string DisplayName => "Take Hideouts";

    [SettingPropertyBool("Taking Hideouts", RequireRestart = false, Order = 1, HintText = "Allows taking and later abandoning bandit hideouts.")]//, IsToggle = false, Order = 1, RequireRestart = false)]
    [SettingPropertyGroup("Taking Hideouts")]
    public bool TakingHideoutsEnabled { get; set; } = true;

    [SettingPropertyFloatingInteger("Hideout Cost Multiplier", 0.0f, 12.0f, RequireRestart = false, Order = 2, HintText = "Modifies the initial cost of taking a hideout")]
    [SettingPropertyGroup("Taking Hideouts")]
    public float HideoutCostMultiplier { get; set; } = 4.0f;


    [SettingPropertyBool("Recruit Bandit Troops", RequireRestart = false, HintText = "Allows recruiting bandit troops.")]
    [SettingPropertyGroup("Recruit Bandits")]
    public bool RecruitingBanditsEnabled { get; set; } = true;

    [SettingPropertyInteger("Bandit recruitment cost multiplier", 0, 20, RequireRestart = false, HintText = "A multiplier for how much bandit troops cost when recruiting from hideouts")]
    [SettingPropertyGroup("Recruit Bandits")]
    public int BanditRecruitmentCostMultiplier { get; set; } = 5;

    [SettingPropertyBool("Hideout Patrol Creation Enabled", RequireRestart = false, HintText = "Allows the creation of bandit patrol parties.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public bool HideoutPatrolsEnabled { get; set; } = true;

    [SettingPropertyInteger("Hideout Patrol Party Size Limit", 20, 60, RequireRestart = false, HintText = "Changes the party size limit of owned bandit patrol parties.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public int HideoutPatrolSize { get; set; } = 20;

    [SettingPropertyBool("Show Bandits on Party Screen", RequireRestart = true, HintText = "Whether or not to show bandit parties on the clan party page. " +
      "Disabling this will make it hard to disband them, if desired.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public bool ShowBanditsOnPartyScreen { get; set; } = false;

    [SettingPropertyBool("Show Bandit Patrol Party Tracker", RequireRestart = true, HintText = "Whether or not to show bandit parties on the map " +
      "when out of view distance, like with armies or caravans.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public bool ShowBanditPatrolMobilePartyTracker { get; set; } = false;


    [SettingPropertyBool("Enable Bandit Patrols Submenu", RequireRestart = false, HintText = "Whether or not to group hideout patrol menu options inside a submenu. " +
      "No effect unless activated from the main menu.")]
    [SettingPropertyGroup("Hideout Menus")]
    public bool PatrolSubmenuEnabled { get; set; } = true;

    [SettingPropertyBool("Enable Hideout Stash Submenu", RequireRestart = false, HintText = "Whether or not to group hideout stash menu options (item, prisoner, troop stashes) inside a submenu. " +
      "No effect unless activated from the main menu.")]
    [SettingPropertyGroup("Hideout Menus")]
    public bool StashSubmenuEnabled { get; set; } = true;


    [SettingPropertyFloatingInteger("Take hideout roguery gain multiplier", 0.0f, 10.0f, RequireRestart = false, HintText = "A multiplier for how much roguery experience is awarded on initially taking a hideout")]
    [SettingPropertyGroup("Hideout Experience")]
    public float TakeHideoutExpMultiplier { get; set; } = 1.0f;

    [SettingPropertyFloatingInteger("Hideout ownership roguery gain multiplier", 0.0f, 10.0f, RequireRestart = false, HintText = "A multiplier for how much roguery experience is awarded daily for each hideout owned")]
    [SettingPropertyGroup("Hideout Experience")]
    public float HideoutOwnershipExpMultipier { get; set; } = 1.0f;

    [SettingPropertyFloatingInteger("Bandit recruitment roguery gain multiplier", 0.0f, 10.0f, RequireRestart = false, HintText = "A multiplier for how much roguery experience is awarded when recruiting bandits from hideouts")]
    [SettingPropertyGroup("Hideout Experience")]
    public float BanditRecruitmentExpMultiplier { get; set; } = 1.0f;
  }
}
