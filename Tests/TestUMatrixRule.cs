using System;
using uMatrixCleaner;
using Xunit;

namespace Tests
{
    public class TestUMatrixRule
    {
        //        static TestUMatrixRule()
        //        {
        //#if DEBUG
        //            //预先初始化DomainParser，否则调试时容易发送求值超时。
        //            UMatrixRule.domainParser.Get("www.google.com");
        //#endif
        //        }


        [Theory]
        [InlineData("thisav.com * cookie", "* 1st-party cookie", true)]
        [InlineData("* 1st-party cookie", "thisav.com * cookie", true)]
        [InlineData( "thisav.com 1st-party cookie","thisav.com * cookie", true)]
        [InlineData("cw.com.tw 1st-party cookie", "thisav.com * cookie", false)]
        [InlineData("thisav.com * cookie", "cw.com.tw 1st-party cookie", false)]
        [InlineData("* ajax.googleapis.com script", "* 1st-party script", true)]
        [InlineData("youku.com 103.38.56.70 media", "* 1st-party *", false)]
        [InlineData("* 1st-party script", "accuweather.com vortex.accuweather.com script", true)]
        public void TestCovers(string r1, string r2, bool? result)
        {
            var rule1 = new UMatrixRule(r1 + " block");
            var rule2 = new UMatrixRule(r2 + " block");

            Assert.Equal(result, rule1.Contains(rule2));
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
