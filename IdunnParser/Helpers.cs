using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IdunnParser
{
    public class Helpers
    {
        static public string CleanOfWhitespace(string line)
        {
            return System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ");
        }

        /// <summary>
        /// this return the string pretty formated (line break at { } etc...)
        /// </summary>
        /// <param name="inStr"></param>
        /// <returns></returns>
        static public string PrettyFormat(string inText)
        {
            string inStr = CleanOfWhitespace(inText); 

            int level = 1;
            string outStr = "";
            outStr += inStr[0] + jumpLine(level);
            for (int i = 1; i < inStr.Length-1; ++i)
            {
                if (
                    (inStr[i] == '{' || inStr[i] == '}' || inStr[i] == '[' || inStr[i] == ']'))
                {
                    if (inStr[i] == '{' || inStr[i] == '[')
                    {
                        outStr += jumpLine(level);
                        level += 1;
                    }
                    else
                    {
                        level -= 1;
                        outStr += jumpLine(level);
                    }

                    outStr += inStr[i] + jumpLine(level);
                }
                else if (inStr[i] == ',' && inStr[i + 1] != '\n')
                {
                    outStr += inStr[i];
                    outStr += jumpLine(level);
                }
                else
                    outStr += inStr[i];
            }

            outStr += "\n}";

            return outStr;
        }

        static protected string jumpLine(int level)
        {
            string outstr = "";

            outstr += '\n';
            for (int i = 0; i < level; ++i)
                outstr += '\t';

            return outstr;
        }
    }
}
