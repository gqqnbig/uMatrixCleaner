using System;
using System.Diagnostics;
using System.Linq;
using Nager.PublicSuffix;

namespace uMatrixCleaner
{

    public class Selector
    {
        /// <summary>
        /// 在一般化过程中保存原始选择器
        /// </summary>
        private readonly Selector originalRule;


        public HostPredicate Source { get; }
        public HostPredicate Destination { get; }
        public TypePredicate Type { get; }

        public Selector(string str)
        {
            string[] parts = str.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Source = new HostPredicate(parts[0]);
            Destination = new HostPredicate(parts[1]);
            Type = parts[2] == "*" ? TypePredicate.All : (TypePredicate)Enum.Parse(typeof(TypePredicate), parts[2], true);
        }

        public Selector(HostPredicate source, HostPredicate destination, TypePredicate type, Selector selector) : this(source, destination, type)
        {
            this.originalRule = selector;
        }

        public Selector(HostPredicate source, HostPredicate destination, TypePredicate type)
        {
            Source = source;
            Destination = destination;
            Type = type;
        }

        /// <summary>
        /// 表示此规则的具体性。数值越高，规则越具体。
        /// </summary>
        public int Specificity
        {
            get
            {
                return Source.Specificity * 100 + Destination.Specificity * 10 + (Type == TypePredicate.All ? 0 : 1);
            }
        }

        /// <summary>
        /// 计算本选择器（来源、目标、类型）的范围是不是另一个规则的选择器的范围的超集，
        /// 或本选择器的范围与另一个规则的选择器的范围具有交集。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSuperOrHasJoint(Selector other)
        {
            if (EvaluateRelationship(other, out var a, out var b, out var c) == false)
                return false;

            return a.GetValueOrDefault(true) || b.GetValueOrDefault(true) || c.GetValueOrDefault(true);
        }

        private bool EvaluateRelationship(Selector other, out bool? sourceResult, out bool? destinationResult, out bool? typeResult)
        {
            sourceResult = Source.Covers(other.Source);
            destinationResult = Destination.Covers(other.Destination);
            typeResult = Type.HasFlag(other.Type);
            if (Type == TypePredicate.All && other.Type == TypePredicate.All)
                typeResult = null;

            if ((Source.IsDomain || Source.IsIP) && (other.Source.IsDomain || other.Source.IsIP)
                && Source.IsSubDomain(other.Source) == false && other.Source.IsSubDomain(Source) == false)
                return false;

            if ((Destination.IsDomain || Destination.IsIP) && (other.Destination.IsDomain || other.Destination.IsIP) && destinationResult == false
                && Destination.IsSubDomain(other.Destination) == false && other.Destination.IsSubDomain(Destination) == false)
                return false;

            //如果我的目标字段是1st-party，对方来源和目标不是1st-party，则不包含
            if (Destination.Value == "1st-party" && IsNot1stParty(other))
                return false;
            //如果我的来源和目标不是1st-party，对方的目标字段是1st-party，则不包含
            if (other.Destination.Value == "1st-party" && IsNot1stParty(this))
                return false;

            if (Type != TypePredicate.All && other.Type != TypePredicate.All && Type != other.Type)
                return false;

            return true;
        }


        /// <summary>
        /// 计算本选择器是不是另一个规则的选择器有交集。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool HasJoint(Selector other)
        {
            if (EvaluateRelationship(other, out var a, out var b, out var c) == false)
                return false;

            return a == null && b == null && c == null;
        }

        public bool IsSuperOf(Selector other)
        {
            if (EvaluateRelationship(other, out var a, out var b, out var c) == false)
                return false;

            return a.GetValueOrDefault(false) && b.GetValueOrDefault(false) && c.GetValueOrDefault(false);
        }

        /// <summary>
        /// 测试本选择器是不是另一个选择器的真超集
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsProperSuperOf(Selector other)
        {
            return this.Equals(other) == false && IsSuperOf(other);
        }

        private static bool IsNot1stParty(Selector other)
        {
            return ((other.Destination.IsDomain && other.Source.IsDomain && other.Destination.Value.EndsWith(other.Source.GetRootDomain()) == false) ||
                    (other.Destination.IsDomain && other.Source.IsIP || other.Destination.IsIP && other.Source.IsDomain) ||
                    (other.Destination.IsIP && other.Source.IsIP && other.Destination.Value != other.Source.Value));
        }

