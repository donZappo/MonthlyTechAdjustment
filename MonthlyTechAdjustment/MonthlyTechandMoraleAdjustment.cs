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


namespace MonthlyTechandMoraleAdjustment
{
    public static class Pre_Control
    {
        public static void Init()
        {
            HarmonyInstance.Create("dZ.Zappo.MonthlyTechAdjustment").PatchAll(Assembly.GetExecutingAssembly());
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
                int valuee = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
                if (valuee < 0)
                {
                    valuee = valuee * 2;
                }
                int num = valuee * settings.MechTechScale;
                int num2 = valuee;
                if (!settings.AdjustTechs)
                {
                    num = 0;
                    num2 = 0;
                }
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num, -1, true);
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2, -1, true);

                foreach (Pilot pilot in __instance.PilotRoster)
                {
                    if (pilot.pilotDef.PilotTags.Contains("pilot_noble") && settings.QuirksEnabled)
                    {
                        if (valuee == 2)
                        {
                            pilot.pilotDef.PilotTags.Add("pilot_morale_high");
                            pilot.pilotDef.PilotTags.Remove("pilot_morale_low");
                        }
                        else if (valuee < 0)
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
            int valuee = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
            int MoraleChange = 5 * valuee;
            if (valuee < 0)
            {
                valuee = valuee * 2;
            }
            int num = -valuee * settings.MechTechScale;
            int num2 = -valuee;
            if (!settings.AdjustTechs)
            {
                num = 0;
                num2 = 0;
            }
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, MoraleChange, -1, true);

            
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
                        ChangeComb = keyValuePair.Value.ToString() + ", " + (2 * TechNum).ToString() + " Tech";
                        moraleFields[num3].text = ChangeComb;
                    }
                    else
                    {
                        ChangeComb = "+" + keyValuePair.Value.ToString() + ", +" + TechNum.ToString() + " Tech";
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
                string SString = skillnum.ToString() + " Tech";
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
    }
}