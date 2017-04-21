using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BahaTurret
{
    class ModuleExplosiveRes : PartModule, IDamageReceiver
    {
        [KSPField]
        public string resource = "Oxidizer:0.01";

        public Dictionary<string, float> scales;
        public void initScales()
        {
            string[] scalesArr = resource.Split();
            foreach (string v in scalesArr)
            {
                string[] vArr = v.Split(':');
                scales.Add(vArr[0], float.Parse(vArr[1]));
            }
        }

        [KSPField]
        public float power = 1f;
        [KSPField]
        public float radius = 1f;
        [KSPField]
        public float heat = 1f;

        [KSPField]
        public string explModelPath = "BDArmory/Models/explosion/explosionLarge";
        [KSPField]
        public string explSoundPath = "BDArmory/Sounds/explode1";

        public float ExplodeScale
        {
            get
            {
                float scale = 0f;
                foreach (var res in part.Resources)
                {
                    if (scales.ContainsKey(res.resourceName))
                    {
                        scale += scales[res.resourceName] * (float)res.amount;
                    }
                }
                return scale;
            }
        }

        public void Detonate()
        {
            float explScale = ExplodeScale;
            if (explScale > 0f)
            {
                List<string> explRes = new List<string>();
                foreach (var res in scales.Keys)
                {
                    if (part.Resources[res].amount > 0d)
                        explRes.Add(res);
                }
                FlightLogger.eventLog.Add($"[{KSPUtil.PrintTimeStamp(FlightLogger.met)}]{explRes} in {part.partInfo.title} was ignited");
                ExplosionFX.CreateExplosion(part.partTransform.position, explScale * radius, explScale * power * 2f, explScale * heat, part.vessel, FlightGlobals.upAxis, explModelPath, explSoundPath);
            }
        }

        public override void OnStart(StartState state)
        {
            initScales();
            if (HighLogic.LoadedSceneIsFlight)
                part.OnJustAboutToBeDestroyed += Detonate; 
        }

        public void ReceiveDamage(float kineticEnergy, DamageSim.ArmorPenetrationResults APResult, bool ignoreArmor = false)
        {
            if (APResult == DamageSim.ArmorPenetrationResults.FullyPenetrated ||
                APResult == DamageSim.ArmorPenetrationResults.Penetrated || 
                (ignoreArmor && kineticEnergy >= 0.5f * part.mass * 1000f * Mathf.Pow(part.crashTolerance, 2f)))
            {
                if (ExplodeScale > 0f)
                    part.explode();
            }
        }
    }
}
