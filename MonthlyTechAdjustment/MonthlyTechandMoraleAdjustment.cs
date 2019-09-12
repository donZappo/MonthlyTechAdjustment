using System;
using System.Reflection;
using BattleTech;
using Harmony;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Newtonsoft;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using HBS.Collections;
using static MonthlyTechandMoraleAdjustment.Logger;


namespace MonthlyTechandMoraleAdjustment
{
    public static class Pre_Control
    {
        #region Init

        public static void Init(string modDir, string settings)
        {
            var harmony = HarmonyInstance.Create("com.MonthlyTechAndMorale");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // read settings
            try
            {
                Settings = JsonConvert.DeserializeObject<ModSettings>(settings);
                Settings.modDirectory = modDir;
            }
            catch (Exception)
            {
                Settings = new ModSettings();
            }

            // blank the logfile
            Clear();
            // PrintObjectFields(Settings, "Settings");
        }
        // logs out all the settings and their values at runtime
        internal static void PrintObjectFields(object obj, string name)
        {
            LogDebug($"[START {name}]");

            var settingsFields = typeof(ModSettings)
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in settingsFields)
            {
                if (field.GetValue(obj) is IEnumerable &&
                    !(field.GetValue(obj) is string))
                {
                    LogDebug(field.Name);
                    foreach (var item in (IEnumerable)field.GetValue(obj))
                    {
                        LogDebug("\t" + item);
                    }
                }
                else
                {
                    LogDebug($"{field.Name,-30}: {field.GetValue(obj)}");
                }
            }

            LogDebug($"[END {name}]");
        }

        #endregion

