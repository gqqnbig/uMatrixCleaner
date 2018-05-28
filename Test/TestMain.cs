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
        public void TestGetClosestParent()
        {
            var rule = new UMatrixRule("* www.google-analytics.com image allow");
            var rules = new List<UMatrixRule>(new[]
            {
                new UMatrixRule("* * image allow"),
                new UMatrixRule("* 1st-party image allow"),
            });
            rules.Add(rule);


            var cr = Program.GetClosestParent(rule, rules);
            Assert.Equal("* 1st-party image allow", cr.ToString());
        }
    }
}
