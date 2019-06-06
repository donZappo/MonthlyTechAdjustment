using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BattleTech;

namespace MonthlyTechandMoraleAdjustment
{
    public static class Fields
    {

        public static int ExpenseLevel = 0;
        public static int DeltaMechTech = 0;
        public static int DeltaMedTech = 0;
        public static int StartingMechTech = UnityGameInstance.BattleTechGame.Simulation.CompanyStats.GetValue<int>("MechTechSkill");
        public static int StartingMedTech = UnityGameInstance.BattleTechGame.Simulation.CompanyStats.GetValue<int>("MedTechSkill");
        
    }
}
