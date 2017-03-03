using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BahaTurret.DamageSim
{
    class ModuleHealth : PartModule
    {
        #region KSPField UI

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Health", guiFormat = "P0")]
        [UI_ProgressBar(scene = UI_Scene.Flight, minValue = 0f, maxValue = 1f, controlEnabled = false)]
        public float health = 1f;

        #endregion

        #region API

        public event Callback<float, float> OnHealthChanged;    //OnHealthChanged(from, to)

        public void ApplyDamageHeat(float heatDamage)
        {
            ApplyDamagePercentage(GetDamagePercentage(heatDamage));
        }

        public void ApplyDamagePercentage(float damagePercentage)
        {
            float changedHealth = Mathf.Clamp01(health - damagePercentage);
            OnHealthChanged?.Invoke(health, changedHealth);
            health = changedHealth;
        }

        public float GetDamagePercentage(float heatDamage)
        {
            return heatDamage / (float)part.maxTemp;
        }

        #endregion

        #region KSP Events

        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);

            if (state != StartState.None && state != StartState.Editor)
            {
                OnHealthChanged?.Invoke(health, health);
            }
        }

        #endregion

        #region Debug Stuff

        [KSPEvent(guiName = "DEBUG_ApplyDamage100t", guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = float.MaxValue, advancedTweakable = true)]
        public void Debug_ApplyDamage100t() { ApplyDamageHeat(100f); }

        [KSPEvent(guiName = "DEBUG_ApplyDamage10p", guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = float.MaxValue, advancedTweakable = true)]
        public void Debug_ApplyDamage10p() { ApplyDamagePercentage(0.1f); }

        #endregion
    }
}
