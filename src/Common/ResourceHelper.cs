using PipServices3.Commons.Errors;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PipServices3.GraphQL.Common
{
	internal static class ResourceHelper
	{
		internal static string LoadEmbeddedFile(string resourceName, Type type)
		{
			var assembly = Assembly.GetAssembly(type);
			var fullName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.Contains(resourceName));
			if (string.IsNullOrEmpty(fullName))
				throw new NotFoundException($"Embedded resource file {resourceName} not found in assembly");

			var resourceAsStream = assembly.GetManifestResourceStream(fullName);
			var reader = new StreamReader(resourceAsStream);
			var text = reader.ReadToEnd();
			return text;
		}
	}
}
