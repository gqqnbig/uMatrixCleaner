using System;
using System.Diagnostics;
using System.Linq;
using Nager.PublicSuffix;

namespace uMatrixCleaner
{
    public class UMatrixRule
    {
        internal static readonly DomainParser domainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));

        /// <summary>
        /// 在一般化过程中保存原始规则
        /// </summary>
        private UMatrixRule originalRule;

        public HierarchicalUrl Source { get; }

        public HierarchicalUrl Destination { get; }

        public DataType Type { get; }

        public bool IsAllow { get; }

        private int specificity = -1;

        /// <summary>
        /// 表示此规则的具体性。数值越高，规则越具体。
        /// </summary>
        public int Specificity
        {
            get
            {
                if (specificity == -1)
                    specificity = Source.Specificity * 100 + Destination.Specificity * 10 + (Type == DataType.All ? 0 : 1);
                return specificity;
            }
        }

        public UMatrixRule(HierarchicalUrl source, HierarchicalUrl destination, DataType type, bool isAllow)
        {
            if (HierarchicalUrl.N1stParty.Equals(source))
                throw new ArgumentException("source不能为1st-party。");

            Source = source;
            Destination = destination;
            Type = type;
            IsAllow = isAllow;
        }

        private UMatrixRule(HierarchicalUrl source, HierarchicalUrl destination, DataType type, bool isAllow, UMatrixRule original) : this(source, destination, type, isAllow)
        {
            this.originalRule = original;
        }


        public UMatrixRule(string line)
        {
            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Source = new HierarchicalUrl(parts[0]);
            Destination = new HierarchicalUrl(parts[1]);
            Type = parts[2] == "*" ? uMatrixCleaner.DataType.All : (DataType)Enum.Parse(typeof(DataType), parts[2], true);
            IsAllow = parts[3] == "allow";
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            var other = obj as UMatrixRule;
            if (other == null)
                return false;

            return Source == other.Source && Destination == other.Destination && Type == other.Type && IsAllow == other.IsAllow;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Source?.GetHashCode() ?? 0 ^ Destination?.GetHashCode() ?? 0 ^ Type.GetHashCode() ^ IsAllow.GetHashCode();
        }

        public override string ToString()
        {
            var typeString = Type == DataType.All ? "*" : Type.ToString().ToLower();
            return $"{Source} {Destination} {typeString} {(IsAllow ? "allow" : "block")}";
        }

        /// <summary>
        /// 来源、目标、类型是否包含另一个规则。不考虑动作。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Contains(UMatrixRule other)
        {
            if (Type.HasFlag(other.Type) == false)
                return false;

            if (Destination.Value == HierarchicalUrl.N1stParty.Value && other.Source.IsUrl && other.Destination.IsUrl && other.Destination.Value.EndsWith(other.Source.Value) == false)
                return false;

            var a = Source.Covers(other.Source);
            Debug.Assert(a != null, "Source.Contains()不能返回null。");

            if (Source.IsUrl && other.Source.IsUrl && a == false)
                return false;


            var b = Destination.Covers(other.Destination);

            return a.GetValueOrDefault(true) || b.GetValueOrDefault(true);
        }

        /// <summary>
        /// 返回一个更一般化的规则。如果本规则已经是顶级规则，则返回null。
        /// <para>返回的规则的<see cref="Specificity"/>小于本规则的<see cref="Specificity"/>。</para>
        /// </summary>
        /// <returns></returns>
        public UMatrixRule Generalize()
        {
            if (Type != DataType.All)
                return new UMatrixRule(Source, Destination, DataType.All, IsAllow, originalRule ?? this);


            Debug.Assert(Source.Value != "1st-party");

            //https://github.com/gorhill/uMatrix/wiki/Changes-from-HTTP-Switchboard#no-more-restriction-on-effective-domain-boundaries
            //URL可以到顶级域，但我这里不允许。

            string generalizedDestination;
            if (Destination.Value.Count(c => c == '.') > 1 && string.IsNullOrEmpty(domainParser.Get(Destination).SubDomain) == false)
                generalizedDestination = Destination.Value.Substring(Destination.Value.IndexOf('.') + 1);
            else if (Destination.Value == Source.Value && Destination.Value != "*")
                generalizedDestination = "1st-party";
            else if (Destination.Value != "*")
                generalizedDestination = "*";
            else
            {
                Debug.Assert(Destination.Value == "*");
                generalizedDestination = null;
            }

            if (generalizedDestination != null)
                return new UMatrixRule(Source, new HierarchicalUrl(generalizedDestination), (originalRule ?? this).Type, IsAllow, originalRule ?? this);


            string generalizedSource;
            if (Source.Value.Count(c => c == '.') > 1 && string.IsNullOrEmpty(domainParser.Get(Source).SubDomain) == false)
            {
                int p = Source.Value.IndexOf('.');
                generalizedSource = Source.Value.Substring(p + 1);
            }
            else if (Source.Value != "*")
                generalizedSource = "*";
            else
            {
                Debug.Assert(Source.Value == "*");
                generalizedSource = null;
            }
            if (generalizedSource != null)
                return new UMatrixRule(new HierarchicalUrl(generalizedSource), (originalRule ?? this).Destination, (originalRule ?? this).Type, IsAllow, originalRule ?? this);

            return null;
        }
    }

    [Flags]
    public enum DataType
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


    public class HierarchicalUrl
    {
        public static readonly HierarchicalUrl N1stParty = new HierarchicalUrl("1st-party");

        public bool IsUrl => Value.Contains(".");

        public string Value { get; }

        private int specificity = -1;
        public int Specificity
        {
            get
            {
                if (specificity == -1)
                {
                    if (Value == "*")
                        specificity = 0;
                    else if (Value == "1st-party")
                        specificity = 1;
                    else
                        specificity = 1 + (UMatrixRule.domainParser.Get(Value).SubDomain?.Count(c => c == '.') ?? 0); //Null合并运算符的优先级比加号低，所以加号会先算，所以要用括号包起来。
                }

                return specificity;
            }
        }

        public HierarchicalUrl(string value)
        {
            this.Value = value;
        }


        /// <summary>
        /// 返回null表示部分包含。*和1st-party是这种情况。
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool? Covers(HierarchicalUrl url)
        {
            if (Value == "1st-party" || url.Value == "1st-party")
                return null;

            return Value == "*" || url.Value.EndsWith(Value);
        }


        public HierarchicalUrl GetParent()
        {
            var urlSegmentCount = Value.Count(c => c == '.') + 1;
            if (urlSegmentCount > 2)
                return new HierarchicalUrl(GetLastUrlSegments(Value, urlSegmentCount - 1));
            else if (urlSegmentCount == 2)
                return new HierarchicalUrl("1st-party");
            else if (Value == "1st-party")
                return new HierarchicalUrl("*");
            else
            {
                Debug.Assert(Value == "*");
                return null;
            }

        }

        public override bool Equals(object obj)
        {
            var other = obj as HierarchicalUrl;
            if (other == null)
                return false;
            return Value == other.Value;
        }

        public override string ToString()
        {
            return Value;
        }

        internal static string GetLastUrlSegments(string url, int segmentCount)
        {
            int p = url.Length - 1;
            while (p > -1 && segmentCount-- > 0)
                p = url.LastIndexOf('.', p) - 1;

            return p >= 0 ? url.Substring(p + 2) : url;
        }


        public static implicit operator string(HierarchicalUrl url)
        {
            return url.Value;
        }
    }
}