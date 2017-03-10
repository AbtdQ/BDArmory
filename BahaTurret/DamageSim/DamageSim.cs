using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BahaTurret
{
    static class DamageSim
    {
        public enum ArmorPenetrationResults
        {
            Richochet,
            NotPenetrated,
            Penetrated,
            FullyPenetrated
        }

        public class BulletHitResult
        {
            public ArmorPenetrationResults APResult;
            public ArmorPenetration.BulletPenetrationData bulletPenetrationData;
            
            public BulletHitResult(ArmorPenetrationResults APResult = ArmorPenetrationResults.NotPenetrated, ArmorPenetration.BulletPenetrationData bulletPenetrationData = null)
            { this.APResult = APResult; this.bulletPenetrationData = bulletPenetrationData; }
        }

        static public bool HasArmorModule(Part part) { return part && part.Modules.Contains<ModuleArmor>(); }

        static public bool HasHealthModule(Part part) { return part && part.Modules.Contains<ModuleHealth>(); }

        static public ModuleArmor GetArmorModule(Part part) { return HasArmorModule(part) ? part.Modules.GetModule<ModuleArmor>() : null; }

        static public ModuleHealth GetHealthModule(Part part) { return HasHealthModule(part) ? part.Modules.GetModule<ModuleHealth>() : null; }

        static public void ApplyHeatDamage(Part part, float heatDamage) { var DM = GetHealthModule(part); if (DM) DM.ApplyDamageHeat(heatDamage); else part.temperature += heatDamage; }

        static public BulletHitResult BulletHitPart(Part part, Ray ray, RaycastHit hit, PooledBullet bullet)
        {

        }

        static public BulletHitResult BulletHitBuilding(DestructibleBuilding building, Ray ray, RaycastHit hit, PooledBullet bullet)
        {

        }
    }
}
