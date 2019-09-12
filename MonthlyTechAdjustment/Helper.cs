using BattleTech;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using Harmony;
using BattleTech.Save.Test;
using System.Linq;

namespace MonthlyTechandMoraleAdjustment
{
    public class SaveFields
    {
        public int ExpenseLevel = 0;
        public int DeltaMechTech = 0;
        public int DeltaMedTech = 0;

        public SaveFields(int expenseLevel, int deltamechtech, int deltamedtech)
        {
            ExpenseLevel = expenseLevel;
            DeltaMechTech = deltamechtech;
            DeltaMedTech = deltamedtech;
        }
    }
    public class SaveHandling
    {
        [HarmonyPatch(typeof(SimGameState), "_OnAttachUXComplete")]
        public static class SimGameState__OnAttachUXComplete_Patch
        {
            public static void Postfix()
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim.CompanyTags.Any(x => x.StartsWith("MTMA{")))
                {
                    DeserializeMTMA();
                }
            }
        }

        [HarmonyPatch(typeof(SerializableReferenceContainer), "Save")]
        public static class SerializableReferenceContainer_Save_Patch
        {
            public static void Prefix()
            {
                if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                SerializeMTMA();
            }
        }

        [HarmonyPatch(typeof(SerializableReferenceContainer), "Load")]
        public static class SerializableReferenceContainer_Load_Patch
        {
            // get rid of tags before loading because vanilla behaviour doesn't purge them
            public static void Prefix()
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                sim.CompanyTags.Where(tag => tag.StartsWith("MTMA")).Do(x => sim.CompanyTags.Remove(x));
            }

            public static void Postfix()
            {
                
            }
        }

        internal static void SerializeMTMA()
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            bool tagexists = false;
            foreach (string tagcheck in sim.CompanyTags)
            {
                if (tagcheck.StartsWith("MTMA"))
                    tagexists = true;
            }
            if (!tagexists)
            {
                Adjust_Techs_Financial_Report_Patch.SaveFields = new SaveFields(0, 0, 0);
                Fields.ExpenseLevel = 0;
                Fields.DeltaMechTech = 0;
                Fields.DeltaMedTech = 0;
            }
            sim.CompanyTags.Where(tag => tag.StartsWith("MTMA")).Do(x => sim.CompanyTags.Remove(x));
            sim.CompanyTags.Add("MTMA" + JsonConvert.SerializeObject(Adjust_Techs_Financial_Report_Patch.SaveFields));
            foreach (string tagcheck in sim.CompanyTags)
            {
                if (tagcheck.StartsWith("MTMA"))
                    Logger.Log(tagcheck);
            }
        }

        internal static void DeserializeMTMA()
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            Adjust_Techs_Financial_Report_Patch.SaveFields = JsonConvert.DeserializeObject<SaveFields>(sim.CompanyTags.First(x => x.StartsWith("MTMA{")).Substring(4));
            Fields.ExpenseLevel = Adjust_Techs_Financial_Report_Patch.SaveFields.ExpenseLevel;
            Fields.DeltaMechTech = Adjust_Techs_Financial_Report_Patch.SaveFields.DeltaMechTech;
            Fields.DeltaMedTech = Adjust_Techs_Financial_Report_Patch.SaveFields.DeltaMedTech;
        }
    }
}