using System;
using uMatrixCleaner;
using Xunit;

namespace Tests
{
    public class TestUMatrixRule
    {
        [Theory]
        [InlineData("thisav.com * cookie", "* 1st-party cookie", true)]
        [InlineData("* 1st-party cookie", "thisav.com * cookie", true)]
        [InlineData("thisav.com 1st-party cookie", "thisav.com * cookie", true)]
        [InlineData("cw.com.tw 1st-party cookie", "thisav.com * cookie", false)]
        [InlineData("thisav.com * cookie", "cw.com.tw 1st-party cookie", false)]
        [InlineData("* ajax.googleapis.com script", "* 1st-party script", true)]
        [InlineData("youku.com 103.38.56.70 media", "* 1st-party *", false)]
        [InlineData("* 1st-party script", "accuweather.com vortex.accuweather.com script", true)]
        [InlineData("wikipedia.org * *", "* * css", true)]
        [InlineData("* * css", "* 1st-party *", true)]
        [InlineData("* 1st-party other", "wenku.baidu.com baidu.com other", true)]
        [InlineData("* youtube.com *", "91porn.com 192.240.120.2 media", false)]
        [InlineData("* cdn.sstatic.net script", "stackexchange.com sstatic.net script", true)]
        public void TestCovers(string r1, string r2, bool result)
        {
            var rule1 = new UMatrixRule(r1 + " block");
            var rule2 = new UMatrixRule(r2 + " block");

            Assert.Equal(result, rule1.Selector.IsSuperOrHasJoint(rule2.Selector));
        }

        [Theory]
        [InlineData("* cdn.sstatic.net script", "stackexchange.com sstatic.net script", false)]
        public void TestIsProperSuperOf(string r1, string r2, bool result)
        {
            var rule1 = new UMatrixRule(r1 + " block");
            var rule2 = new UMatrixRule(r2 + " block");

            Assert.Equal(result, rule1.Selector.IsProperSuperOf(rule2.Selector));
        }

        [Fact]
        public void TestGeneralize()
        {
            var g = new UMatrixRule("gqqnbig.blogspot.com charliegogogogo.blogspot.com script block").Selector;
            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com blogspot.com script", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com * script", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com script", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com blogspot.com script", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com 1st-party script", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com 1st-party *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com * script", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* charliegogogogo.blogspot.com script", g.ToString());

            g = g.Generalize();
            Assert.Equal("* charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* blogspot.com script", g.ToString());

            g = g.Generalize();
            Assert.Equal("* blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * script", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * *", g.ToString());

            g = g.Generalize();
            Assert.Null(g);

        }


        [Fact]
        public void TestGeneralizeWithTld()
        {
            var g = new UMatrixRule("blog.sina.com.cn sjs.sinajs.cn * allow").Selector;
            g = g.Generalize();
            Assert.Equal("blog.sina.com.cn sinajs.cn *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blog.sina.com.cn * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn sjs.sinajs.cn *", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn sinajs.cn *", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* sjs.sinajs.cn *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* sinajs.cn *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * *", g.ToString());

            g = g.Generalize();
            Assert.Null(g);
        }

        [Fact]
        public void TestSpecificity()
        {
            var r1 = new UMatrixRule("* thisav.com script block");
            var r2 = new UMatrixRule("* thisav.com * block");

            Assert.True(r1.Selector.Specificity > r2.Selector.Specificity);
        }

        [Fact]
        public void TestGetRootDomain()
        {
            var addressPredicate = new HostPredicate("wenku.baidu.com");
            Assert.Equal("baidu.com", addressPredicate.GetRootDomain());

            addressPredicate = new HostPredicate("1.2.3.4");
            Assert.ThrowsAny<Exception>(() => addressPredicate.GetRootDomain());

            addressPredicate = new HostPredicate("baidu.com");
            Assert.Equal("baidu.com", addressPredicate.GetRootDomain());
        }
    }
}
