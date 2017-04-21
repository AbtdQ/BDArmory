using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BahaTurret
{
    class ModuleArmor : PartModule, IPartMassModifier, IPartCostModifier
    {
        public enum ArmorShapes { Box, Plane }

        static public ModuleArmor _defaultArmor = new ModuleArmor();

        #region KSPField UI

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor", guiUnits = "mm")]
        [UI_FloatEdit(affectSymCounterparts = UI_Scene.Editor, scene = UI_Scene.Editor, incrementLarge = 50f, incrementSlide = 1f, unit = "mm")]
        public float armorThickness = float.NaN;

        public void InitGUIarmorThickness()
        {
            var armorThicknessField = Fields[nameof(armorThickness)];
            var armorThicknessCtrlEditor = armorThicknessField?.uiControlEditor as UI_FloatEdit;
            if (armorThicknessCtrlEditor != null)
            {
                armorThicknessCtrlEditor.maxValue = maxThickness;
                armorThicknessCtrlEditor.minValue = defaultThickness;
            }
            var armorThicknessCtrlFlight = armorThicknessField?.uiControlFlight as UI_FloatEdit;
            if (armorThicknessCtrlFlight != null)
            {
                armorThicknessCtrlFlight.maxValue = maxThickness;
                armorThicknessCtrlFlight.minValue = defaultThickness;
            }
            if (float.IsNaN(armorThickness)) armorThickness = defaultThickness;
        }

        #endregion

        #region KSPField

        [KSPField]
        public float defaultThickness = 2f;

        [KSPField]
        public float maxThickness = 100f;

        [KSPField]
        public float density = 7800f;     // steel, kg/m^3

        [KSPField]
        public float equivalentCoefficient = 1f;

        [KSPField]
        public float unitVolumeCost = 400f;     // fund/m^3

        [KSPField]
        public float surfaceArea = float.NaN;

        [KSPField]
        public float unitThicknessExplosionHeatDecrement = 1f;      // K/mm

        [KSPField]
        public ArmorShapes shape = ArmorShapes.Box;

        #endregion

        #region API

        public float ExtraThickness { get { return armorThickness - defaultThickness; } }

        public float Area { get { return (float.IsNaN(surfaceArea) ? part.partInfo.partSize : surfaceArea) * (shape == ArmorShapes.Plane ? 0.5f : 1f); } }

        public float Volume { get { return (armorThickness / 1000f) * Area; } }

        public float Mass { get { return Volume * density / 1000f; } }

        public float ExtraVolume { get { return (ExtraThickness / 1000f) * Area; } }

        public float ExtraMass { get { return ExtraVolume * density / 1000f; } }

        public float Cost { get { return ExtraVolume * unitVolumeCost; } }

        public float ExplosionHeatDecrement { get { return armorThickness * unitThicknessExplosionHeatDecrement; } }

        public float GetArmorThicknessIn(float angleFromNormal)
        {
            return Mathf.Clamp(armorThickness * equivalentCoefficient / Mathf.Cos(angleFromNormal * Mathf.Deg2Rad), 0f, 2f * armorThickness);
        }

        public float GetArmorThicknessOut(float angleFromNormal)
        {
            return shape == ArmorShapes.Plane ? 0f : Mathf.Clamp(armorThickness * equivalentCoefficient / Mathf.Cos(angleFromNormal * Mathf.Deg2Rad), 0f, 2f * armorThickness);
        }

        #endregion

        #region KSP Events

        public override void OnStart(StartState state)
        {
            InitGUIarmorThickness();
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.Append(Environment.NewLine);
            info.Append($"Shape: {shape}{Environment.NewLine}");
            info.Append($"Default Thickness: {defaultThickness}mm" + Environment.NewLine);
            info.Append($"Max Thickness: {maxThickness}mm" + Environment.NewLine);
            info.Append($"HE Heat Decrement: {unitThicknessExplosionHeatDecrement}K/mm" + Environment.NewLine);
            info.Append($"Area: {Area.ToString("0.00")}m^2" + Environment.NewLine);
            info.Append($"Density: {density}Kg/m^3" + Environment.NewLine);
            info.Append($"Unit Volume Cost: {unitVolumeCost}" + Environment.NewLine);
            return info.ToString();
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return float.IsNaN(Cost) ? 0f : Cost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return float.IsNaN(ExtraMass) ? 0f : ExtraMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        #endregion
    }
}
