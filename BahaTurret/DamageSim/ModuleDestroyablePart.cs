using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BahaTurret
{
    class ModuleDestroyablePart : PartModule, IDamageReceiver
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Damaged", guiFormat = "P0")]
        [UI_ProgressBar(minValue = 0f, maxValue = 1f, scene = UI_Scene.Flight, controlEnabled = false)]
        public float GUIDamageRate = 0f;

        [KSPField]
        public float dmgOnRicochet = 0f;
        [KSPField]
        public float dmgOnNotPenetrated = 0.1f;
        [KSPField]
        public float dmgOnPenetrated = 1f;
        [KSPField]
        public float dmgOnFullyPenetrated = 1f;

        [KSPField]
        public float HPMultiplier = 10f;

        public float crashEk { get { return 0.5f * Mathf.Pow(part.crashTolerance, 2f) * part.mass * 1000f * HPMultiplier; } }

        protected float GetModifiedDamage(float damage, DamageSim.ArmorPenetrationResults APResult)
        {
            switch (APResult)
            {
                case DamageSim.ArmorPenetrationResults.Ricochet:
                    return damage * dmgOnRicochet;
                case DamageSim.ArmorPenetrationResults.NotPenetrated:
                    return damage * dmgOnNotPenetrated;
                case DamageSim.ArmorPenetrationResults.Penetrated:
                    return damage * dmgOnPenetrated;
                case DamageSim.ArmorPenetrationResults.FullyPenetrated:
                    return damage * dmgOnFullyPenetrated;
                default:
                    return damage;
            }
        }

        protected void UpdateDamage(float damage)
        {
            GUIDamageRate = Mathf.Clamp01(GUIDamageRate + damage / (crashEk));

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log($"[{part.name}]DmgAdded={(damage / crashEk) * 100f}%, totDmg={GUIDamageRate * 100f}%");
        }

        protected void CheckState()
        {
            if (GUIDamageRate >= 1f)
            {
                FlightLogger.eventLog.Add($"[{KSPUtil.PrintTimeStamp(FlightLogger.met)}]{part.partInfo.title} was destroyed by high speed metals");
                part?.explode();
            }
        }

        public void ReceiveDamage(float kineticEnergy, DamageSim.ArmorPenetrationResults APResult, bool ignoreArmor = false)
        {
            if (!ignoreArmor)
                kineticEnergy = GetModifiedDamage(kineticEnergy, APResult);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log($"[{part.name}]Hit! Ek={kineticEnergy}, crashEk={crashEk}");
            UpdateDamage(kineticEnergy);
            CheckState();
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.Append($"HP:{crashEk}J{Environment.NewLine}");
            info.Append($"Damage Coefficients:{Environment.NewLine}");
            info.Append($"Ricochet: {dmgOnRicochet.ToString("P0")}{Environment.NewLine}");
            info.Append($"NotPenetrated: {dmgOnNotPenetrated.ToString("P0")}{Environment.NewLine}");
            info.Append($"Penetrated: {dmgOnPenetrated.ToString("P0")}{Environment.NewLine}");
            info.Append($"FullyPenetrated: {dmgOnFullyPenetrated.ToString("P0")}{Environment.NewLine}");
            return info.ToString();
        }
    }
}
