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
            Ricochet,
            NotPenetrated,
            Penetrated,
            FullyPenetrated
        }

        public class BulletHitResult
        {
            public ArmorPenetrationResults APResult;
            public ArmorPenetration.BulletPenetrationData bulletPenetrationData;
            
            public BulletHitResult(ArmorPenetrationResults APResult = ArmorPenetrationResults.Penetrated, ArmorPenetration.BulletPenetrationData bulletPenetrationData = null)
            { this.APResult = APResult; this.bulletPenetrationData = bulletPenetrationData; }
        }

        static public float KineticToHeat(float kineticEnergy)
        {
            return kineticEnergy / 40f;
        }

        static public float HeatToKinetic(float heatDamage)
        {
            return heatDamage * 40f;
        }

        static public bool HasArmorModule(Part part) { return part && part.Modules.Contains<ModuleArmor>(); }

        static public ModuleArmor GetArmorModule(Part part) { if (HasArmorModule(part)) return part.Modules.GetModule<ModuleArmor>(); else { ModuleArmor._defaultArmor.armorThickness = 5f; return ModuleArmor._defaultArmor; } }

        static public void ApplyDamageToPart(Part part, float kineticEnergy, ArmorPenetrationResults APResult, bool ignoreArmor = false)
        {
            bool useLegacyDamage = true;
            foreach (PartModule module in part.Modules)
            {
                if (module is IDamageReceiver)
                {
                    useLegacyDamage = false;
                    ((IDamageReceiver)module).ReceiveDamage(kineticEnergy, APResult, ignoreArmor);
                }
            }
            if (useLegacyDamage)
            {
                float heatDamage = KineticToHeat(kineticEnergy);
                if (!ignoreArmor)
                    switch (APResult)
                    {
                        case ArmorPenetrationResults.Ricochet:
                        case ArmorPenetrationResults.NotPenetrated:
                            heatDamage *= 0.05f;
                            break;
                        case ArmorPenetrationResults.Penetrated:
                            heatDamage *= 0.3f;
                            break;
                        case ArmorPenetrationResults.FullyPenetrated:
                            heatDamage *= 0.15f;
                            break;
                    }
                part.temperature += kineticEnergy;
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log($"[{nameof(DamageSim)}]DamageReceiver Not Found, Heat to {part.name} = {heatDamage.ToString("0.00")}");
            }

            if (BDArmorySettings.INSTAKILL) part.explode();
        }

        static public void ApplyDamageToBuilding(DestructibleBuilding building, float damage, ArmorPenetrationResults APResult)
        {
            switch (APResult)
            {
                case ArmorPenetrationResults.Ricochet:
                case ArmorPenetrationResults.NotPenetrated:
                case ArmorPenetrationResults.FullyPenetrated:
                    damage /= 8f;
                    break;
                case ArmorPenetrationResults.Penetrated:
                    break;
            }
            building.AddDamage(damage);
            if (building.Damage > building.impactMomentumThreshold)
                building.Demolish();
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log($"[{nameof(DamageSim)}]Damage to {building.name} = {damage.ToString("0.00")}");

            if (BDArmorySettings.INSTAKILL) building.Demolish();
        }

        static public BulletHitResult BulletHitPart(Part part, Ray ray, RaycastHit hit, PooledBullet bullet)
        {
            var hitResult = new BulletHitResult(bulletPenetrationData : new ArmorPenetration.BulletPenetrationData(ray, hit));

            switch (bullet.bullet.bulletType)
            {
                case BulletInfo.BulletTypes.AP:
                    // get physics data
                    ArmorPenetration.DoPenetrationRay(hitResult.bulletPenetrationData, bullet.bullet.positiveCoefficient);
                    var normalizedVel = bullet.GetNormalizedDirection(hitResult.bulletPenetrationData);
                    var hitAngleIn = Mathf.Clamp(Vector3.Angle(normalizedVel, -hit.normal), 0f, 90f);
                    var hitAngleOut = Mathf.Clamp(Vector3.Angle(normalizedVel, hitResult.bulletPenetrationData.hitResultOut.normal), 0f, 90f);

                    // get armor data
                    var armorModule = GetArmorModule(part);
                    var armorThicknessIn = armorModule.GetArmorThicknessIn(hitAngleIn);
                    var armorThicknessOut = armorModule.GetArmorThicknessOut(hitAngleOut);
                    var bulletPenetration = bullet.GetPenetration();
                    hitResult.bulletPenetrationData.armorThickness = armorThicknessIn + armorThicknessOut;

                    // APResult
                    if (bulletPenetration < armorModule.GetArmorThicknessIn(0f) * 2f && bullet.IsRicochetOnPart(part, hitAngleIn, bullet.GetImpactVelocity()))
                        hitResult.APResult = ArmorPenetrationResults.Ricochet;
                    else if (bulletPenetration < armorThicknessIn)
                        hitResult.APResult = ArmorPenetrationResults.NotPenetrated;
                    else if (bulletPenetration < armorThicknessIn + armorThicknessOut)
                        hitResult.APResult = ArmorPenetrationResults.Penetrated;
                    else
                        hitResult.APResult = ArmorPenetrationResults.FullyPenetrated;

                    // debug log
                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit {part.name}, {hitResult.APResult}, ArmorIn={armorThicknessIn.ToString("0.00")}, ArmorOut={armorThicknessOut.ToString("0.00")}, Penetration={bulletPenetration.ToString("0.00")}, Ek={bullet.GetKineticEnergy()}");

                    // apply damage
                    ApplyDamageToPart(part, bullet.GetKineticEnergy() * (BDArmorySettings.DMG_MULTIPLIER / 100f) * (bulletPenetration > armorThicknessIn ? Mathf.Clamp01((bulletPenetration - armorThicknessIn)) / bulletPenetration : 1f), hitResult.APResult);
                    break;
                case BulletInfo.BulletTypes.HE:
                    ExplosionFX.CreateExplosion(hit.point - (ray.direction * 0.1f), bullet.radius, bullet.blastPower, bullet.blastHeat, bullet.sourceVessel, bullet.currentVelocity.normalized, bullet.explModelPath, bullet.explSoundPath);
                    hitResult.APResult = ArmorPenetrationResults.NotPenetrated;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit {part.name}, Boom!");
                    break;
            }

            return hitResult;
        }

        static public BulletHitResult BulletHitBuilding(DestructibleBuilding building, Ray ray, RaycastHit hit, PooledBullet bullet)
        {
            var hitResult = new BulletHitResult(bulletPenetrationData: new ArmorPenetration.BulletPenetrationData(ray, hit));

            switch (bullet.bullet.bulletType)
            {
                case BulletInfo.BulletTypes.AP:
                    // get physics data
                    ArmorPenetration.DoPenetrationRay(hitResult.bulletPenetrationData, bullet.bullet.positiveCoefficient);
                    var normalizedVel = bullet.GetNormalizedDirection(hitResult.bulletPenetrationData);
                    var hitAngleIn = Mathf.Clamp(Vector3.Angle(normalizedVel, -hit.normal), 0f, 90f);
                    var hitAngleOut = Mathf.Clamp(Vector3.Angle(normalizedVel, hitResult.bulletPenetrationData.hitResultOut.normal), 0f, 90f);

                    // get armor data
                    hitResult.bulletPenetrationData.armorThickness *= 10f;     // concrete(m) => RHA(mm)
                    var bulletPenetration = bullet.GetPenetration();

                    // APResult
                    if (bullet.IsRicochet(hitAngleIn))
                        hitResult.APResult = ArmorPenetrationResults.Ricochet;
                    else if (bulletPenetration < hitResult.bulletPenetrationData.armorThickness * 0.06f)
                        hitResult.APResult = ArmorPenetrationResults.NotPenetrated;
                    else if (bulletPenetration < hitResult.bulletPenetrationData.armorThickness)
                        hitResult.APResult = ArmorPenetrationResults.Penetrated;
                    else
                        hitResult.APResult = ArmorPenetrationResults.FullyPenetrated;

                    // debug log
                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit {building.name}, {hitResult.APResult}, Thickness={hitResult.bulletPenetrationData.armorThickness.ToString("0.00")}, Penetration={bulletPenetration.ToString("0.00")}, Ek={bullet.GetKineticEnergy()}");

                    // apply damage
                    ApplyDamageToBuilding(building, bullet.GetDamageToBuilding(), hitResult.APResult);
                    break;
                case BulletInfo.BulletTypes.HE:
                    ExplosionFX.CreateExplosion(hit.point - (ray.direction * 0.1f), bullet.radius, bullet.blastPower, bullet.blastHeat, bullet.sourceVessel, bullet.currentVelocity.normalized, bullet.explModelPath, bullet.explSoundPath);
                    hitResult.APResult = ArmorPenetrationResults.NotPenetrated;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit {building.name}, Boom!");
                    break;
            }

            return hitResult;
        }

        static public BulletHitResult BulletHitScenery(Ray ray, RaycastHit hit, PooledBullet bullet)
        {
            var hitResult = new BulletHitResult();

            switch (bullet.bullet.bulletType)
            {
                case BulletInfo.BulletTypes.AP:
                    if (bullet.IsRicochet(Mathf.Clamp(Vector3.Angle(bullet.currentVelocity, -hit.normal), 0f, 90f)))
                        hitResult.APResult = ArmorPenetrationResults.Ricochet;
                    else
                        hitResult.APResult = ArmorPenetrationResults.NotPenetrated;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit Scenery, {hitResult.APResult}");
                    break;
                case BulletInfo.BulletTypes.HE:
                    ExplosionFX.CreateExplosion(hit.point - (ray.direction * 0.1f), bullet.radius, bullet.blastPower, bullet.blastHeat, bullet.sourceVessel, bullet.currentVelocity.normalized, bullet.explModelPath, bullet.explSoundPath);
                    hitResult.APResult = ArmorPenetrationResults.NotPenetrated;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log($"[{nameof(DamageSim)}]{bullet.bullet.name} Hit Scenery, Boom!");
                    break;

            }

            return hitResult;
        }

        static public void ExplosionHitPart(Part part, Ray ray, RaycastHit rayHit, float heat, float power, float distanceFactor)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb)
                rb.AddForceAtPosition(ray.direction * power * distanceFactor * ExplosionFX.ExplosionImpulseMultiplier, rayHit.point, ForceMode.Impulse);
            if (heat < 0) heat = power;
            float kineticEnergy = HeatToKinetic(
                                        Mathf.Clamp(
                                                       ((BDArmorySettings.DMG_MULTIPLIER / 100f) * ExplosionFX.ExplosionHeatMultiplier * heat * distanceFactor / part.crashTolerance)
                                                       - GetArmorModule(part).ExplosionHeatDecrement,
                                                       0f, float.PositiveInfinity
                                                   )
                                                );
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log($"[{nameof(DamageSim)}]Explosion Damage To {part.name} = {kineticEnergy.ToString("0.00")}");
            ApplyDamageToPart(part, kineticEnergy, ArmorPenetrationResults.NotPenetrated, true);
        }

        static public void ExplosionHitBuilding(DestructibleBuilding building, float power, float distanceFactor)
        {
            float damageToBuilding = (BDArmorySettings.DMG_MULTIPLIER / 100f) * ExplosionFX.ExplosionHeatMultiplier * 0.00645f * power * distanceFactor;
            if (damageToBuilding > building.impactMomentumThreshold / 10f)
                building.AddDamage(damageToBuilding);
            if (building.Damage > building.impactMomentumThreshold)
                building.Demolish();
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log($"[{nameof(DamageSim)}]Explosion Damage To {building.name} = {damageToBuilding.ToString("0.00")}");

            if (BDArmorySettings.INSTAKILL) building.Demolish();
        }
    }
}
