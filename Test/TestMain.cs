using System;
using System.Collections.Generic;
using System.Text;
using uMatrixCleaner;
using Xunit;

namespace Test
{
    public class TestMain
    {
        [Fact]
        public void TestFindConflictingRule()
        {
            var rule = new UMatrixRule("* www.google-analytics.com image allow");
            var rules = new List<UMatrixRule>(new[]
            {
                new UMatrixRule("* * image block"),
                new UMatrixRule("* 1st-party image block"),
            });
            rules.Add(rule);


            var cr = Program.FindConflictingRule(rule, rules);
            Assert.Equal("* 1st-party image block", cr.ToString());
        }
    }
}
