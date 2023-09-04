using HotChocolate.Types;
using PipServices3.Commons.Data;
using PipServices3.GraphQL.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipServices3.GraphQL.Services.Types
{
	public class DataPageType: ObjectType<DataPage<Dummy>>
	{
		protected override void Configure(IObjectTypeDescriptor<DataPage<Dummy>> descriptor)
		{
			
		}
	}
}
