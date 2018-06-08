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

			var rules = new List<UMatrixRule>(from line in input.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
											  where line.Length > 0
											  select new UMatrixRule(line));
			var relationshipManager = new RuleRelationshipManager(rules);
			var eventRaised = false;
			relationshipManager.MergeEvent += (_, e) =>
			{
				eventRaised = true;
				Assert.AreEqual(new UMatrixRule("* * css allow"), e.MasterRule);
				Assert.AreEqual(2, e.RulesToDelete.Count);
			};
			rules = relationshipManager.Clean(2);
			Assert.IsTrue(eventRaised, "MergeEvent is not raised.");
			Assert.AreEqual(2, rules.Count, "Rules: \r\n" + string.Join(", ", rules));
		}

	}
}
