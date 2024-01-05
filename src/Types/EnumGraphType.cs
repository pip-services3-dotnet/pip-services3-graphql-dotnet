using GraphQL.Types;
using System;

namespace PipServices3.GraphQL.Types
{
	/// <summary>
	/// A temporary solution to resolve the described issue:
	/// Unable to serialize 'NONE' value of type 'DummyTypes' to the enumeration type 'DummyTypes'. 
	/// Enumeration does not contain such value. Available values: 'NONE' of type 'String', 'TYPE1' of type 'String', 'TYPE2' of type 'String'.".
	/// </summary>
	/// <example>
	/// <code>
	/// public override void Register()
	///	{
	///		base.Register();
	///		RegisterEnum<DummyTypes>();
	/// }
	/// </code>
	/// </example>	
	internal class EnumGraphType<TEnum> : EnumerationGraphType<TEnum>
			where TEnum : Enum
	{
		private readonly Type _enumType = typeof(TEnum);

		public override object ParseValue(object value)
		{
			if (value is string strValue)
			{

#if NETSTANDARD2_0
				try
				{
					return Enum.Parse(_enumType, strValue, true);
				}
				catch { }
#else
				if (Enum.TryParse(_enumType, strValue, true, out object enumValue)) return enumValue;
#endif
			}

			if (value is int intValue)
			{
				try
				{
					return (TEnum)Enum.ToObject(_enumType, intValue);
				}
				catch { }
			}
			
			return base.ParseValue(value);
		}
	}
}
