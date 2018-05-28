using System;
using uMatrixCleaner;
using Xunit;

namespace Test
{
    public class TestUMatrixRule
    {
        [Fact]
        public void TestCovers()
        {
            var rule1 = new UMatrixRule(new HierarchicalUrl("thisav.com"), new HierarchicalUrl("*"), DataType.Cookie, false);
            var rule2 = new UMatrixRule(HierarchicalUrl.N1stParty, HierarchicalUrl.N1stParty, DataType.Cookie, false);

            Assert.Null(rule2.Covers(rule1));
            Assert.Null(rule1.Covers(rule2));

            rule1 = new UMatrixRule(new HierarchicalUrl("thisav.com"), new HierarchicalUrl("*"), DataType.Cookie, false);
            rule2 = new UMatrixRule(new HierarchicalUrl("thisav.com"), HierarchicalUrl.N1stParty, DataType.Cookie, false);

            Assert.Null(rule2.Covers(rule1));
        }

        [Fact]
        public void TestGeneralize()
        {
            var rule = new UMatrixRule("gqqnbig.blogspot.com charliegogogogo.blogspot.com script block");
            var g = rule.Generalize();
            Assert.Equal("gqqnbig.blogspot.com charliegogogogo.blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com blogspot.com script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com * script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com * * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com blogspot.com script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com 1st-party script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com 1st-party * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com * script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com * * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* charliegogogogo.blogspot.com script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* charliegogogogo.blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* blogspot.com script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* blogspot.com * block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * script block", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * * block", g.ToString());

            g = g.Generalize();
            Assert.Null(g);

        }
    }
}
