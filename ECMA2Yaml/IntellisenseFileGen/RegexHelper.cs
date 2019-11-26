using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IntellisenseFileGen
{
    public class RegexHelper
    {
        public static string[] GetMatches_All_JustWantedOne(Regex regex, string input)
        {
            List<string> strList = new List<string>();
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            MatchCollection matches = GetMatch(regex, input);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            strList.Add(groups[i].Value);
                        }
                    }
                }
            }

            return strList.ToArray();
        }

        private static MatchCollection GetMatch(Regex regex, string input)
        {
            if (regex.IsMatch(input))
            {
                return regex.Matches(input);
            }
            return null;
        }
    }
}
