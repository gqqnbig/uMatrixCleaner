using System;
using System.Diagnostics;
using System.Linq;
using Nager.PublicSuffix;

namespace uMatrixCleaner
{
    public class UMatrixRule
    {
        internal static readonly DomainParser domainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));

#if DEBUG
        static UMatrixRule()
        {
            //预先初始化DomainParser，否则调试时容易发送求值超时。
            domainParser.Get("www.google.com");
        }
#endif


        /// <summary>
        /// 在一般化过程中保存原始规则
        /// </summary>
        private UMatrixRule originalRule;

        public HostPredicate Source { get; }

        public HostPredicate Destination { get; }

        public TypePredicate Type { get; }

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
                    specificity = Source.Specificity * 100 + Destination.Specificity * 10 + (Type == TypePredicate.All ? 0 : 1);
                return specificity;
            }
        }

        public UMatrixRule(HostPredicate source, HostPredicate destination, TypePredicate type, bool isAllow)
        {
            if (HostPredicate.N1stParty.Equals(source))
                throw new ArgumentException("source不能为1st-party。");

            Source = source;
            Destination = destination;
            Type = type;
            IsAllow = isAllow;
        }

        private UMatrixRule(HostPredicate source, HostPredicate destination, TypePredicate type, bool isAllow, UMatrixRule original) : this(source, destination, type, isAllow)
        {
            this.originalRule = original;
        }


        public UMatrixRule(string line)
        {
            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Source = new HostPredicate(parts[0]);
            Destination = new HostPredicate(parts[1]);
            Type = parts[2] == "*" ? uMatrixCleaner.TypePredicate.All : (TypePredicate)Enum.Parse(typeof(TypePredicate), parts[2], true);
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
            var typeString = Type == TypePredicate.All ? "*" : Type.ToString().ToLower();
            return $"{Source} {Destination} {typeString} {(IsAllow ? "allow" : "block")}";
        }

        /// <summary>
        /// 来源、目标、类型是否包含另一个规则。不考虑动作。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Contains(UMatrixRule other)
        {

            if (Source.IsDomain && other.Source.IsDomain && other.Source.Value.EndsWith(Source.Value) == false)
                return false;

            var a = Source.CoversExclusively(other.Source);

            if (Destination.IsDomain && other.Destination.IsDomain && other.Destination.Value.EndsWith(Destination.Value) == false)
                return false;

            //如果我的目标字段是1st-party，对方来源和目标不是1st-party，则不包含
            if (Destination.Value == "1st-party" && IsNot1stParty(other))
                return false;
            //如果我的来源和目标不是1st-party，对方的目标字段是1st-party，则不包含
            if (other.Destination.Value == "1st-party" && IsNot1stParty(this))
                return false;


            var b = Destination.CoversExclusively(other.Destination);

            if (Type != TypePredicate.All && other.Type != TypePredicate.All && Type != other.Type)
                return false;

            var c = Type.HasFlag(other.Type);

            return a.GetValueOrDefault(true) || b.GetValueOrDefault(true) || c;




            //if (Type.HasFlag(other.Type) == false)
            //    return false;

            //if (Destination.Value == HostPredicate.N1stParty.Value && other.Source.IsDomain && other.Destination.IsDomain && other.Destination.Value.EndsWith(other.Source.Value) == false)
            //    return false;



            //var a = Source.CoversExclusively(other.Source);
            //Debug.Assert(a != null, "Source.Contains()不能返回null。");

            //var b = Destination.CoversExclusively(other.Destination);

            //return a.GetValueOrDefault(true) || b.GetValueOrDefault(true);
        }

        private static bool IsNot1stParty(UMatrixRule other)
        {
            return ((other.Destination.IsDomain && other.Source.IsDomain && other.Destination.Value.EndsWith(other.Source.GetRootDomain()) == false) ||
                    (other.Destination.IsDomain && other.Source.IsIP || other.Destination.IsIP && other.Source.IsDomain) ||
                    (other.Destination.IsIP && other.Source.IsIP && other.Destination.Value != other.Source.Value));
        }

        /// <summary>
        /// 返回一个更一般化的规则。如果本规则已经是顶级规则，则返回null。
        /// <para>返回的规则的<see cref="Specificity"/>小于本规则的<see cref="Specificity"/>。</para>
        /// </summary>
        /// <returns></returns>
        public UMatrixRule Generalize()
        {
            if (Type != TypePredicate.All)
                return new UMatrixRule(Source, Destination, TypePredicate.All, IsAllow, originalRule ?? this);


            Debug.Assert(Source.Value != "1st-party");

            //https://github.com/gorhill/uMatrix/wiki/Changes-from-HTTP-Switchboard#no-more-restriction-on-effective-domain-boundaries
            //URL可以到顶级域，但我这里不允许。

            string generalizedDestination;
            if (Destination.IsDomain && string.IsNullOrEmpty(domainParser.Get(Destination).SubDomain) == false)
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
                return new UMatrixRule(Source, new HostPredicate(generalizedDestination), (originalRule ?? this).Type, IsAllow, originalRule ?? this);


            string generalizedSource;
            if (Source.IsDomain && string.IsNullOrEmpty(domainParser.Get(Source).SubDomain) == false)
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
                return new UMatrixRule(new HostPredicate(generalizedSource), (originalRule ?? this).Destination, (originalRule ?? this).Type, IsAllow, originalRule ?? this);

            return null;
        }
    }

    [Flags]
    public enum TypePredicate
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


    public class HostPredicate
    {
        //https://url.spec.whatwg.org/#host-representation ，Host是Domain或者IP。

        public static readonly HostPredicate N1stParty = new HostPredicate("1st-party");

        public bool IsDomain => Value.Contains(".") && IsIP == false;

        public bool IsIP => Value.All(c => c == '.' || char.IsDigit(c)); //目前仅支持IPv4

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
                    else if (IsIP)
                        specificity = 1;
                    else
                        specificity = 1 + (UMatrixRule.domainParser.Get(Value).SubDomain?.Count(c => c == '.') ?? 0); //Null合并运算符的优先级比加号低，所以加号会先算，所以要用括号包起来。
                }

                return specificity;
            }
        }

        public HostPredicate(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// 返回不带子域名的名称
        /// </summary>
        /// <returns></returns>
        public string GetRootDomain()
        {
            if (IsDomain == false)
                throw new NotSupportedException($"当地址谓词不是URL时，不能调用{nameof(GetRootDomain)}。");
            if (IsIP)
                throw new NotSupportedException($"当地址谓词是IP时，不能调用{nameof(GetRootDomain)}。");

            var domainName = UMatrixRule.domainParser.Get(Value);

            if (domainName == null)
                return Value;

            var subDomain = domainName.SubDomain;
            if (subDomain == null)
                return Value;
            else
                return Value.Substring(subDomain.Length + 1);//去掉.
        }


        /// <summary>
        /// 返回null表示部分包含。*和1st-party是这种情况。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool? Covers(HostPredicate other)
        {
            if (Value == "1st-party" || other.Value == "1st-party")
                return null;

            return Value == "*" || other.Value.EndsWith(Value);
        }

        /// <summary>
        /// 返回null表示部分包含。*和1st-party是这种情况。
        /// Exclusively表示本规则和other不能相同。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool? CoversExclusively(HostPredicate other)
        {
            if (Value == other.Value)
                return false;

            if (Value == "1st-party" || other.Value == "1st-party")
                return null;

            return Value == "*" || other.Value.EndsWith(Value);
        }


        public HostPredicate GetParent()
        {
            if (IsDomain)
            {
                var subDomainSegmentCount = UMatrixRule.domainParser.Get(Value).SubDomain?.Count(c => c == '.') ?? 0;
                if (subDomainSegmentCount > 0)
                    return new HostPredicate(GetLastUrlSegments(Value, subDomainSegmentCount - 1));
                else
                    return N1stParty;
            }

            if (Value == "1st-party")
                return new HostPredicate("*");
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


        public static implicit operator string(HostPredicate url)
        {
            return url.Value;
        }
    }
}