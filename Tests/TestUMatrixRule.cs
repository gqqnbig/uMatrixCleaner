using System;
using uMatrixCleaner;
using Xunit;

namespace Tests
{
    public class TestUMatrixRule
    {

        [Fact]
        public void TestCovers()
        {
            var rule1 = new UMatrixRule(new HierarchicalUrl("thisav.com"), new HierarchicalUrl("*"), DataType.Cookie, false);
            var rule2 = new UMatrixRule(new HierarchicalUrl("*"), HierarchicalUrl.N1stParty, DataType.Cookie, false);

            Assert.Null(rule2.Covers(rule1));
            Assert.False(rule1.Covers(rule2), $"{rule1}不应覆盖{rule2}");

            rule1 = new UMatrixRule(new HierarchicalUrl("thisav.com"), new HierarchicalUrl("*"), DataType.Cookie, false);
            rule2 = new UMatrixRule(new HierarchicalUrl("thisav.com"), HierarchicalUrl.N1stParty, DataType.Cookie, false);

            Assert.Null(rule2.Covers(rule1));

            rule1 = new UMatrixRule("cw.com.tw 1st-party cookie block");
            rule2 = new UMatrixRule("thisav.com * cookie block");
            Assert.False(rule2.Covers(rule1), $"{rule2}不应覆盖{rule1}");
            Assert.False(rule1.Covers(rule2), $"{rule1}不应覆盖{rule2}");

        }

        [Fact]
        public void TestGeneralize()
        {
            var g = new UMatrixRule("gqqnbig.blogspot.com charliegogogogo.blogspot.com script block");
            g = g.Generalize();
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


        [Fact]
        public void TestGeneralizeWithTld()
        {
            var g = new UMatrixRule("blog.sina.com.cn sjs.sinajs.cn * allow");
            g = g.Generalize();
            Assert.Equal("blog.sina.com.cn sinajs.cn * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("blog.sina.com.cn * * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn sjs.sinajs.cn * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn sinajs.cn * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn * * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("* sjs.sinajs.cn * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("* sinajs.cn * allow", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * * allow", g.ToString());
            
            g = g.Generalize();
            Assert.Null(g);
        }

        [Fact]
        public void TestSpecificity()
        {
            var r1 = new UMatrixRule("* thisav.com script block");
            var r2 = new UMatrixRule("* thisav.com * block");

            Assert.True(r1.Specificity > r2.Specificity);
        }
    }
}
