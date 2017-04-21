using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BahaTurret
{
    interface IDamageReceiver
    {
        void ReceiveDamage(float kineticEnergy, DamageSim.ArmorPenetrationResults APResult, bool ignoreArmor = false);
    }
}
