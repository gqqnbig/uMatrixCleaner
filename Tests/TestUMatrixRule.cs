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
        [InlineData("* youtube.com *", "* youtube.com media", true)]
        [InlineData("* youtube.com media", "* youtube.com *", true)]
        public static void TestIsSuperOrHasJoint(string s1, string s2, bool result)
        {
            var selector1 = new Selector(s1);
            var selector2 = new Selector(s2);

            Assert.Equal(result, selector1.IsSuperOrHasJoint(selector2));
        }

        [Fact(DisplayName = "Domains ending with the same string are not subdomain.")]
        public static void TestIsSuperOrHasJointWithAlikeDomains()
        {
            var s1 = new Selector("licdn.com * script block");
            var s2 = new Selector("alicdn.com * script block");

            Assert.False(s1.IsSuperOrHasJoint(s2));
        }

        [Theory]
        [InlineData("* cdn.sstatic.net script", "stackexchange.com sstatic.net script", false)]
        [InlineData("acfun.tv cdn.aixifan.com *", "acfun.tv cdn.aixifan.com other", true)]
        public static void TestIsProperSuperOf(string r1, string r2, bool result)
        {
            var s1 = new Selector(r1);
            var s2 = new Selector(r2);

            Assert.Equal(result, s1.IsProperSuperOf(s2));
        }

        [Fact]
        public static void TestGeneralize()
        {
            var g = new UMatrixRule("gqqnbig.blogspot.com charliegogogogo.blogspot.com script block").Selector;
            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("gqqnbig.blogspot.com charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com blogspot.com script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("gqqnbig.blogspot.com blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("gqqnbig.blogspot.com * script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("gqqnbig.blogspot.com * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("blogspot.com charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com blogspot.com script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("blogspot.com blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com 1st-party script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("blogspot.com 1st-party *", g.ToString());

            g = g.Generalize();
            Assert.Equal("blogspot.com * script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("blogspot.com * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* charliegogogogo.blogspot.com script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("* charliegogogogo.blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* blogspot.com script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("* blogspot.com *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* * script", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("* * *", g.ToString());

            g = g.Generalize();
            Assert.Null(g);

        }


        [Fact]
        public static void TestGeneralizeWithTld()
        {
            var g = new UMatrixRule("blog.sina.com.cn sjs.sinajs.cn * allow").Selector;
            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
			Assert.Equal("blog.sina.com.cn sinajs.cn *", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("blog.sina.com.cn * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("sina.com.cn sjs.sinajs.cn *", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("sina.com.cn sinajs.cn *", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("sina.com.cn * *", g.ToString());

            g = g.Generalize();
            Assert.Equal("* sjs.sinajs.cn *", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("* sinajs.cn *", g.ToString());

            Assert.Equal(1, Program.GetDistance(g.GetDistanceTo(g = g.Generalize())));
            Assert.Equal("* * *", g.ToString());

            g = g.Generalize();
            Assert.Null(g);
        }

        [Fact]
        public static void TestSpecificity()
        {
            var s1 = new Selector("* thisav.com script block");
            var s2 = new Selector("* thisav.com * block");

            Assert.True(s1.Specificity > s2.Specificity);
        }

        [Fact]
        public static void TestGetRootDomain()
        {
            var addressPredicate = new HostPredicate("wenku.baidu.com");
            Assert.Equal("baidu.com", addressPredicate.GetRootDomain());

            //addressPredicate = new HostPredicate("1.2.3.4");
            //Assert.ThrowsAny<Exception>(() => addressPredicate.GetRootDomain());

            addressPredicate = new HostPredicate("baidu.com");
            Assert.Equal("baidu.com", addressPredicate.GetRootDomain());
        }
    }
}
