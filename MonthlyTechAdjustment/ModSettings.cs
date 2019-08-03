using System.Collections.Generic;
using BattleTech;

namespace MonthlyTechandMoraleAdjustment
{
    public class ModSettings
    {
        public bool Debug = false;
        public string modDirectory;

        public bool AdjustTechs = true;
        public int MechTechScale = 1;
        public bool QuirksEnabled = false;

        public int fontsize = 20;
    }
}
