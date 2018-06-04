using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using uMatrixCleaner;
using Xunit;

namespace Tests
{
    public class TestMain
    {
        [Fact]
        public static void TestDeduplicate()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<Program>();

            var r1 = new UMatrixRule("* 1st-party css block");
            var r2 = new UMatrixRule("google.com * css allow");
            var r3 = new UMatrixRule("mail.google.com * css allow");

            var rules = new List<UMatrixRule>(new[] { r1, r2, r3 });
            Program.Merge(rules, int.MaxValue);
            var duplicatedRules = from rule in rules
                                  group rule by rule into g
                                  where g.Count() > 1
                                  select g.Key;
            Assert.False(duplicatedRules.Any(), string.Join(", ", duplicatedRules) + "在返回值中重复");
            Assert.Equal(2, rules.Count);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);

            r1 = new UMatrixRule("login.mail.google.com * css allow");
            r2 = new UMatrixRule("google.com * css allow");
            r3 = new UMatrixRule("mail.google.com * css block");
            rules = new List<UMatrixRule>(new[] { r1, r2, r3 });
            Program.Merge(rules, int.MaxValue);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);
            Assert.Contains(r3, rules);
        }

		[Fact]
		public static void TestMergeDestination()
		{
			var input = @"
* * * block
* * css allow
* * frame block
* * image allow
* * script block
* 1st-party * allow
* 1st-party frame allow
* 1st-party script allow";
            var baseRules = (from line in input.Split("\r\n")
                             where line.Length > 0
                             select new UMatrixRule(line)).ToList().AsReadOnly();

            var rules = new List<UMatrixRule>(baseRules);
            var r1 = new UMatrixRule("appledaily.com rtnvideo1.appledaily.com.tw media allow");
            var r2 = new UMatrixRule("appledaily.com video.appledaily.com.tw media allow");

            rules.Add(r1);
            rules.Add(r2);

            Program.Merge(rules, 2);

            Assert.DoesNotContain(r1, rules);
            Assert.DoesNotContain(r2, rules);
            var r3 = new UMatrixRule("appledaily.com appledaily.com.tw media allow");
            Assert.Contains(r3, rules);


            rules = new List<UMatrixRule>(baseRules);
            r1 = new UMatrixRule("qq.com captcha.qq.com script allow");
            r2 = new UMatrixRule("qq.com check.ptlogin2.qq.com script allow");
            r3 = new UMatrixRule("mp.weixin.qq.com qq.com script block");

            rules.Add(r1);
            rules.Add(r2);
            rules.Add(r3);
            Program.Merge(rules, 2);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);
            Assert.Contains(r3, rules);



            rules = new List<UMatrixRule>(baseRules);
            r1 = new UMatrixRule("thestandnews.com s-static.ak.facebook.com frame allow");
            r2 = new UMatrixRule("appledaily.com.tw s-static.ak.facebook.com frame allow");
            r3 = new UMatrixRule("appledaily.com.tw www.facebook.com frame allow");

            rules.Add(r1);
            rules.Add(r2);
            rules.Add(r3);
            Program.Merge(rules, 3);
            Assert.Contains(r1, rules);
            Assert.Contains(r2, rules);
            Assert.Contains(r3, rules);
        }

		[Fact]
		public static void TestMergeWildcard()
		{
			var input = @"
* * * block
* google.com css allow
* facebook.com css allow";

            var rules = new List<UMatrixRule>(from line in input.Split("\r\n")
                                              where line.Length > 0
                                              select new UMatrixRule(line));
            Program.Merge(rules, 2);
            Assert.Contains(new UMatrixRule("* * css allow"), rules);
            Assert.Equal(2, rules.Count);
        }

		[Fact]
		public static void TestMergeBlock()
		{

            var input = @"
* * * block
* * css allow
* * frame block
* * image allow
* * script block
* 1st-party * allow
* 1st-party frame allow
* 1st-party script allow
* www.baidu.com cookie block
* tieba.baidu.com cookie block";
            var rules = new List<UMatrixRule>(from line in input.Split("\r\n")
                                              where line.Length > 0
                                              select new UMatrixRule(line));
            Program.Merge(rules, 2);
            Assert.Contains(new UMatrixRule("* baidu.com cookie block"), rules);

        }

		[Fact(DisplayName = "Merge wildcard type with action block")]
		public static void TestMergeTypeWildcardBlock()
		{
			var input = @"
* * * block
* * css allow
* * frame block
* * image allow
* * script block
* 1st-party * allow
* 1st-party frame allow
* 1st-party script allow
* baidu.com css block
* baidu.com image block";
            var rules = new List<UMatrixRule>(from line in input.Split("\r\n")
                                              where line.Length > 0
                                              select new UMatrixRule(line));
            Program.Merge(rules, 2);
            Assert.Contains(new UMatrixRule("* baidu.com * block"), rules);

        }
    }
}
