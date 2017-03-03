using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BahaTurret
{
    static class DamageSim
    {
        static public bool UseModuleDamage(Part part)
        {
            if (part && part.Modules.Contains<ModuleArmor>())
                return true;
            else
                return false;
        }

        static public ModuleArmor GetArmorModule(Part part)
        {
            if (part && part.Modules.Contains<ModuleArmor>())
                return part.Modules.GetModule<ModuleArmor>();
            else
                return null;
        }

        static public ModuleHealth GetHealthModule(Part part)
        {
            if (part && part.Modules.Contains<ModuleHealth>())
                return part.Modules.GetModule<ModuleHealth>();
            else
                return null;
        }


    }
}
