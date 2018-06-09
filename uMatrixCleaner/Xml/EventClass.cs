using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace uMatrixCleaner.Xml
{
	[XmlRoot("Events")]
	public class EventsHelper
	{
		[XmlElement("MergeEvent", typeof(MergeEventArgs))]
		[XmlElement("DedupRuleEvent", typeof(DedupRuleEventArgs))]
		public List<EventArgs> Events { get; set; } = new List<EventArgs>();
	}
}