        /// <summary>
        /// 返回一个更一般化的选择器。如果本选择器已经是最一般的选择器，即 * * *，则返回null。
        /// <para>返回的选择器的<see cref="Specificity"/>小于本选择器的<see cref="Specificity"/>。</para>
        /// </summary>
        /// <returns></returns>
        public Selector Generalize()
        {
            if (Type != TypePredicate.All)
                return new Selector(Source, Destination, TypePredicate.All, originalRule ?? this);


            Debug.Assert(Source.Value != "1st-party");

            //https://github.com/gorhill/uMatrix/wiki/Changes-from-HTTP-Switchboard#no-more-restriction-on-effective-domain-boundaries
            //URL可以到顶级域，但我这里不允许。

            string generalizedDestination;
            if (Destination.IsDomain && String.IsNullOrEmpty(HostPredicate.domainParser.Get(Destination).SubDomain) == false)
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
                return new Selector(Source, new HostPredicate(generalizedDestination), (originalRule ?? this).Type, originalRule ?? this);


            string generalizedSource;
            if (Source.IsDomain && String.IsNullOrEmpty(HostPredicate.domainParser.Get(Source).SubDomain) == false)
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
                return new Selector(new HostPredicate(generalizedSource), (originalRule ?? this).Destination, (originalRule ?? this).Type, originalRule ?? this);

            return null;
        }

        public override string ToString()
        {
            var typeString = Type == TypePredicate.All ? "*" : Type.ToString().ToLower();
            return $"{Source} {Destination} {typeString}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as Selector;
            if (other == null)
                return false;

            return Source.Value == other.Source.Value && Destination.Value == other.Destination.Value && Type == other.Type;
        }

        public override int GetHashCode()
        {
            return Source.Value.GetHashCode() ^ Destination.Value.GetHashCode() ^ Type.GetHashCode();
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
        //https://url.spec.whatwg.org/#host-representation ，Host是Domain或IP的统称。


        internal static readonly DomainParser domainParser = new DomainParser(new FileTldRuleProvider("public_suffix_list.dat"));

        public static readonly HostPredicate N1stParty = new HostPredicate("1st-party");

#if DEBUG
        static HostPredicate()
        {
            //预先初始化DomainParser，否则调试时容易发送求值超时。
            domainParser.Get("www.google.com");
        }
#endif

        public bool IsDomain => HostType == UriHostNameType.Dns;


        /// <summary>
        /// 返回IPv4、DNS、Basic（通配符）。
        /// </summary>
        public UriHostNameType HostType { get; }

        public bool IsIP => HostType == UriHostNameType.IPv4;

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
                        specificity = 1 + ((domainParser.Get(Value).SubDomain?.Count(c => c == '.') + 1) ?? 0); //Null合并运算符的优先级比加号低，所以加号会先算，所以要用括号包起来。
                }

                return specificity;
            }
        }

        public HostPredicate(string value)
        {
            this.Value = value;

            if (Value.All(c => c == '.' || Char.IsDigit(c)))
                HostType = UriHostNameType.IPv4;
            else if (Value.Contains("."))
                HostType = UriHostNameType.Dns;
            else
                HostType = UriHostNameType.Basic;
        }

        /// <summary>
        /// 返回不带子域名的名称
        /// </summary>
        /// <returns></returns>
        public string GetRootDomain()
        {
            if (IsIP)
                return Value;
            if (IsDomain == false)
                throw new NotSupportedException($"地址谓词{Value}不是域名，因此不能调用{nameof(GetRootDomain)}。");

            var domainName = domainParser.Get(Value);

            if (domainName == null)
                return Value;

            return domainName.RegistrableDomain;
        }

        /// <summary>
        /// 自己是自己的子域名
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsSubDomain(HostPredicate other)
        {
            Debug.Assert(IsDomain == false || this.IsSubDomainCore(this));

            return IsSubDomainCore(other);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool IsSubDomainCore(HostPredicate other)
        {
            if (other.HostType == UriHostNameType.Basic)
                return false;

            if (IsIP)
                return Value == other.Value;


            if (IsDomain)
            {
                //可能一个主机谓词是com，另一个是google.com，则后者被会判断为前者的子域名。
                //如果要严谨一点，需要调用DomainParser，这个调用很慢。

                var longSubDomainSegments = Value.Split('.');
                var shortSubDomainSegments = other.Value.Split('.');


                for (int i = longSubDomainSegments.Length - 1, j = shortSubDomainSegments.Length - 1; i >= 0 && j >= 0; i--, j--)
                {
                    if (longSubDomainSegments[i] != shortSubDomainSegments[j])
                        return false;
                }

                return true;
            }

            throw new NotSupportedException($"不能对{Value}调用{nameof(IsSubDomain)}()。");
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