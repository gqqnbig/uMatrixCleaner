using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace uMatrixCleaner
{
    public class Program
    {
        static void Main(string[] args)
        {
            Clean(System.IO.File.ReadAllText(@"D:\Documents\Visual Studio 2017\Projects\uMatrixCleaner\test.txt"));

        }


        static string Clean(string input)
        {
            var rules = from line in input.Split("\r\n")
                        where line.Length > 0 && line.StartsWith("matrix-off") == false && line.StartsWith("noscript-spoof") == false
                        select new UMatrixRule(line);


            rules.Count();





            throw new NotImplementedException();
        }



        public static UMatrixRule GetClosestParent(UMatrixRule rule, ICollection<UMatrixRule> list)
        {
            var closestParent = list.Where(r => r.Equals(rule) == false && r.Covers(rule).GetValueOrDefault(true))
                                .OrderByDescending(r => r.Specificity).FirstOrDefault();
            return closestParent;
        }
    }
}
