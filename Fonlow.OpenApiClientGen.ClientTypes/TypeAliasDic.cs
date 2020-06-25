﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fonlow.OpenApiClientGen.ClientTypes
{
	public class TypeAliasDic
	{
		readonly Dictionary<string, string> dic = new Dictionary<string, string>();

		//private static readonly Lazy<TypeAliasDic>
		//lazy =
		//new Lazy<TypeAliasDic>
		//	(() => new TypeAliasDic());

		//public static TypeAliasDic Instance { get { return lazy.Value; } }

		//private TypeAliasDic()
		//{
		//}

		public void Add(string alias, string typeName)
		{
			dic.TryAdd(alias, typeName);
		}

		public bool TryGet(string alias, out string typeName)
		{
			bool r = dic.TryGetValue(alias, out typeName);
			if (!r)
			{
				return false;
			}

			bool r2 = TryGet(typeName, out string typeName2);
			if (r2)
			{
				typeName = typeName2;
				return true;
			}

			return true;
		}

		public void Clear()
		{
			dic.Clear();
		}
	}
}

