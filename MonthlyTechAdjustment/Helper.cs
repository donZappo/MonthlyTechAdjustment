using BattleTech;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

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

    public class HelperHelper
    {
        public static void SaveState(string instanceGUID, DateTime saveTime)
        {
            try
            {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string filePath = $"Mods/MonthlyTechandMoraleAdjustment/saves/" + instanceGUID + "-" + unixTimestamp + ".json";
                (new FileInfo(filePath)).Directory.Create();
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    SaveFields fields = new SaveFields(Fields.ExpenseLevel, Fields.DeltaMechTech, Fields.DeltaMedTech);
                    string json = JsonConvert.SerializeObject(fields);
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                Helper.Logger.LogError(ex);
            }
        }

        public static void LoadState(string instanceGUID, DateTime saveTime)
        {
            try
            {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string filePath = $"Mods/MonthlyTechandMoraleAdjustment/saves/" + instanceGUID + "-" + unixTimestamp + ".json";
                if (File.Exists(filePath))
                {
                    using (StreamReader r = new StreamReader(filePath))
                    {
                        string json = r.ReadToEnd();
                        SaveFields save = JsonConvert.DeserializeObject<SaveFields>(json);
                        Fields.ExpenseLevel = save.ExpenseLevel;
                        Fields.DeltaMechTech = save.DeltaMechTech;
                        Fields.DeltaMedTech = save.DeltaMedTech;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.Logger.LogError(ex);
            }
        }
    }
}