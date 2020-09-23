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

    [SettingPropertyFloatingInteger("Hideout Cost Multiplier", 0.0f, 3.0f, RequireRestart = false, Order = 2, HintText = "Modifies the initial cost of taking a hideout")]//, Order = 2, RequireRestart = false)]
    [SettingPropertyGroup("Taking Hideouts")]
    public float HideoutCostMultiplier { get; set; } = 1.0f;


    [SettingPropertyBool("Recruit Bandit Troops", RequireRestart = false, HintText = "Allows recruiting bandit troops.")]//, IsToggle = false, Order = 1, RequireRestart = false)]
    [SettingPropertyGroup("Recruit Bandits")]
    public bool RecruitingBanditsEnabled { get; set; } = true;
    /*
    public static TakeHideoutsSettings Instance
    {
      get
      {
        return (TakeHideoutsSettings)SettingsDatabase.GetSettings<TakeHideoutsSettings>();
      }
    }

    [XmlElement]
    public override string ID { get; set; } = "MyModSettings";
    public override string ModName => "Testing Modlib";
    public override string ModuleFolderName => "testfolder";

    [XmlElement]
    [SettingProperty("Option 1", 0, 100)]
    //[SettingPropertyGroup("Level1")]
    public int Test_Option1 { get; set; } = 0;
    */
    /*
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

    public override string Id { get; } = "TakeHideouts";
    public override string DisplayName { get; } = $"Take Hideouts";
    public override string FolderName => nameof(TakeHideoutsSettings);
    public override string Format => "json";


    [SettingPropertyBool("Taking Hideouts", HintText = "Allows taking and later abandoning bandit hideouts.")]//, IsToggle = false, Order = 1, RequireRestart = false)]
//    [SettingPropertyGroup("Taking Hideouts")]
    public bool TakingHideoutsEnabled { get; set; }

    [SettingPropertyFloatingInteger("Hideout Cost Multiplier", 0.0f, 3.0f, HintText = "Modifies the initial cost of taking a hideout")]//, Order = 2, RequireRestart = false)]
//    [SettingPropertyGroup("Taking Hideouts")]
    public float HideoutCostMultiplier { get; set; }


    [SettingPropertyBool("Recruit Bandit Troops", HintText = "Allows recruiting bandit troops.")]//, IsToggle = false, Order = 1, RequireRestart = false)]
//[SettingPropertyGroup("Recruit Bandits")]
    public bool RecruitingBanditsEnabled { get; set; }
    */
  }
}
