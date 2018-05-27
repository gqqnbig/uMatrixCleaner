using System;
using System.Diagnostics;
using System.Linq;

namespace uMatrixCleaner
{
    class Program
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
    }

    [DebuggerDisplay("{Source, nq} {Target, nq} {Type, nq} {IsAllow?\"allow\":\"block\", nq}")]
    class UMatrixRule
    {
        public string Source { get; set; }

        public string Target { get; set; }

        public Type Type { get; set; }

        public bool IsAllow { get; set; }

        public UMatrixRule(string line)
        {
            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Source = parts[0];
            Target = parts[1];
            Type = parts[2] == "*" ? Type.All : (Type)Enum.Parse(typeof(Type), parts[2], true);
            IsAllow = parts[3] == "allow";
        }
    }

    [Flags]
    enum Type
    {
        Cookie = 1,
        Css = 2,
        Image = 4,
        Media = 8,
        Script = 16,
        Xhr = 32,
        Frame = 64,
        Other = 128,
        All = 255

    }


}
