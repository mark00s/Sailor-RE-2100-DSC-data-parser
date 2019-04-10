using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSc_Thesis.Models
{
    class DistressDataResolver
    {
        public string ResolveCategory(string code)
        {
            if (code == "112")
                return "Danger (alarm)";
            if (code == "110")
                return "Urgency";
            if (code == "108")
                return "Safety";
            if (code == "106")
                return "Ship's interests";
            if (code == "100")
                return "Routine call";
            return code;
        }

        public string ResolveEndOfSequence(string code)
        {
            if (code == "117")
                return "RQ Acknowledge required";
            else if (code == "122")
                return "BQ Acknowledge respond";
            else if (code == "127")
                return "Other calls";
            return code;
        }

        public string ResolveDistressCode(string code)
        {
            switch (code) {
                case "100":
                    return "Fire, explosion";
                case "101":
                    return "Flooding";
                case "102":
                    return "Colision";
                case "103":
                    return "Grounding";
                case "104":
                    return "Listing, capsizing";
                case "105":
                    return "Sinking";
                case "106":
                    return "Disable and adrift";
                case "107":
                    return "Undesined distress";
                case "108":
                    return "Abandoning ship";
                case "112":
                    return "EPIRB emision";
            }
            return code;
        }
    }
}
