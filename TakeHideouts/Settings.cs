using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;

using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base.Global;

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

    [SettingPropertyBool("Hideout Patrol Creation Enabled", RequireRestart = false, HintText = "Allows the creation of bandit patrol parties.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public bool HideoutPatrolsEnabled { get; set; } = true;

    [SettingPropertyBool("Show Bandits on Party Screen", RequireRestart = true, HintText = "Whether or not to show bandit parties on the clan party page. " +
      "Disabling this will make it hard to disband them, if desired.")]
    [SettingPropertyGroup("Bandit Patrols")]
    public bool ShowBanditsOnPartyScreen { get; set; } = true;

    //TODO option to show/hide hideout patrols on party screen. Maybe somehow just set them all to militias or something
    //or can I modify harmony patch prepare methods at runtime? probably not
  }
}
