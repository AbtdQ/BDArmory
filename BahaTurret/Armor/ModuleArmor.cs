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

        #region KSPField UI

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Armor", guiUnits = "mm")]
        [UI_FloatEdit(affectSymCounterparts = UI_Scene.Editor, scene = UI_Scene.Editor, incrementLarge = 50f, incrementSlide = 1f, unit = "mm")]
        public float thickness = float.NaN;

        public void InitGUI()
        {
            var thicknessField = Fields[nameof(thickness)];
            var thicknessCtrlEditor = thicknessField?.uiControlEditor as UI_FloatEdit;
            if (thicknessCtrlEditor != null)
            {
                thicknessCtrlEditor.maxValue = maxThickness;
                thicknessCtrlEditor.minValue = defaultThickness;
            }
            var thicknessCtrlFlight = thicknessField?.uiControlFlight as UI_FloatEdit;
            if (thicknessCtrlFlight != null)
            {
                thicknessCtrlFlight.maxValue = maxThickness;
                thicknessCtrlFlight.minValue = defaultThickness;
            }
            if (float.IsNaN(thickness)) thickness = defaultThickness;
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
        public float equivalent = 1f;      // Coefficient

        [KSPField]
        public float unitVolumeCost = 400f;     // Fund/m^3

        [KSPField]
        public float surfaceArea = float.NaN;

        [KSPField]
        public float explDefense = 1f;      // Kelvin/mm

        [KSPField]
        public ArmorShapes shape = ArmorShapes.Box;

        #endregion

        #region API

        public float ExtraThickness { get { return thickness - defaultThickness; } }

        public float Area { get { return (float.IsNaN(surfaceArea) ? part.partInfo.partSize : surfaceArea) * (shape == ArmorShapes.Plane ? 0.5f : 1f); } }

        public float Volume { get { return (thickness / 1000f) * Area; } }

        public float Mass { get { return Volume * density / 1000f; } }

        public float ExtraVolume { get { return (ExtraThickness / 1000f) * Area; } }

        public float ExtraMass { get { return ExtraVolume * density / 1000f; } }

        public float Cost { get { return ExtraVolume * unitVolumeCost; } }

        public float ExplDefense { get { return thickness * explDefense; } }

        public float GetThicknessIn(float angleFromNormal)
        {
            return Mathf.Clamp(thickness * equivalent / Mathf.Cos(angleFromNormal * Mathf.Deg2Rad), 0f, 2f * thickness);
        }

        public float GetThicknessOut(float angleFromNormal)
        {
            return shape == ArmorShapes.Plane ? 0f : Mathf.Clamp(thickness * equivalent / Mathf.Cos(angleFromNormal * Mathf.Deg2Rad), 0f, 2f * thickness);
        }

        #endregion

        #region KSP Events

        public override void OnStart(StartState state)
        {
            InitGUI();
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.Append(Environment.NewLine);
            info.Append($"Shape: {shape}{Environment.NewLine}");
            info.Append($"Default Thickness: {defaultThickness}mm" + Environment.NewLine);
            info.Append($"Max Thickness: {maxThickness}mm" + Environment.NewLine);
            info.Append($"HE Defense: {explDefense}Kelvin/mm" + Environment.NewLine);
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
