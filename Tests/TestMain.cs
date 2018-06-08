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
			var relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.DedupEvent += (_, e) =>
			{
				Assert.Single(e.DuplicateRules);
				Assert.Equal(r3, e.DuplicateRules[0]);
			};

			rules = relationshipManager.Clean(int.MaxValue);
			var duplicatedRules = from rule in rules
								  group rule by rule into g
								  where g.Count() > 1
								  select g.Key;
			Assert.False(duplicatedRules.Any(), string.Join(", ", duplicatedRules) + "在返回值中重复");
			Assert.Equal(2, rules.Count);

			r1 = new UMatrixRule("login.mail.google.com * css allow");
			r2 = new UMatrixRule("google.com * css allow");
			r3 = new UMatrixRule("mail.google.com * css block");
			rules = new List<UMatrixRule>(new[] { r1, r2, r3 });
			relationshipManager = new RuleRelationshipManager(rules);

			relationshipManager.DedupEvent += (_, __) => Assert.False(true, "不应该执行去重。");
			relationshipManager.MergeEvent += (_, __) => Assert.False(true, "不应该执行合并。");
			rules = relationshipManager.Clean(int.MaxValue);
			Assert.Equal(3, rules.Count);
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
            var baseRules = (from line in input.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                             where line.Length > 0
                             select new UMatrixRule(line)).ToList().AsReadOnly();

            var rules = new List<UMatrixRule>(baseRules);
            var r1 = new UMatrixRule("appledaily.com rtnvideo1.appledaily.com.tw media allow");
            var r2 = new UMatrixRule("appledaily.com video.appledaily.com.tw media allow");

            rules.Add(r1);
            rules.Add(r2);

			var relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.MergeEvent += (_, e) =>
			{
				Assert.Contains(r1, e.RulesToDelete);
				Assert.Contains(r2, e.RulesToDelete);
				Assert.Equal(new UMatrixRule("appledaily.com appledaily.com.tw media allow"), e.MasterRule);
			};
			relationshipManager.Clean(2);



			rules = new List<UMatrixRule>(baseRules);
			r1 = new UMatrixRule("qq.com captcha.qq.com script allow");
			r2 = new UMatrixRule("qq.com check.ptlogin2.qq.com script allow");
			var r3 = new UMatrixRule("mp.weixin.qq.com qq.com script block");

			rules.Add(r1);
			rules.Add(r2);
			rules.Add(r3);
			relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.DedupEvent += (_, __) => Assert.False(true, "不应该执行去重。");
			relationshipManager.MergeEvent += (_, __) => Assert.False(true, "不应该执行合并。");
			relationshipManager.Clean(2);



            rules = new List<UMatrixRule>(baseRules);
            r1 = new UMatrixRule("thestandnews.com s-static.ak.facebook.com frame allow");
            r2 = new UMatrixRule("appledaily.com.tw s-static.ak.facebook.com frame allow");
            r3 = new UMatrixRule("appledaily.com.tw www.facebook.com frame allow");

			rules.Add(r1);
			rules.Add(r2);
			rules.Add(r3);
			relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.DedupEvent += (_, __) => Assert.False(true, "不应该执行去重。");
			relationshipManager.MergeEvent += (_, __) => Assert.False(true, "不应该执行合并。");
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
			var rules = new List<UMatrixRule>(from line in input.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
											  where line.Length > 0
											  select new UMatrixRule(line));
			var relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.MergeEvent += (_, e) =>
			{
				Assert.Equal(new UMatrixRule("* baidu.com cookie block"), e.MasterRule);
				Assert.Equal(2, e.RulesToDelete.Count);
			};
			relationshipManager.Clean(2);
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
			var rules = new List<UMatrixRule>(from line in input.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
											  where line.Length > 0
											  select new UMatrixRule(line));
			var relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.DedupEvent += (_, __) => Assert.False(true, "不应该执行去重。");
			relationshipManager.MergeEvent += (_, e) =>
			{
				Assert.Equal(new UMatrixRule("* baidu.com * block"), e.MasterRule);
				Assert.Equal(2, e.RulesToDelete.Count);
			};
			relationshipManager.Clean(2);
		}
	}
}
