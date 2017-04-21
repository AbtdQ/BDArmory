using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BahaTurret
{
    public class ExplosionFX : MonoBehaviour
    {
        KSPParticleEmitter[] pEmitters;
        Light lightFX;
        float startTime;
        public AudioClip exSound;
        public AudioSource audioSource;
        float maxTime = 0;

        public float range;

        void Start()
        {
            startTime = Time.time;
            pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
            foreach (KSPParticleEmitter pe in pEmitters)
            {
                pe.emit = true;

                //if(pe.useWorldSpace) pe.force = (4.49f * FlightGlobals.getGeeForceAtPosition(transform.position));

                if (pe.maxEnergy > maxTime)
                {
                    maxTime = pe.maxEnergy;
                }
            }
            lightFX = gameObject.AddComponent<Light>();
            lightFX.color = Misc.ParseColor255("255,238,184,255");
            lightFX.intensity = 8;
            lightFX.range = range*3f;
            lightFX.shadows = LightShadows.None;


            audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;

            audioSource.PlayOneShot(exSound);
        }

        void FixedUpdate()
        {
            lightFX.intensity -= 12*Time.fixedDeltaTime;
            if (Time.time - startTime > 0.2f)
            {
                foreach (KSPParticleEmitter pe in pEmitters)
                {
                    pe.emit = false;
                }
            }

            if (Time.time - startTime > maxTime)
            {
                GameObject.Destroy(gameObject);
            }
        }

        public static void CreateExplosion(Vector3 position, float radius, float power, float heat, Vessel sourceVessel,
            Vector3 direction, string explModelPath, string soundPath)
        {
            GameObject go;
            AudioClip soundClip;

            go = GameDatabase.Instance.GetModel(explModelPath);
            soundClip = GameDatabase.Instance.GetAudioClip(soundPath);


            Quaternion rotation = Quaternion.LookRotation(VectorUtils.GetUpDirection(position));
            GameObject newExplosion = (GameObject) GameObject.Instantiate(go, position, rotation);
            newExplosion.SetActive(true);
            ExplosionFX eFx = newExplosion.AddComponent<ExplosionFX>();
            eFx.exSound = soundClip;
            eFx.audioSource = newExplosion.AddComponent<AudioSource>();
            eFx.audioSource.minDistance = 200;
            eFx.audioSource.maxDistance = 5500;
            eFx.audioSource.spatialBlend = 1;
            eFx.range = radius;

            if (power <= 5)
            {
                eFx.audioSource.minDistance = 4f;
                eFx.audioSource.maxDistance = 3000;
                eFx.audioSource.priority = 9999;
            }
            foreach (KSPParticleEmitter pe in newExplosion.GetComponentsInChildren<KSPParticleEmitter>())
            {
                pe.emit = true;
            }

            DoExplosionDamage(position, power, heat, radius, sourceVessel);
        }

        public static float ExplosionHeatMultiplier = 4200;
        public static float ExplosionImpulseMultiplier = 1.5f;

		public static void DoExplosionRay(Ray ray, float power, float heat, float maxDistance, ref List<Part> ignoreParts, ref List<DestructibleBuilding> ignoreBldgs, Vessel sourceVessel = null)
		{
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, maxDistance, 557057))
			{
				float sqrDist = (rayHit.point - ray.origin).sqrMagnitude;
				float sqrMaxDist = maxDistance * maxDistance;
				float distanceFactor = Mathf.Clamp01((sqrMaxDist - sqrDist) / sqrMaxDist);
				//parts
				Part part = rayHit.collider.GetComponentInParent<Part>();
				if(part)
				{
					Vessel missileSource = null;
					if(sourceVessel != null)
					{
                        var ml = part.FindModuleImplementing<MissileBase>();
						if(ml)
						{
							missileSource = ml.SourceVessel;
						}
					}

                    if (!ignoreParts.Contains(part) && part.physicalSignificance == Part.PhysicalSignificance.FULL &&
                        (!sourceVessel || sourceVessel != missileSource))
                    {
                        ignoreParts.Add(part);
                        DamageSim.ExplosionHitPart(part, ray, rayHit, heat, power, distanceFactor);
                        return;
                    }
                }

                //buildings
                DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();
                if (building && !ignoreBldgs.Contains(building))
                {
                    ignoreBldgs.Add(building);
                    DamageSim.ExplosionHitBuilding(building, power, distanceFactor);
                }
            }
        }

        public static List<Part> ignoreParts = new List<Part>();
        public static List<DestructibleBuilding> ignoreBuildings = new List<DestructibleBuilding>();

		public static void DoExplosionDamage(Vector3 position, float power, float heat, float maxDistance, Vessel sourceVessel)
		{
			if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log($"[{nameof(ExplosionFX)}]: ======= Doing explosion sphere =========");
			ignoreParts.Clear();
			ignoreBuildings.Clear();

		    var vesselsAffected =
		        BDATargetManager.LoadedVessels.Where(
		            v => v != null && v.loaded && !v.packed && (v.CoM - position).magnitude < maxDistance*4);

		    var partsAffected =
		        vesselsAffected.SelectMany(v => v.parts).Where(p => p!=null && p && (p.transform.position - position).magnitude < maxDistance);

		    foreach (var part in partsAffected)
		    {
                DoExplosionRay(new Ray(position, part.transform.TransformPoint(part.CoMOffset) - position), power, heat, maxDistance, ref ignoreParts, ref ignoreBuildings, sourceVessel);
            }
		      
			foreach(var bldg in BDATargetManager.LoadedBuildings)
			{
				if(bldg == null) continue;
				if((bldg.transform.position - position).magnitude < maxDistance * 1000)
				{
					DoExplosionRay(new Ray(position, bldg.transform.position - position), power, heat, maxDistance, ref ignoreParts, ref ignoreBuildings);
				}
			}
		}
	}
}

