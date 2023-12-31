﻿using GraphQL.Types;
using PipServices3.GraphQL.Data;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummyParamType: ObjectGraphType<DummyParam>
	{
		public DummyParamType() 
		{
			Field(x => x.Name).Description("The name of the DummyParam.");
			Field(x => x.Value).Description("The value of the DummyParam.");
		}
	}
}
