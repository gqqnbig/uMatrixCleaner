using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace uMatrixCleaner.Xml
{
	static class XmlSerializationHelper
	{
		private static bool IsCompilerGenerated(FieldInfo fieldInfo)
		{
			foreach (Attribute attribute in fieldInfo.GetCustomAttributes(false))
			{
				if (attribute is CompilerGeneratedAttribute)
					return true;
			}

			return false;
		}

		public static void SetGetterOnlyAutoProperty(this object obj, string propertyName, object value)
		{
			var field = obj.GetType().GetFields(BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance)
						 .First(f => f.Name.Contains(propertyName) && IsCompilerGenerated(f));
			field.SetValue(obj, value);
		}
	}
}
