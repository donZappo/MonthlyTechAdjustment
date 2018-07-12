using System;
using System.Reflection;
using BattleTech;
using Harmony;
using BattleTech.UI;
using Newtonsoft;
using Newtonsoft.Json;
using System.IO;


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
            if (updateMorale)
            {
                int valuee = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
                if (valuee < 0)
                {
                    valuee = valuee * 2;
                }
                int num = valuee * 1000;
                int num2 = valuee;
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num, -1, true);
                __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2, -1, true);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnNewQuarterBegin")]
    public static class Reset_State_Patch
    {
        public static void Postfix(SimGameState __instance)
        {
            int valuee = __instance.CompanyStats.GetValue<int>("ExpenseLevel");
            int MoraleChange = 5 * valuee;
            if (valuee < 0)
            {
                valuee = valuee * 2;
            }
            int num = -valuee * 1000;
            int num2 = -valuee;
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MechTechSkill", StatCollection.StatOperation.Int_Add, num, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "MedTechSkill", StatCollection.StatOperation.Int_Add, num2, -1, true);
            __instance.CompanyStats.ModifyStat<int>("SimGame", 0, "Morale", StatCollection.StatOperation.Int_Subtract, MoraleChange, -1, true);
        }
    }
}