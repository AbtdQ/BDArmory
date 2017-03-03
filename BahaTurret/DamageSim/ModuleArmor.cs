using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BahaTurret
{
    class ModuleArmor : PartModule, IPartMassModifier, IPartCostModifier
    {
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            throw new NotImplementedException();
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            throw new NotImplementedException();
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            throw new NotImplementedException();
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            throw new NotImplementedException();
        }
    }
}
