using System.Collections.Generic;
using uMatrixCleaner;
using Xunit;

namespace Tests
{
    public class TestMain
    {
        [Fact]
        public void TestDeduplicate()
        {
            var r1 = new UMatrixRule("* 1st-party css block");
            var r2 = new UMatrixRule("google.com * css allow");
            var r3 = new UMatrixRule("mail.google.com * css allow");

            var rules = new LinkedList<UMatrixRule>(new[] { r1, r2, r3 });
            Program.Deduplicate(rules, new System.Predicate<UMatrixRule>[0]);

            Assert.Equal(2, rules.Count);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);

            r1 = new UMatrixRule("login.mail.google.com * css allow");
            r2 = new UMatrixRule("google.com * css allow");
            r3 = new UMatrixRule("mail.google.com * css block");
            rules = new LinkedList<UMatrixRule>(new[] { r1, r2, r3 });
            Program.Deduplicate(rules, new System.Predicate<UMatrixRule>[0]);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);
            Assert.Contains(r3, rules);
        }
    }
}
