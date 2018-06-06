using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using uMatrixCleaner;

namespace Tests
{
	[TestClass]
	public class TestMain2
	{

		[TestMethod]
		public void TestMergeWildcard()
		{
			var input = @"
* * * block
* google.com css allow
* facebook.com css allow";

			var rules = new List<UMatrixRule>(from line in input.Split("\r\n")
											  where line.Length > 0
											  select new UMatrixRule(line));
			var relationshipManager = new RuleRelationshipManager(rules);
			relationshipManager.MergeEvent += (_, e) =>
			{
				Assert.AreEqual(new UMatrixRule("* * css allow"), e.MasterRule);
				Assert.AreEqual(2, e.RulesToDelete.Count);
			};
			rules = relationshipManager.Clean(2);
			Assert.AreEqual(4, rules.Count, "Rules: \r\n" + string.Join(", ", rules));
		}

	}
}
