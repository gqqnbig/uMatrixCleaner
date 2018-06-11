using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using uMatrixCleaner;

namespace Tests
{
	public class HostPredicateTester
    {
	    [Fact]
		public void TestSpecificity()
	    {
			var p1=new HostPredicate("*");
		    var p2 = HostPredicate.N1stParty;

		    Assert.True(p1.Specificity < p2.Specificity, "\"*\"的规格强度小于\"1st-party\"");
	    }
    }
}
