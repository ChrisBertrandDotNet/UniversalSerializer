﻿
// Copyright Christophe Bertrand.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UniversalSerializerLib3
{
	internal class ParsedAssemblyQualifiedName
	{
		internal Lazy<AssemblyName> AssemblyNameDescriptor;
		internal Lazy<Type> FoundType;
		internal readonly string AssemblyDescriptionString;
		internal readonly string TypeName;
		internal readonly string ShortAssemblyName;
		internal readonly string Version;
		internal readonly string Culture;
		internal readonly string internalKeyToken;
		internal readonly List<ParsedAssemblyQualifiedName> GenericParameters = new List<ParsedAssemblyQualifiedName>();
		internal readonly Lazy<string> CSharpStyleName;
		internal readonly Lazy<string> VBNetStyleName;

		internal ParsedAssemblyQualifiedName(string AssemblyQualifiedName)
		{
			int index = -1;
			block rootBlock = new block();
			{
				int bcount = 0;
				block currentBlock = rootBlock;
				for (int i = 0; i < AssemblyQualifiedName.Length; ++i)
				{
					char c = AssemblyQualifiedName[i];
					if (c == '[')
					{
						if (AssemblyQualifiedName[i + 1] == ']') // Array type.
							i++;
						else
						{
							++bcount;
							var b = new block() { iStart = i + 1, level = bcount, parentBlock = currentBlock };
							currentBlock.innerBlocks.Add(b);
							currentBlock = b;
						}
					}
					else if (c == ']')
					{
						currentBlock.iEnd = i - 1;
						if (AssemblyQualifiedName[currentBlock.iStart] != '[')
						{
							currentBlock.parsedAssemblyQualifiedName = new ParsedAssemblyQualifiedName(AssemblyQualifiedName.Substring(currentBlock.iStart, i - currentBlock.iStart));
							if (bcount == 2)
								this.GenericParameters.Add(currentBlock.parsedAssemblyQualifiedName);
						}
						currentBlock = currentBlock.parentBlock;
						--bcount;
					}
					else if (bcount == 0 && c == ',')
					{
						index = i;
						break;
					}
				}
			}

			this.TypeName = AssemblyQualifiedName.Substring(0, index);

			this.CSharpStyleName = new Lazy<string>(
				() =>
				{
					return this.LanguageStyle("<", ">");
				});

			this.VBNetStyleName = new Lazy<string>(
				() =>
				{
					return this.LanguageStyle("(Of ", ")");
				});

			this.AssemblyDescriptionString = AssemblyQualifiedName.Substring(index + 2);

			{
				List<string> parts = AssemblyDescriptionString.Split(',')
																 .Select(x => x.Trim())
																 .ToList();
				this.Version = LookForPairThenRemove(parts, "Version");
				this.Culture = LookForPairThenRemove(parts, "Culture");
				this.internalKeyToken = LookForPairThenRemove(parts, "internalKeyToken");
				if (parts.Count > 0)
					this.ShortAssemblyName = parts[0];
			}

			this.AssemblyNameDescriptor = new Lazy<AssemblyName>(
				() => new System.Reflection.AssemblyName(this.AssemblyDescriptionString));

			this.FoundType = new Lazy<Type>(
				() => TypeFromAssemblyQualifiedName(AssemblyQualifiedName));
		}

		internal static Type TypeFromAssemblyQualifiedName(string AssemblyQualifiedName)
		{
			var searchedType = Type.GetType(AssemblyQualifiedName);
			if (searchedType != null)
				return searchedType;
			foreach (var assem in FrameworkTools.Framework.Assemblies.Value)
			{
				searchedType =
					assem.GetType(AssemblyQualifiedName);
				if (searchedType != null)
					return searchedType;
			}
			return null; // Not found.
		}


		internal string LanguageStyle(string prefix, string suffix)
		{
			if (this.GenericParameters.Count > 0)
			{
				StringBuilder sb = new StringBuilder(this.TypeName.Substring(0, this.TypeName.IndexOf('`')));
				sb.Append(prefix);
				bool pendingElement = false;
				foreach (var param in this.GenericParameters)
				{
					if (pendingElement)
						sb.Append(", ");
					sb.Append(param.LanguageStyle(prefix, suffix));
					pendingElement = true;
				}
				sb.Append(suffix);
				return sb.ToString();
			}
			else
				return this.TypeName;
		}

		class block
		{
			internal int iStart;
			internal int iEnd;
			internal int level;
			internal block parentBlock;
			internal List<block> innerBlocks = new List<block>();
			internal ParsedAssemblyQualifiedName parsedAssemblyQualifiedName;
		}

		static string LookForPairThenRemove(List<string> strings, string Name)
		{
			for (int istr = 0; istr < strings.Count; istr++)
			{
				string s = strings[istr];
				int i = s.IndexOf(Name);
				if (i == 0)
				{
					int i2 = s.IndexOf('=');
					if (i2 > 0)
					{
						string ret = s.Substring(i2 + 1);
						strings.RemoveAt(istr);
						return ret;
					}
				}
			}
			return null;
		}

#if DEBUG
		// Makes debugging easier.
		public override string ToString()
		{
			return this.CSharpStyleName.ToString();
		}
#endif
	}



}
