using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

public struct ProgramInfo
{
	public readonly string CodeBase;
	public readonly string Filename;
	public readonly string Name;
	public readonly string Description;
	public readonly Version Version;
	public ProgramInfo(string codeBase, string filename, string name, string description, Version version)
	{
		CodeBase = codeBase;
		Filename = filename;
		Name = name;
		Description = description;
		Version = version;
	}
}
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field,AllowMultiple =false,Inherited = true)]
public sealed class CmdArgAttribute : System.Attribute
{
	public string Name { get; set; } = null;
	public bool Required { get; set; } = false;
	public string Description { get; set; } = null;
	public string ElementName { get; set; } = null;
	public CmdArgAttribute(string name = null, bool required = false, string description = null, string elementName = null) { Name = name; Required = required; Description = description; ElementName = elementName; }
}
partial class Program
{
	internal static readonly ProgramInfo Info = new ProgramInfo(_GetCodeBase(), Path.GetFileName(_GetCodeBase()), _GetName(), _GetDescription(),_GetVersion());
	
	const string _ProgressTwirl = "-\\|/";
	const char _ProgressBlock = '■';
	const string _ProgressBack = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
	const string _ProgressBackOne = "\b";
	static readonly StringBuilder _ProgressBuffer = new StringBuilder();

	internal static void WriteProgress(int progress, bool update, TextWriter output)
	{
		_ProgressBuffer.Clear();
		if (update)
			_ProgressBuffer.Append(_ProgressBackOne);
		_ProgressBuffer.Append(_ProgressTwirl[progress % _ProgressTwirl.Length]);
		output.Write(_ProgressBuffer.ToString());
	}
	internal static void WriteProgressBar(int percent, bool update, TextWriter output)
	{
		_ProgressBuffer.Clear();
		if (update)
			_ProgressBuffer.Append(_ProgressBack);
		_ProgressBuffer.Append("[");
		var p = (int)((percent / 10f) + .5f);
		for (var i = 0; i < 10; ++i)
		{
			if (i >= p)
				_ProgressBuffer.Append(' ');
			else
				_ProgressBuffer.Append(_ProgressBlock);
		}
		_ProgressBuffer.Append(string.Format("] {0,3:##0}%", percent));
		output.Write(_ProgressBuffer.ToString());
	}
	static string _GetCodeBase()
	{
		try
		{
			return Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
		}
		catch
		{
			if (Environment.CommandLine[0]=='\"')
			{
				return Environment.CommandLine.Substring(1, Environment.CommandLine.IndexOf('\"',1)-1);
			}
			return Environment.CommandLine.Substring(0, Environment.CommandLine.IndexOf(' '));
		}
	}
	static Version _GetVersion()
	{
		try
		{
			return Assembly.GetExecutingAssembly().GetName().Version;
		}
		catch
		{
			return null;
		}
	}
	static string _GetName()
	{
		try
		{
			foreach (var attr in Assembly.GetExecutingAssembly().CustomAttributes)
			{
				if (typeof(AssemblyTitleAttribute) == attr.AttributeType)
				{
					var str = attr.ConstructorArguments[0].Value as string;
					if (!string.IsNullOrWhiteSpace(str))
					{
						return str;
					}

				}
			}
		}
		catch { }
		return Path.GetFileNameWithoutExtension(_GetCodeBase());
	}
	static string _GetDescription()
	{
		try
		{
			foreach (var attr in Assembly.GetExecutingAssembly().CustomAttributes)
			{
				if (typeof(AssemblyDescriptionAttribute) == attr.AttributeType)
				{
					var str = attr.ConstructorArguments[0].Value as string;
					if (!string.IsNullOrWhiteSpace(str))
					{
						return str;
					}
				}
			}
		}
		catch { }
		return null;
	}
	private static string _GetCmdArgName(MemberInfo member)
	{
		if (!(member is FieldInfo) && !(member is PropertyInfo))
		{
			return null;
		}
		var cmdArg = member.GetCustomAttribute(typeof(CmdArgAttribute), true);
		if (cmdArg != null)
		{
			var ca = cmdArg as CmdArgAttribute;
			var result = ca.Name;
			if (string.IsNullOrWhiteSpace(result))
			{
				result = member.Name;
			}
			return result;
		}
		return null;
	}
	private static string _GetCmdArgElemName(MemberInfo member)
	{
		var result = "item";
		if ((member is FieldInfo) || (member is PropertyInfo))
		{
			var cmdArg = member.GetCustomAttribute(typeof(CmdArgAttribute), true);
			if (cmdArg != null)
			{
				var ca = cmdArg as CmdArgAttribute;
				if (!string.IsNullOrWhiteSpace(ca.ElementName))
				{
					result = ca.ElementName;
				}
			}
		}
		return result;
	}
	private static bool _GetCmdArgIsList(MemberInfo member)
	{
		if (!(member is FieldInfo) && !(member is PropertyInfo))
		{
			return false;
		}
		var t = _CmdArgGetType(member);
		if(t.IsArray)
		{
			return true;
		}
		foreach (var it in t.GetInterfaces())
		{
			if (!it.IsGenericType) continue;
			var tdef = it.GetGenericTypeDefinition();
			if (typeof(ICollection<>) == tdef)
			{

				return true;

			}
		}
		return false;
	}
	private static string _GetCmdArgDesc(MemberInfo member)
	{
		if (!(member is FieldInfo) && !(member is PropertyInfo))
		{
			return null;
		}
		var cmdarg = member.GetCustomAttribute(typeof(CmdArgAttribute), true) as CmdArgAttribute;
		if (cmdarg != null)
		{
			var result = cmdarg.Description;
			if (result != null)
			{
				return result;
			}
		}
		var desc = member.GetCustomAttribute(typeof(DescriptionAttribute), true) as DescriptionAttribute;
		if (desc != null)
		{
			var result = desc.Description;
			if (string.IsNullOrWhiteSpace(result))
			{
				result = "";
			}
			return result;
		}
		return "";
	}
	static Dictionary<string, MemberInfo> _CmdArgsReflect()
	{
		var mia = typeof(Program).GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		var result = new Dictionary<string, MemberInfo>(mia.Length);
		for (int i = 0; i < mia.Length; i++)
		{
			var m = mia[i];
			var arg = _GetCmdArgName(m);
			if (arg != null)
			{
				// this is a cmd arg
				result.Add(arg, m);
			}
		}
		return result;
	}
	static CmdArgAttribute _CmdArgAttr(MemberInfo m)
	{
		return m.GetCustomAttribute(typeof(CmdArgAttribute), true) as CmdArgAttribute;
	}
	private static void _CmdArgsCrack(string[] args, Dictionary<string, MemberInfo> mappings)
	{
		if(mappings.Count==0 && args.Length>0)
		{
			throw new ArgumentException(string.Format("Unrecognized argument {0}", args[0]));
		}
		var argi = 0;
		string defaultname = null;
		MemberInfo defaultMember;
		CmdArgAttribute cmdArgAttr;
		var found = new HashSet<string>();
		mappings.TryGetValue("<default>", out defaultMember);
		if (defaultMember != null)
		{
			found.Add("<default>");
			cmdArgAttr = _CmdArgAttr(defaultMember);
			if (args.Length == 0 || args[0][0] == '/')
			{

				if (cmdArgAttr.Required)
					throw new ArgumentException(string.Format("<default> must be specified."));
			}
			else
			{
				var o = _CmdArgGetDefaultValue(defaultMember);
				Type et = _CmdArgGetType(defaultMember);
				var isarr = et.IsArray;
				MethodInfo coladd = null;
				MethodInfo parse = null;
				if (isarr)
				{
					et = et.GetElementType();
				}
				else
				{
					foreach (var it in et.GetInterfaces())
					{
						if (!it.IsGenericType) continue;
						var tdef = it.GetGenericTypeDefinition();
						if (typeof(ICollection<>) == tdef)
						{

							et = et.GenericTypeArguments[0];
							coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, new Type[] { et });

						}
					}
				}
				TypeConverter conv = TypeDescriptor.GetConverter(et);
				if (conv != null)
				{
					if (!conv.CanConvertFrom(typeof(string)))
					{
						conv = null;
					}
				}
				if (conv == null && !isarr && coladd == null)
				{
					var bt = et;
					while (parse == null && bt != null)
					{
						try
						{
							parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
						}
						catch (AmbiguousMatchException)
						{
							parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) });

						}
						bt = bt.BaseType;
					}
				}
				if (!isarr && coladd == null && !(o is string) && conv == null)
					throw new InvalidProgramException(string.Format("Type for {0} must be string or a collection, array or convertible type", defaultname));

				for (; argi < args.Length; ++argi)
				{
					var arg = args[argi];
					if (arg[0] == '/') break;
					if (isarr)
					{
						var arr = (Array)o;
						Array newArr = null;
						if (arr == null)
						{
							newArr = Array.CreateInstance(et, 1);
						}
						else
						{
							newArr = Array.CreateInstance(et, arr.Length + 1);
							Array.Copy(arr, newArr, newArr.Length - 1);
						}
						object v;
						v = arg;
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { arg });
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(arg);
						}
						newArr.SetValue(v, newArr.Length - 1);
						_CmdArgSetValue(defaultMember, newArr);
						o = newArr;
					}
					else if (coladd != null)
					{
						if (o == null)
						{
							o = Activator.CreateInstance(_CmdArgGetType(defaultMember));
							_CmdArgSetValue(defaultMember, o);
						}
						object v;
						v = arg;
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { arg });
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(arg);
						}
						coladd.Invoke(o, new object[] { v });
					}
					else if ("" == o as string)
					{
						_CmdArgSetValue(defaultMember, arg);
					}
					else if (conv != null)
					{
						_CmdArgSetValue(defaultMember, conv.ConvertFromInvariantString(arg));
					}
					else if (parse != null)
					{
						_CmdArgSetValue(defaultMember, parse.Invoke(null, new object[] { arg }));
					}
					else
						throw new ArgumentException(string.Format("Only one <{0}> value may be specified.", defaultname));
				}
			}
		}
		for (; argi < args.Length; ++argi)
		{
			var arg = args[argi];
			if (string.IsNullOrWhiteSpace(arg) || arg[0] != '/')
			{
				throw new ArgumentException(string.Format("Expected switch instead of {0}", arg));
			}
			arg = arg.Substring(1);
			if (!char.IsLetterOrDigit(arg, 0))
				throw new ArgumentException("Invalid switch /{0}", arg);
			MemberInfo member;
			object o;
			
			if (!mappings.TryGetValue(arg, out member))
			{
				throw new InvalidProgramException(string.Format("Unknown switch /{0}", arg));
			}
			
			Type et = _CmdArgGetType(member);
			o = _CmdArgGetValue(member);
			var isarr = et.IsArray;
			MethodInfo coladd = null;
			MethodInfo parse = null;
			var isbool = et == typeof(bool);
			var isstr = et == typeof(string);
			if (isarr)
			{
				et = et.GetElementType();
			}
			else
			{
				foreach (var it in et.GetInterfaces())
				{
					if (!it.IsGenericType) continue;
					var tdef = it.GetGenericTypeDefinition();
					if (typeof(ICollection<>) == tdef)
					{
						et = et.GenericTypeArguments[0];
						coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, new Type[] { et });
						break;

					}
				}
			}
			TypeConverter conv = TypeDescriptor.GetConverter(et);
			if (conv != null)
			{
				if (!conv.CanConvertFrom(typeof(string)))
				{
					conv = null;
				}
			}
			if (conv == null)
			{
				var bt = et;
				while (parse == null && bt != null)
				{
					try
					{
						parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
					}
					catch (AmbiguousMatchException)
					{
						parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) });

					}
					bt = bt.BaseType;
				}
			}
			if (isarr || coladd != null)
			{
				while (++argi < args.Length)
				{
					var sarg = args[argi];
					if (sarg[0] == '/')
					{
						--argi;
						break;
					}
					if (isarr)
					{

						var arr = (Array)o;
						Array newArr = null;
						if (arr == null)
						{
							newArr = Array.CreateInstance(et, 1);
						}
						else
						{
							newArr = Array.CreateInstance(et, arr.Length + 1);
							Array.Copy(arr, newArr, newArr.Length - 1);
						}
						object v = sarg;
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { sarg });
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(sarg);
						}
						newArr.SetValue(v, arr.Length - 1);
						o = newArr;
						_CmdArgSetValue(member, newArr);

					}
					else if (coladd != null)
					{
						if(o==null)
						{
							o = Activator.CreateInstance(_CmdArgGetType(member));
							_CmdArgSetValue(member, o);
						}
						object v = sarg;
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { sarg });
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(sarg);
						}
						coladd.Invoke(o, new object[] { v });
					}
				}
			}
			else if (isstr)
			{
				if (argi == args.Length - 1)
					throw new ArgumentException(string.Format("Missing value for /{0}", arg));
				var sarg = args[++argi];
				member = mappings[arg];
				o = _CmdArgGetValue(member);
				if (!found.Contains(member.Name))
				{
					_CmdArgSetValue(member, sarg);
				}
				else
					throw new ArgumentException(string.Format("Only one <{0}> value may be specified.", arg));
			}
			else if (isbool)
			{
				if (o is bool && (bool)o)
				{
					throw new ArgumentException(string.Format("Only one /{0} switch may be specified.", arg));
				}
				_CmdArgSetValue(member, true);
			}
			else if (conv != null)
			{
				if (argi == args.Length - 1)
					throw new ArgumentException(string.Format("Missing value for /{0}", arg));
				_CmdArgSetValue(member, conv.ConvertFromInvariantString(args[++argi]));
			}
			else if (parse != null)
			{
				_CmdArgSetValue(member, parse.Invoke(o, new object[] { args[++argi] }));
			}
			else
				throw new InvalidProgramException(string.Format("Type for {0} must be a boolean, a string, a string collection, a string array, or a convertible type", arg));
			found.Add(arg);
		}
		foreach (var map in mappings)
		{
			if (_CmdArgAttr(map.Value).Required)
			{
				if (!found.Contains(map.Key))
				{
					throw new ArgumentException(string.Format("Missing required switch /{0}", map.Key));
				}
			}
		}
	}
	private static object _CmdArgGetDefaultValue(MemberInfo m)
	{
		if (m == null) return null;
		var dva = m.GetCustomAttribute(typeof(DefaultValueAttribute), true);
		if (dva != null)
		{
			var tdva = (DefaultValueAttribute)dva;
			if (tdva.Value != null)
			{
				return tdva.Value;
			}
		}
		var pi = m as PropertyInfo;
		if (pi != null)
		{
			return pi.GetValue(null);
		}
		var fi = m as FieldInfo;
		if (fi != null)
		{
			return fi.GetValue(null);
		}
		return null;
	}
	private static object _CmdArgGetValue(MemberInfo m)
	{
		if (m == null) return null;
		var pi = m as PropertyInfo;
		if (pi != null)
		{
			return pi.GetValue(null);
		}
		var fi = m as FieldInfo;
		if (fi != null)
		{
			return fi.GetValue(null);
		}
		return null;
	}
	private static void _CmdArgSetValue(MemberInfo m, object v)
	{
		if (m == null) return;
		var pi = m as PropertyInfo;
		if (pi != null)
		{
			pi.SetValue(null, v);
			return;
		}
		var fi = m as FieldInfo;
		if (fi != null)
		{
			fi.SetValue(null, v);
		}
	}
	private static Type _CmdArgGetType(MemberInfo m)
	{
		if (m == null) return null;

		var pi = m as PropertyInfo;
		if (pi != null)
		{
			return pi.PropertyType;
		}
		var fi = m as FieldInfo;
		if (fi != null)
		{
			return fi.FieldType;
		}
		return null;
	}
	internal static void PrintUsage(int width = 60)
	{
		var mappings = _CmdArgsReflect();
		TextWriter w = Console.Error;
		var sb = new StringBuilder();
		var sba = new StringBuilder();
		sb.Append("Usage: ");
		sb.Append(Path.GetFileNameWithoutExtension(Info.Filename));
		sb.Append(" ");
		var descmap = new Dictionary<string, string>();
		var remaining = width - sb.Length;
		int maxNameLen = 0;
		foreach ( var m in mappings)
		{
			sba.Clear();
            if (remaining<0)
            {
				sb.Append(Environment.NewLine);
				remaining = width;
            }
            CmdArgAttribute attr = _CmdArgAttr(m.Value);
			string desc = _GetCmdArgDesc(m.Value);
			var list = _GetCmdArgIsList(m.Value);
			var name = _GetCmdArgName(m.Value);
			if(!string.IsNullOrWhiteSpace(desc))
			{
				descmap.Add(m.Key, desc);
			} else
			{
				descmap.Add(m.Key, "");
			}
			if(!attr.Required)
			{
				sba.Append("[ ");
			}
			if(attr.Name!="<default>")
			{
				if ((m.Key.Length + 1) > maxNameLen)
				{
					maxNameLen = m.Key.Length + 1;
				}

				sba.Append('/');
				sba.Append(attr.Name);
				sba.Append(' ');
			} else
			{
				if ((m.Key.Length ) > maxNameLen)
				{
					maxNameLen = m.Key.Length ;
				}

			}
			if (remaining-sba.Length<0)
			{
				sb.Append(Environment.NewLine);
				sb.Append("    ");
				remaining = width - 4;
			}
			else
			{
				remaining -= sba.Length;
			}
			sb.Append(sba);
			sba.Clear();
			var itemName = _GetCmdArgElemName(m.Value);

			if (list)
			{				
				sba.Append("{ ");
				if (remaining - sba.Length < 0)
				{
					sb.Append(Environment.NewLine);
					sb.Append("    ");
					remaining = width - 4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				
				sba.Clear();
				for (int i = 1;i<3;++i)
				{
					sba.Append('<');
					sba.Append(itemName + i.ToString());
					sba.Append("> ");
					if (remaining - sba.Length < 0)
					{
						sb.Append(Environment.NewLine);
						sb.Append("    ");
						remaining = width - 4;
					}
					else
					{
						remaining -= sba.Length;
					}
					sb.Append(sba);
					sba.Clear();
				}
				sba.Append('<');
				sba.Append(itemName+"(N)> ");
				if (remaining - sba.Length < 0)
				{
					sb.Append(Environment.NewLine);
					sb.Append("    ");
					remaining = width - 4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				sba.Clear();
			} else
			{
				sba.Append('<');
				sba.Append(itemName);
				sba.Append('>');
				if (remaining - sba.Length < 0)
				{
					sb.Append(Environment.NewLine);
					sb.Append("    ");
					remaining = width - 4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				sba.Clear();
			}

			if (list)
			{
				sba.Append("} ");
				if (remaining - sba.Length < 0)
				{
					sb.Append(Environment.NewLine);
					sb.Append("    ");
					remaining = width-4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				sba.Clear();
			} else
			{
				sba.Append(' ');
			}
			if (!attr.Required)
			{
				sba.Append("] ");
				if (remaining - sba.Length < 0)
				{
					sb.Append(Environment.NewLine);
					sb.Append("    ");
					remaining = width - 4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				sba.Clear();
			}
		}
		
		w.WriteLine(sb.ToString().Trim());
		var printedName = false;
		if(!string.IsNullOrWhiteSpace(Info.Name) && Info.Version!=null)
		{
			w.WriteLine();
			w.Write(Info.Name);
			w.Write(" v");
			w.WriteLine(Info.Version.ToString());
			printedName = true;
		}
		if(!string.IsNullOrWhiteSpace(Info.Description))
		{
			w.WriteLine(WrapText(Info.Description,width,4));
			w.WriteLine();
		} else if(printedName)
		{
			Console.WriteLine();
		}
		foreach(var m in mappings)
		{
			if (m.Key != "<default>")
			{
				w.Write('/');
				w.Write(m.Key);
				w.Write(new string(' ', maxNameLen + 1 - m.Key.Length));
			} else
			{
				w.Write(m.Key);
				w.Write(new string(' ', maxNameLen + 2 - m.Key.Length));
			}
			
			w.WriteLine(WrapText(_GetCmdArgDesc(m.Value), width, 4, maxNameLen + 2).Trim());
		}
		w.Write("/?");
		w.Write(new string(' ', maxNameLen));
		w.WriteLine(WrapText("Displays this screen and exits", width, 4, maxNameLen + 2).Trim());
		w.WriteLine();
	}
	public static bool IsStale(string inputfile, string outputfile)
	{
		var result = true;
		// File.Exists doesn't always work right
		try
		{
			if (File.GetLastWriteTimeUtc(outputfile) >= File.GetLastWriteTimeUtc(inputfile))
				result = false;
		}
		catch { }
		return result;
	}
	public static string WrapText(string text, int width, int indent=0, int startOffset=0)
	{
		string[] originalLines = text.Split(new string[] { " " },
			StringSplitOptions.None);

		StringBuilder result = new StringBuilder();
		StringBuilder actualLine = new StringBuilder();
		double actualWidth = startOffset;
		var first = true;
		foreach (var item in originalLines)
		{
			actualLine.Append(item + " ");
			actualWidth += item.Length;

			if (actualWidth > width)
			{
				if(result.Length>0)
				{
					result.Append(Environment.NewLine);
					if (indent > 0)
					{
						result.Append(new string(' ', indent));
					}
					
				}
				result.Append(actualLine.ToString());
				actualLine.Clear();
				actualWidth = indent;
			}
			
		}
		if(actualLine.Length > 0)
		{
			if (!first)
			{
				result.Append(Environment.NewLine);
				if (indent > 0)
				{
					result.Append(new string(' ', indent));

				}
			}
			result.Append(actualLine.ToString());
			first = false;
		}
		return result.ToString();
	}
	public static int Main(string[] args)
	{
		if (args.Length == 1 && args[0] == "/?")
		{
			PrintUsage();
			return 0;
		}
#if !DEBUG
			try
			{
#endif
		var mappings = _CmdArgsReflect();
		_CmdArgsCrack(args, mappings);
		Run();

#if !DEBUG
			}
			catch(Exception ex) {
			return _ReportError(ex);
			}
#endif

		return 0;
	}
#if !DEBUG
	private static int _ReportError(Exception ex)
	{
		PrintUsage();
		Console.Error.WriteLine("Error: "+ex.Message);
		return ex.HResult;
	}
#endif
}
