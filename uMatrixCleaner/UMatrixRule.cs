using System;
using Nager.PublicSuffix;

namespace uMatrixCleaner
{
    public class UMatrixRule
    {
        public bool IsAllow { get; }

        public Selector Selector { get; }

        public int Priority
        {
            get
            {
                return Selector.Specificity * 10 + (IsAllow ? 0 : 1);
            }
        }

        public UMatrixRule(HostPredicate source, HostPredicate destination, TypePredicate type, bool isAllow)
        {
            if (HostPredicate.N1stParty.Equals(source))
                throw new ArgumentException("source不能为1st-party。");

            Selector = new Selector(source, destination, type);
            IsAllow = isAllow;
        }


        public UMatrixRule(string line)
        {
            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var source = new HostPredicate(parts[0]);
            var destination = new HostPredicate(parts[1]);
            var type = parts[2] == "*" ? uMatrixCleaner.TypePredicate.All : (TypePredicate)Enum.Parse(typeof(TypePredicate), parts[2], true);
            Selector = new Selector(source, destination, type);
            IsAllow = parts[3] == "allow";
        }

        public override bool Equals(object obj)
        {
            var other = obj as UMatrixRule;
            if (other == null)
                return false;

            return Selector.Equals(other.Selector) && IsAllow == other.IsAllow;
        }

        public override int GetHashCode()
        {
            return Selector.Source.Value.GetHashCode() ^ Selector.Destination.Value.GetHashCode() ^ Selector.Type.GetHashCode() ^ IsAllow.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Selector} {(IsAllow ? "allow" : "block")}";
        }
    }
}