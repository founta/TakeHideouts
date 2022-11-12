using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

using HarmonyLib;


namespace TakeHideouts
{
  class TakeHideoutsSaveableTypeDefiner : SaveableTypeDefiner
  {
    private static int TakeHideoutsBaseID = 3705000;
    private static int OwnedBanditPatrolID = 3705001;

    public TakeHideoutsSaveableTypeDefiner() : base(TakeHideoutsBaseID)
    { }

    protected override void DefineClassTypes()
    {
      AddClassDefinition(typeof(OwnedBanditPatrolComponent), OwnedBanditPatrolID);
    }
  }


}
