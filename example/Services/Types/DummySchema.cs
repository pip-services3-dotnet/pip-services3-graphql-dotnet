using GraphQL.Types;
using PipServices3.Commons.Refer;

namespace PipServices3.GraphQL.Services.Types
{
	public class DummySchema: Schema, IReferenceable
	{
		public DummySchema()
		{
			Query =  new DummyQueryType();
			Mutation = new DummyMutationType();
		}

		public void SetReferences(IReferences references)
		{
			if (Query is IReferenceable query) query.SetReferences(references);
			if (Mutation is IReferenceable mutation) mutation.SetReferences(references);
		}
	}
}