        internal static ModSettings Settings;

    }
    
        
    [HarmonyPatch(typeof(SimGameState), "SetExpenditureLevel")]
    public static class Adjust_Techs_Financial_Report_Patch
    {
    public static SaveFields SaveFields;
    public static void Prefix(SimGameState __instance, bool updateMorale, List<TemporarySimGameResult> ___TemporaryResultTracker)
        {
            var settings = Pre_Control.Settings;
            if (updateMorale)
            {
                var expenseLevel = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
                    
                if (expenseLevel < 0)
                {
                    expenseLevel = expenseLevel * 2;
                }
                int dMechTech = expenseLevel * settings.MechTechScale;
                int dMedTech = expenseLevel;

                if (!settings.AdjustTechs)
                {
                    dMechTech = 0;
                    dMedTech = 0;
                }

                var StartingMechTech = __instance.CompanyStats.GetValue<int>("MechTechSkill");
                var StartingMedTech = __instance.CompanyStats.GetValue<int>("MedTechSkill");
                var MechTChange = StartingMechTech + dMechTech;
                var MedTChange = StartingMedTech + dMedTech;

                if (MechTChange < 1)
                    MechTChange = 1;
                if (MedTChange < 1)
                    MedTChange = 1;

                __instance.CompanyStats.Set<int>("MechTechSkill", MechTChange);
                __instance.CompanyStats.Set<int>("MedTechSkill", MedTChange);

                Fields.DeltaMechTech = StartingMechTech - MechTChange;
                Fields.DeltaMedTech = StartingMedTech - MedTChange;
                Fields.ExpenseLevel = __instance.CompanyStats.GetValue<int>("ExpenseLevel");


                SaveFields = new SaveFields(Fields.ExpenseLevel, Fields.DeltaMechTech, Fields.DeltaMedTech);
                SaveHandling.SerializeMTMA();


                //Pilot Quirk area
                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_noble") && settings.QuirksEnabled)
                    {
                        if (Fields.ExpenseLevel == 2)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_high");

                            var eventTagSet = new TagSet();

                            Traverse.Create(eventTagSet).Field("items").SetValue(new string[] { "pilot_morale_high" });
                            Traverse.Create(eventTagSet).Field("tagSetSourceFile").SetValue("Tags/PilotTags");
                            Traverse.Create(eventTagSet).Method("UpdateHashCode").GetValue();

                            var EventTime = new TemporarySimGameResult();
                            EventTime.ResultDuration = 30;
                            EventTime.Scope = EventScope.MechWarrior;
                            EventTime.TemporaryResult = true;
                            EventTime.AddedTags = eventTagSet;
                            Traverse.Create(EventTime).Field("targetPilot").SetValue(pilot);

                            Traverse.Create(__instance).Method("AddOrRemoveTempTags", new[] { typeof(TemporarySimGameResult), typeof(bool) }).
                                GetValue(EventTime, true);
                            ___TemporaryResultTracker.Add(EventTime);
                        }
                        else if (Fields.ExpenseLevel < 0)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_low");

                            var eventTagSet = new TagSet();

                            Traverse.Create(eventTagSet).Field("items").SetValue(new string[] { "pilot_morale_low" });
                            Traverse.Create(eventTagSet).Field("tagSetSourceFile").SetValue("Tags/PilotTags");
                            Traverse.Create(eventTagSet).Method("UpdateHashCode").GetValue();

                            var EventTime = new TemporarySimGameResult();
                            EventTime.ResultDuration = 30;
                            EventTime.Scope = EventScope.MechWarrior;
                            EventTime.TemporaryResult = true;
                            EventTime.AddedTags = eventTagSet;
                            Traverse.Create(EventTime).Field("targetPilot").SetValue(pilot);

                            Traverse.Create(__instance).Method("AddOrRemoveTempTags", new[] { typeof(TemporarySimGameResult), typeof(bool) }).
                                GetValue(EventTime, true);
                            ___TemporaryResultTracker.Add(EventTime);
                        }
                    }
                }

                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    var rng = new System.Random();
                    int Roll = rng.Next(1, 100);
                    if (pilot.pilotDef.PilotTags.Contains("pilot_dishonest") && settings.QuirksEnabled)
                    {
                        if (Roll <= 33)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_drunk");
                            pilot.pilotDef.PilotTags.Remove("pilot_unstable");
                            pilot.pilotDef.PilotTags.Remove("pilot_rebellious");
                        }
                        else if (Roll > 33 && Roll <= 66)
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_drunk");
                            pilot.pilotDef.PilotTags.Add("pilot_unstable");
                            pilot.pilotDef.PilotTags.Remove("pilot_rebellious");
                        }
                        else
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_drunk");
                            pilot.pilotDef.PilotTags.Remove("pilot_unstable");
                            pilot.pilotDef.PilotTags.Add("pilot_rebellious");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SGMechTechPointsDisplay), "Refresh")]
    public static class SGMechTechPointsDisplay_Refresh_Patch
    {
        public static void Postfix(SGMechTechPointsDisplay __instance)
        {
            // optionally changing the number displayed but not the value
            //Color colorRef;
            //__instance.NumMechTechText.SetText("{0}", __instance.simState.GetCompanyModifiedInt("MechTechSkill", out colorRef) / 1000f);
            var settings = Pre_Control.Settings;
            __instance.NumMechTechText.fontSize = settings.fontsize;
            __instance.NumMechTechText.enableWordWrapping = false;
        }
    }

    [HarmonyPatch(typeof(SGMedTechPointsDisplay), "Refresh")]
    public static class SGMedTechPointsDisplay_Refresh_Patch
    {
        public static void Postfix(SGMedTechPointsDisplay __instance)
        {
            // optionally changing the number displayed but not the value
            //Color colorRef;
            //__instance.NumMechTechText.SetText("{0}", __instance.simState.GetCompanyModifiedInt("MechTechSkill", out colorRef) / 1000f);
            var settings = Pre_Control.Settings;
            __instance.NumMedTechText.fontSize = settings.fontsize;
            __instance.NumMedTechText.enableWordWrapping = false;
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnNewQuarterBegin")]
    public static class Reset_State_Patch
    {
        public static void Postfix(SimGameState __instance)
        {
            var settings = Pre_Control.Settings;
            var expenselevel = Fields.ExpenseLevel;
            int MoraleChange = 0;
            if (expenselevel == -2)
                MoraleChange = __instance.Constants.Story.SpartanMoraleModifier;
            if (expenselevel == -1)
                MoraleChange = __instance.Constants.Story.RestrictedMoraleModifier;
            if (expenselevel == 0)
                MoraleChange = __instance.Constants.Story.NormalMoraleModifier;
            if (expenselevel == 1)
                MoraleChange = __instance.Constants.Story.GenerousMoraleModifier;
            if (expenselevel == 2)
                MoraleChange = __instance.Constants.Story.ExtravagantMoraleModifier;

            int newMechValue = __instance.CompanyStats.GetValue<int>("MechTechSkill") + Fields.DeltaMechTech;
            int newMedValue = __instance.CompanyStats.GetValue<int>("MedTechSkill") + Fields.DeltaMedTech;
            int newMoraleValue = __instance.CompanyStats.GetValue<int>("Morale") - MoraleChange;

            __instance.CompanyStats.Set<int>("MechTechSkill", newMechValue);
            __instance.CompanyStats.Set<int>("MedTechSkill", newMedValue);
            __instance.CompanyStats.Set<int>("Morale", newMoraleValue);

        }
    }
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class Update_UI
    {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel)
        {
            var settings = Pre_Control.Settings;
            var moraleFields = Traverse.Create(__instance).Field("ExpenditureLvlBtnMoraleFields").GetValue<List<LocalizableText>>();
            var SimGameTrav = Traverse.Create(__instance).Field("simState").GetValue<SimGameState>();
            var sim = UnityGameInstance.BattleTechGame.Simulation;

            int num3 = 0;
            if (settings.AdjustTechs)
            {
                string ChangeComb = "";
                foreach (KeyValuePair<EconomyScale, int> keyValuePair in SimGameTrav.ExpenditureMoraleValue)
                {
            
                    SimGameTrav.SetExpenditureLevel(keyValuePair.Key, false);
                    int TechNum = num3 - 2;
                    if (TechNum == -2)
                    {
                        ChangeComb = keyValuePair.Value.ToString() + ", " + (-4).ToString() + " Techs";
                        moraleFields[num3].text = ChangeComb;
                    }
                    else if (TechNum <= 0 && TechNum != -2)
                    {
                        ChangeComb = keyValuePair.Value.ToString() + ", " + (2 * TechNum).ToString() + " Techs";
                        moraleFields[num3].text = ChangeComb;
                    }
                    else
                    {
                        ChangeComb = "+" + keyValuePair.Value.ToString() + ", +" + TechNum.ToString() + " Techs";
                        moraleFields[num3].text = ChangeComb;
                    }
                    num3++;
                }
             
                SimGameTrav.SetExpenditureLevel(expenditureLevel, false);

                int ELMV = SimGameTrav.ExpenditureMoraleValue[SimGameTrav.ExpenditureLevel];
                int skillnum = 0;
           
                if (ELMV == sim.Constants.Story.SpartanMoraleModifier)
                    skillnum = -4;
                if (ELMV == sim.Constants.Story.RestrictedMoraleModifier)
                    skillnum = -2;
                if (ELMV == sim.Constants.Story.NormalMoraleModifier)
                    skillnum = 0;
                if (ELMV == sim.Constants.Story.GenerousMoraleModifier)
                    skillnum = 1;
                if (ELMV == sim.Constants.Story.ExtravagantMoraleModifier)
                    skillnum = 2;
             
                string MString = ELMV.ToString();
                if (ELMV > 0)
                {
                    MString = "+" + MString;
                }
                string SString = skillnum.ToString() + " Techs";
                if (skillnum > 0)
                {
                    SString = "+" + SString;
                }
               
                string ModField = MString + ", " + SString;
                var instance = Traverse.Create(__instance);
                var moraleFieldValue = instance.Field("MoraleValueField").GetValue<TextMeshProUGUI>();
                instance.Method("SetField", new Type[] { typeof(LocalizableText), typeof(string) }, new object[] { moraleFieldValue, ModField }).GetValue();
            }
        }
    }
}