﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PD_Helper
{
    //Class for PDCard
    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<PDCard>>(myJsonResponse);
    public class PDCard : IComparable
    {
        // JSON Properties
        [JsonProperty("NAME")]
        public string NAME { get; set; }
        [JsonProperty("ID")]
        public int? ID { get; set; }
        [JsonProperty("SCHOOL")]
        public string SCHOOL { get; set; }
        [JsonProperty("DAMAGE")]
        public string DAMAGE { get; set; }
        [JsonProperty("COST")]
        public string COST { get; set; }
        [JsonProperty("USAGE")]
        public string USAGE { get; set; }
        [JsonProperty("RANGE")]
        public string RANGE { get; set; }
        [JsonProperty("DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [JsonProperty("TYPE")]
        public string TYPE { get; set; }
        [JsonProperty("HEX")]
        public string HEX { get; set; }

        #region Comparators
        // Comparator(s)
        public class SortTypeHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                int typeIntA = a.TypeToInt();
                int typeIntB = b.TypeToInt();

                if (typeIntA > typeIntB) return 1;
                else if (typeIntA < typeIntB) return -1;
                else return 0;
            }
        }

        public class SortSchoolHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                int schoolIntA = a.SchoolToInt();
                int schoolIntB = b.SchoolToInt();

                if (schoolIntA > schoolIntB) return 1;
                else if (schoolIntA < schoolIntB) return -1;
                else return 0;
            }
        }

        public class SortCostHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                bool parseA = int.TryParse(a.COST, out int costA);
                bool parseB = int.TryParse(b.COST, out int costB);
                if (parseA && parseB)
                {
                    if (costA > costB) return 1;
                    else if (costA < costB) return -1;
                    else return 0;
                }
                else if (!parseA && parseB) return 1;
                else if (parseA && !parseB) return -1;
                else return 0;
            }
        }

        public class SortStrHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
				// Only do this sort if they are the same type
				if (a.TYPE != b.TYPE)
				{
                    // Type comparison
                    int typeIntA = a.TypeToInt();
                    int typeIntB = b.TypeToInt();

                    if (typeIntA > typeIntB) return 1;
                    else if (typeIntA < typeIntB) return -1;
                    else return 0;
                }

                // If these are attack/defense then proceed
                if (a.TYPE != "Attack" && a.TYPE != "Defense") return 0;
                
                // Sorting when both a and b are the same type and are attack/defense
                bool parseA = int.TryParse(a.DAMAGE, out int strA);
                bool parseB = int.TryParse(b.DAMAGE, out int strB);
                if (parseA && parseB)
                {
                    if (strA > strB) return -1;
                    else if (strA < strB) return 1;
                    else return 0;
                }
                else if (!parseA && parseB) return 1;
                else if (parseA && !parseB) return -1;
                else return 0;
            }
        }

        public class SortUsesHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                bool parseA = int.TryParse(a.USAGE, out int useA);
                bool parseB = int.TryParse(b.USAGE, out int useB);
                if (parseA && parseB)
                {
                    if (useA > useB) return 1;
                    else if (useA < useB) return -1;
                    else return 0;
                }
                else if (!parseA && parseB) return -1;
                else if (parseA && !parseB) return 1;
                else return 0;
            }
        }

        public class SortRangeHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                int rangeIntA = a.RangeToInt();
                int rangeIntB = b.RangeToInt();

                if (rangeIntA > rangeIntB) return 1;
                else if (rangeIntA < rangeIntB) return -1;
                else return 0;
            }
        }

        public class SortIDHelper : IComparer<PDCard>
        {
            int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
                if (a.ID > b.ID) return 1;
                else if (a.ID < b.ID) return -1;
                else return 0;
            }
        }

        public class SortMultiHelper : IComparer<PDCard>
        {
            IComparer<PDCard>[] comparers;

            public SortMultiHelper(IComparer<PDCard>[] comparers)
			{
				this.comparers = comparers;
			}

			int IComparer<PDCard>.Compare(PDCard a, PDCard b)
            {
				foreach (var item in comparers)
				{
                    int result = item.Compare(a, b);
                    if (result != 0) return result;
				}

                return string.Compare(a.HEX, b.HEX);
            }
        }

        // Default Comparison
        int IComparable.CompareTo(object? obj)
        {
            PDCard card = (PDCard)obj;
            return string.Compare(this.HEX, card.HEX);
        }

        // Return Comparators
        public static IComparer<PDCard> SortType() => new SortTypeHelper();
        public static IComparer<PDCard> SortSchool() => new SortSchoolHelper();
        public static IComparer<PDCard> SortCost() => new SortCostHelper();
        public static IComparer<PDCard> SortStr() => new SortStrHelper();
        public static IComparer<PDCard> SortUses() => new SortUsesHelper();
        public static IComparer<PDCard> SortRange() => new SortRangeHelper();
        public static IComparer<PDCard> SortID() => new SortIDHelper();

        public static IComparer<PDCard> SortMulti(IComparer<PDCard>[] comparers) 
            => new SortMultiHelper(comparers);

        // Type to int for the purpose of ordering
        public int TypeToInt()
        {
            switch (TYPE)
            {
                case "Attack":
                    return 0;
                case "Defense":
                    return 1;
                case "Erase":
                    return 2;
                case "Status":
                    return 3;
                case "Special":
                    return 4;
                case "Environment":
                    return 5;
                default:
                    return 6;
            }
        }

        public int SchoolToInt()
        {
            switch (SCHOOL)
            {
                case "Psycho":
                    return 0;
                case "Optical":
                    return 1;
                case "Nature":
                    return 2;
                case "Ki":
                    return 3;
                case "Faith":
                    return 4;
                default:
                    return -1; // Aura
            }
        }

        public int RangeToInt()
        {
			switch (RANGE)
			{
                case "short":
                    return 0;
                case "medium":
                    return 1;
                case "long":
                    return 2;
                case "mine":
                    return 3;
                case "capsule":
                    return 4;
                case "-":
                    return 5;
                case "self":
                    return 6;
                case "all":
                    return 7;
                case "auto":
                    return 8;
                case "env":
                    return 9;
                default:
                    return 10;
            }
		}

        public static IComparer<PDCard>? DetermineSort(string sort)
        {
            /*
             * School
             * Cost
             * Strength
             * Number of Uses
             * Range
             * ID
             * None
			 */

            switch (sort)
            {
                case "School":
                    return SortSchool();
                case "Cost":
                    return SortCost();
                case "Strength":
                    return SortStr();
                case "Number of Uses":
                    return SortUses();
                case "Range":
                    return SortRange();
                case "ID":
                    return SortID();
                default:
                    return null;
            }
        }

        #endregion

        // TODO: Add SchoolToEnum accessor
        public enum School
        {
            Psycho,
            Optical,
            Nature,
            Ki,
            Faith,
            Aura = -1
        }

        // Card Database: Takes hex and returns card.
        public static Dictionary<string, PDCard> cardDef = JsonConvert.DeserializeObject<Dictionary<string, PDCard>>(File.ReadAllText("SkillDB.json"));

        public static PDCard CardFromName(string name)
        {
            foreach (PDCard card in cardDef.Values)
            {
                if (card.NAME == name)
                {
                    return card;
                }
            }

            return null;
        }

        public static School SchoolFromName(string name) => (School)CardFromName(name).SchoolToInt();
    }
}
