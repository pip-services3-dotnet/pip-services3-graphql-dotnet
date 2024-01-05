using GraphQL.Types;
using System;

namespace PipServices3.GraphQL.Types
{
	public class TimeSpanGraphType : TimeSpanMillisecondsGraphType
	{
		public TimeSpanGraphType()
			: base()
		{ 
			Name = "TimeSpan";
			Description = "The `TimeSpan` scalar type represents a period of time represented as the total number of " +
				"milliseconds in range [-922337203685477, 922337203685477] or string in format [-][d.]hh:mm:ss[.fffffff].";
		}

		public override object ParseValue(object value)
		{
			if (value == null) return null;
			if (TimeSpan.TryParse(value.ToString(), out TimeSpan timeSpan)) return timeSpan;

			return base.ParseValue(value);
		}
	}
}
