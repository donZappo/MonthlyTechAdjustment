using System;
using System.Reflection;
using BattleTech;
using Harmony;
using BattleTech.UI;
using Newtonsoft;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;


namespace MonthlyTechandMoraleAdjustment
{
    public static class Pre_Control
    {
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJson)
        {
            HarmonyInstance.Create("dZ.Zappo.MonthlyTechAdjustment").PatchAll(Assembly.GetExecutingAssembly());
            ModDirectory = directory;
        }
        
    }
    [HarmonyPatch(typeof(GameInstanceSave))]
    [HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    public static class GameInstanceSave_Constructor_Patch
    {
        static void Postfix(GameInstanceSave __instance)
        {
            HelperHelper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
        }
    }

    [HarmonyPatch(typeof(GameInstance), "Load")]
    public static class GameInstance_Load_Patch
    {
        static void Prefix(GameInstanceSave save)
        {
            HelperHelper.LoadState(save.InstanceGUID, save.SaveTime);
        }
    }
    [HarmonyPatch(typeof(SimGameState), "SetExpenditureLevel")]
    public static class Adjust_Techs_Financial_Report_Patch
    {
        public static void Prefix(SimGameState __instance, bool updateMorale)
        {
            Settings settings = Helper.LoadSettings();
            if (updateMorale)
            {
                Fields.ExpenseLevel = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
                int Expenses = Fields.ExpenseLevel;
                if (Expenses < 0)
                {
                    Expenses = Expenses * 2;
                }
                int num = Expenses * settings.MechTechScale;
                int num2 = Expenses;
                if (!settings.AdjustTechs)
                {
                    num = 0;
                    num2 = 0;
                }

                __instance.CompanyStats.GetValue<int>("MechTechSkill");
                var MechTechSkillStart = __instance.CompanyStats.GetValue<int>("MechTechSkill");
                var MedTechSkillStart = __instance.CompanyStats.GetValue<int>("MedTechSkill");

                if (MechTechSkillStart + num < 1)
                {
                    num = -(MechTechSkillStart - settings.MechTechScale);
                }

                if (MedTechSkillStart + num2 < 1)
                {
                    num2 = -(MedTechSkillStart - 1);
                }
                
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num, -1, true);
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2, -1, true);

                Fields.DeltaMechTech = num;
                Fields.DeltaMedTech = num2;

                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_noble") && settings.QuirksEnabled)
                    {
                        if (Fields.ExpenseLevel == 2)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_high");
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_low");
                        }
                        else if (Fields.ExpenseLevel < 0)
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_high");
                            pilot.pilotDef.PilotTags.Add("pilot_morale_low");
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
                            pilot.pilotDef.PilotTags.Remove("pilot_criminal");
                        }
                        else if (Roll > 33 && Roll <= 66)
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_drunk");
                            pilot.pilotDef.PilotTags.Add("pilot_unstable");
                            pilot.pilotDef.PilotTags.Remove("pilot_criminal");
                        }
                        else
                        {
                            pilot.pilotDef.PilotTags.Remove("pilot_drunk");
                            pilot.pilotDef.PilotTags.Remove("pilot_unstable");
                            pilot.pilotDef.PilotTags.Add("pilot_criminal");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnNewQuarterBegin")]
    public static class Reset_State_Patch
    {
        public static void Postfix(SimGameState __instance)
        {
            Settings settings = Helper.LoadSettings();
            int valuee = Fields.ExpenseLevel;
            int MoraleChange = 5 * valuee;
            if (valuee < 0)
            {
                valuee = Fields.ExpenseLevel * 2;
            }
            //int num = -valuee * settings.MechTechScale;
            //int num2 = -valuee;
            int num = -Fields.DeltaMechTech;
            int num2 = -Fields.DeltaMedTech;

            

            if (!settings.AdjustTechs)
            {
                num = 0;
                num2 = 0;
            }

            int FixMorale = 0;
            int FixMechTech = 0;
            int FixMedTech = 0;

            if(settings.FixSavedGame)
            {
                FixMorale = settings.FixMorale;
                FixMechTech = settings.FixMechtech;
                FixMedTech = settings.FixMedTech;
            }


            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num + FixMechTech, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2 + FixMedTech, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, MoraleChange - FixMorale, -1, true);


        }
    }
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class Update_UI
    {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance)
        {
            Settings settings = Helper.LoadSettings();
            var moraleFields = Traverse.Create(__instance).Field("ExpenditureLvlBtnMoraleFields").GetValue<List<TextMeshProUGUI>>();
            var SimGameTrav = Traverse.Create(__instance).Field("simState").GetValue<SimGameState>();
            EconomyScale expenditureLevel = SimGameTrav.ExpenditureLevel;
            int num3 = 0;
            if (settings.AdjustTechs)
            {
                string ChangeComb = "";
                foreach (KeyValuePair<EconomyScale, int> keyValuePair in SimGameTrav.ExpenditureMoraleValue)
                {
                    SimGameTrav.SetExpenditureLevel(keyValuePair.Key, false);
                    int TechNum = num3 - 2;
                    if (TechNum <= 0)
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

                int num = SimGameTrav.ExpenditureMoraleValue[SimGameTrav.ExpenditureLevel];
                int skillnum = num / 5;
                if (skillnum < 0)
                {
                    skillnum = skillnum * 2;
                }
                string MString = num.ToString();
                if (num > 0)
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
                instance.Method("SetField", new Type[] { typeof(TextMeshProUGUI), typeof(string) }, new object[] { moraleFieldValue, ModField }).GetValue();
            }
        }
    }
    public class Helper
    {
        // Token: 0x06000001 RID: 1 RVA: 0x0000208C File Offset: 0x0000028C
        public static Settings LoadSettings()
        {
            Settings result;
            try
            {
                using (StreamReader streamReader = new StreamReader("mods/MonthlyTechandMoraleAdjustment/settings.json"))
                {
                    result = JsonConvert.DeserializeObject<Settings>(streamReader.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                result = null;
            }
            return result;
        }
        public class Logger
        {
            // Token: 0x06000007 RID: 7 RVA: 0x0000210C File Offset: 0x0000030C
            public static void LogError(Exception ex)
            {
                using (StreamWriter streamWriter = new StreamWriter("mods/MonthlyTechandMoraleAdjustment/Log.txt", true))
                {
                    streamWriter.WriteLine(string.Concat(new string[]
                    {
                    "Message :",
                    ex.Message,
                    "<br/>",
                    Environment.NewLine,
                    "StackTrace :",
                    ex.StackTrace,
                    Environment.NewLine,
                    "Date :",
                    DateTime.Now.ToString()
                    }));
                    streamWriter.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                }
            }

            // Token: 0x06000008 RID: 8 RVA: 0x000021C0 File Offset: 0x000003C0
            public static void LogLine(string line)
            {
                string path = "mods/MonthlyTechandMoraleAdjustment/Log.txt";
                using (StreamWriter streamWriter = new StreamWriter(path, true))
                {
                    streamWriter.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                    streamWriter.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
                }
            }
        }
    }
    public class Settings
    {
        public bool AdjustTechs = true;
        public int MechTechScale = 1;
        public bool QuirksEnabled = false;

        public bool FixSavedGame = false;
        public int FixMorale = 0;
        public int FixMechtech = 0;
        public int FixMedTech = 0;
    }
}