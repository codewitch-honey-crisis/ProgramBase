using System.ComponentModel;
using System.Reflection;
using System.Text;
/// <summary>
/// Provides basic information about the executing program
/// </summary>
public struct ProgramInfo
{
	/// <summary>
	/// The codebase of the executable
	/// </summary>
	public readonly string CodeBase;
	/// <summary>
	/// The filename, used for the using screen
	/// </summary>
	public readonly string Filename;
	/// <summary>
	/// The proper name of the assembly
	/// </summary>
	public readonly string Name;
	/// <summary>
	/// A description of the assembly used for the using screen
	/// </summary>
	public readonly string? Description;
	/// <summary>
	/// The version of the assembly used for the using screen
	/// </summary>
	public readonly Version? Version;
	public ProgramInfo(string codeBase, string filename, string name, string? description, Version? version)
	{
		CodeBase = codeBase;
		Filename = filename;
		Name = name;
		Description = description;
		Version = version;
	}
}
/// <summary>
/// Indicates the field or property is a command line argument
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class CmdArgAttribute : System.Attribute
{
	/// <summary>
	/// The name on the command line or &lt;default&gt; for the first unnamed argument
	/// </summary>
	public string? Name { get; set; } = null;
	/// <summary>
	/// True if it is required
	/// </summary>
	public bool Required { get; set; } = false;
	/// <summary>
	/// A description of the field for the using screen
	/// </summary>
	public string? Description { get; set; } = null;
	/// <summary>
	/// The name for value items
	/// </summary>
	public string? ElementName { get; set; } = null;
	/// <summary>
	/// Constructs a new instance
	/// </summary>
	/// <param name="name">The name on the command line or &lt;default&gt; for the first unnamed argument</param>
	/// <param name="required">True if it is required</param>
	/// <param name="description">A description of the field for the using screen</param>
	/// <param name="elementName">The name for value items</param>
	public CmdArgAttribute(string? name = null, bool required = false, string? description = null, string? elementName = null) { Name = name; Required = required; Description = description; ElementName = elementName; }
}
partial class Program
{
	private sealed class _DemandTextWriter : TextWriter
	{
		readonly string _name;
		StreamWriter? _writer = null;
		void EnsureWriter()
		{
			if (_writer == null)
			{
				_writer = new StreamWriter(_name, false);
			}
		}
		public override Encoding Encoding {
			get {
				if (_writer == null)
				{
					return Encoding.UTF8;
				}
				return _writer.Encoding;
			}
		}
		public _DemandTextWriter(string path)
		{
			_name = path;
		}
		public string Name {
			get {
				return _name;
			}
		}
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(_writer != null)
				{
					_writer.Close();
					_writer = null;
				}
			}
			base.Dispose(disposing);	
		}
		public override void Close()
		{
			if (_writer != null)
			{
				_writer.Close();
			}
			base.Close();
		}
		public override void Write(string? value)
		{
			EnsureWriter();
			_writer!.Write(value);
		}
		public override void WriteLine(string? value)
		{
			EnsureWriter();
			_writer!.WriteLine(value);
		}
	}
	/// <summary>
	/// Information about the executing assembly
	/// </summary>
	internal static readonly ProgramInfo Info = new ProgramInfo(_GetCodeBase(), Path.GetFileName(_GetCodeBase()), _GetName(), _GetDescription(), _GetVersion());

	const string _ProgressTwirl = "-\\|/";
	const char _ProgressBlock = '■';
	const string _ProgressBack = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
	const string _ProgressBackOne = "\b";
	static readonly StringBuilder _ProgressBuffer = new StringBuilder();
	/// <summary>
	/// Writes an indeterminate progress indicator to the screen
	/// </summary>
	/// <param name="progress">The integer progress indicator. Just keep incrementing this value.</param>
	/// <param name="update">False if this is the initial call, otherwise true</param>
	/// <param name="output">The <see cref="TextWriter"/> to write to</param>
	internal static void WriteProgress(int progress, bool update, TextWriter output)
	{
		_ProgressBuffer.Clear();
		if (update)
			_ProgressBuffer.Append(_ProgressBackOne);
		_ProgressBuffer.Append(_ProgressTwirl[progress % _ProgressTwirl.Length]);
		output.Write(_ProgressBuffer.ToString());
	}
	/// <summary>
	/// Writes a progress bar indicator to the screen
	/// </summary>
	/// <param name="progress">The integer progress indicator. Should be 0 to 100</param>
	/// <param name="update">False if this is the initial call, otherwise true</param>
	/// <param name="output">The <see cref="TextWriter"/> to write to</param>
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
			if (Environment.CommandLine[0] == '\"')
			{
				return Environment.CommandLine.Substring(1, Environment.CommandLine.IndexOf('\"', 1) - 1);
			}
			return Environment.CommandLine.Substring(0, Environment.CommandLine.IndexOf(' '));
		}
	}
	static Version? _GetVersion()
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
	static string? _GetDescription()
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
	private static string? _GetCmdArgName(MemberInfo member)
	{
		if (!(member is FieldInfo) && !(member is PropertyInfo))
		{
			return null;
		}
		var cmdArg = member.GetCustomAttribute(typeof(CmdArgAttribute), true);
		if (cmdArg != null)
		{
			var ca = cmdArg as CmdArgAttribute;
			var result = ca!.Name;
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
				if (!string.IsNullOrWhiteSpace(ca!.ElementName))
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
		if (t == null) return false;
		if (t.IsArray)
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
	private static string? _GetCmdArgDesc(MemberInfo member)
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
	static CmdArgAttribute? _CmdArgAttr(MemberInfo m)
	{
		return m.GetCustomAttribute(typeof(CmdArgAttribute), true) as CmdArgAttribute;
	}
	private static void _CmdArgsCrack(string[] args, Dictionary<string, MemberInfo> mappings)
	{
		if (mappings.Count == 0 && args.Length > 0)
		{
			throw new ArgumentException(string.Format("Unrecognized argument {0}", args[0]));
		}
		var argi = 0;
		string? defaultname = null;
		MemberInfo? defaultMember;
		CmdArgAttribute? cmdArgAttr;
		var found = new HashSet<string>();
		mappings.TryGetValue("<default>", out defaultMember);
		if (defaultMember != null)
		{
			found.Add("<default>");
			cmdArgAttr = _CmdArgAttr(defaultMember);
			if (args.Length == 0 || args[0][0] == '/')
			{

				if (cmdArgAttr!.Required)
					throw new ArgumentException(string.Format("<default> must be specified."));
			}
			else
			{
				var o = _CmdArgGetDefaultValue(defaultMember);
				Type et = _CmdArgGetType(defaultMember)!;
				var isarr = et.IsArray;
				MethodInfo? coladd = null;
				MethodInfo? colclear = null;
				MethodInfo? parse = null;
				if (isarr)
				{
					et = et.GetElementType()!;
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
							coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[] { et }, null);
							colclear = it.GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
						}
					}
				}
				var isreader = typeof(TextReader) == et;
				var iswriter = typeof(TextWriter) == et;

				TypeConverter? conv = _CmdArgGetConv(defaultMember, et);
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
							parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);

						}
						bt = bt.BaseType;
					}

				}
				if (!isarr && coladd == null && !isreader && !iswriter && !(o is string) && conv == null)
					throw new InvalidProgramException(string.Format("Type for {0} must be string or a collection, array or convertible type", defaultname));
				if (isarr)
				{
					o = Array.CreateInstance(et, 0);
					_CmdArgSetValue(defaultMember, o);
				}
				if (colclear != null)
				{
					if (o != null)
					{
						colclear.Invoke(o, null);
					}
				}
				for (; argi < args.Length; ++argi)
				{
					var arg = args[argi];
					if (arg[0] == '/') break;
					if (isarr)
					{
						Array? arr = (Array)o!;
						Array newArr;
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
						if (isreader == true)
						{
							try
							{
								v = new StreamReader(arg);
							}
							catch (Exception e)
							{
								throw new ArgumentException("File not found", arg, e);
							}
						}
						else if (iswriter == true)
						{
							v = new _DemandTextWriter(arg);
						}
						else
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { arg })!;
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(arg)!;
						}
						newArr.SetValue(v, newArr.Length - 1);
						_CmdArgSetValue(defaultMember, newArr);
						o = newArr;
					}
					else if (coladd != null)
					{
						if (o == null)
						{
							o = Activator.CreateInstance(_CmdArgGetType(defaultMember)!);
							_CmdArgSetValue(defaultMember, o);
						}
						object v;
						v = arg;
						if (isreader == true)
						{
							v = new StreamReader(arg);
						}
						else if (iswriter == true)
						{
							v = new _DemandTextWriter(arg);
						}
						else
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { arg })!;
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(arg)!;
						}
						coladd.Invoke(o, new object[] { v });
					}
					else if (isreader)
					{

						StreamReader reader;
						try
						{
							reader = new StreamReader(arg);
						}
						catch (Exception ex)
						{
							throw new ArgumentException("The file could could not be found", arg, ex);
						}
						_CmdArgSetValue(defaultMember, reader);
					}
					else if (iswriter)
					{
						_CmdArgSetValue(defaultMember, new _DemandTextWriter(arg));
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

			if (!mappings.TryGetValue(arg, out member!))
			{
				throw new InvalidProgramException(string.Format("Unknown switch /{0}", arg));
			}

			Type et = _CmdArgGetType(member)!;
			o = _CmdArgGetValueRaw(member)!;
			var isarr = et.IsArray;
			MethodInfo? coladd = null;
			MethodInfo? colclear = null;
			MethodInfo? parse = null;
			var isbool = et == typeof(bool);
			var isstr = et == typeof(string);
			if (isarr)
			{
				et = et.GetElementType()!;
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
						coladd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[] { et }, null);
						colclear = it.GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
						break;

					}
				}
			}
			var isreader = typeof(TextReader) == et;
			var iswriter = typeof(TextWriter) == et;

			TypeConverter? conv = _CmdArgGetConv(member, et);
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
						parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);

					}
					bt = bt.BaseType;
				}

			}
			if (isarr)
			{
				o = Array.CreateInstance(et, 0);
				_CmdArgSetValue(member, o);
			}
			if (colclear != null)
			{
				if (o != null)
				{
					colclear.Invoke(o, null);
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

						var arr = (Array)o!;
						Array newArr;
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
						if (isreader)
						{
							try
							{
								v = new StreamReader(sarg);
							}
							catch (Exception ex)
							{
								throw new ArgumentException("The file could not be found", arg, ex);
							}
						}
						else if (iswriter)
						{
							v = new _DemandTextWriter(sarg);
						}
						else
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { sarg })!;
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(sarg)!;
						}
						newArr.SetValue(v, arr!.Length - 1);
						o = newArr;
						_CmdArgSetValue(member, newArr);

					}
					else if (coladd != null)
					{
						if (o == null)
						{
							o = Activator.CreateInstance(_CmdArgGetType(member)!)!;
							_CmdArgSetValue(member, o);
						}
						object v = sarg;
						if (conv == null)
						{
							if (parse != null)
							{

								v = parse.Invoke(null, new object[] { sarg })!;
							}
						}
						else
						{
							v = conv.ConvertFromInvariantString(sarg)!;
						}
						coladd.Invoke(o, new object[] { v });
					}
				}
			}
			else if (isreader)
			{
				if (argi == args.Length - 1)
					throw new ArgumentException(string.Format("Missing value for /{0}", arg));
				_CmdArgSetValue(member, new StreamReader(args[++argi]));
			}
			else if (iswriter)
			{
				if (argi == args.Length - 1)
					throw new ArgumentException(string.Format("Missing value for /{0}", arg));
				_CmdArgSetValue(member, new _DemandTextWriter(args[++argi]));
			}
			else if (isstr)
			{
				if (argi == args.Length - 1)
					throw new ArgumentException(string.Format("Missing value for /{0}", arg));
				var sarg = args[++argi];
				member = mappings[arg];
				o = _CmdArgGetValueRaw(member)!;
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
			if (_CmdArgAttr(map.Value)!.Required)
			{
				if (!found.Contains(map.Key))
				{
					throw new ArgumentException(string.Format("Missing required switch /{0}", map.Key));
				}
			}
		}
	}
	private static object? _CmdArgGetDefaultValue(MemberInfo m)
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
	private static TypeConverter? _CmdArgGetConv(MemberInfo m, Type et)
	{
		TypeConverter? result = null;
		var attr = m.GetCustomAttribute(typeof(TypeConverterAttribute), true) as TypeConverterAttribute;
		if (attr != null)
		{
			Type? t = Type.GetType(attr.ConverterTypeName);
			if (t != null)
			{
				result = Activator.CreateInstance(t) as TypeConverter;
				if (result!=null && !result.CanConvertFrom(typeof(string)))
				{
					result = null;
				}
			}
		}
		if (result == null)
		{
			result = TypeDescriptor.GetConverter(et);
			if (!result.CanConvertFrom(typeof(string)))
			{
				result = null;
			}
		}
		return result;
	}
	private static object? _CmdArgGetValueRaw(MemberInfo m)
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
	private static object?[]? _CmdArgGetValues(MemberInfo m)
	{
		if (m == null) return null;
		var pi = m as PropertyInfo;
		object? o = null;
		if (pi != null)
		{
			o = pi.GetValue(null);
		}
		var fi = m as FieldInfo;
		if (fi != null)
		{
			o = fi.GetValue(null);
		}
		if (o == null)
		{
			if (_GetCmdArgIsList(m))
			{
				return new object?[0];
			}
			return new object?[] { null };
		}
		if (_GetCmdArgIsList(m))
		{
			if (o.GetType().IsArray)
			{
				var arr = (Array)o;
				var result = new object[arr.Length];
				Array.Copy(arr, result, arr.Length);
				return result;
			}
			else
			{
				var col = o as System.Collections.ICollection;
				if (o == null) { throw new NotSupportedException("This collection cannot be retrieved"); }
				var result = new object?[col!.Count];
				col.CopyTo(result, 0);
				return result;
			}
		}
		return new object?[] { o };
	}
	private static void _CmdArgSetValue(MemberInfo m, object? v)
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
	private static Type? _CmdArgGetType(MemberInfo m)
	{
		
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
	private static Type? _CmdArgGetElemType(MemberInfo m)
	{
		var t = _CmdArgGetType(m);
		if (t == null) return null;
		if (t.IsArray)
		{
			return t.GetElementType();
		}
		foreach (var it in t.GetInterfaces())
		{
			if (!it.IsGenericType) continue;
			var tdef = it.GetGenericTypeDefinition();
			if (typeof(ICollection<>) == tdef)
			{

				return tdef.GetGenericArguments()[0];

			}
		}
		return t;
	}
	/// <summary>
	/// Prints the Usage screen
	/// </summary>
	/// <param name="width">The width of the display, in characters. Defaults to approximate the console window width</param>
	internal static void PrintUsage(int width = 0)
	{
		if (width == 0)
		{
			width = Console.WindowWidth / 2;
		}
		var mappings = _CmdArgsReflect();
		TextWriter w = Console.Error;
		var sb = new StringBuilder();
		var sba = new StringBuilder();
		sb.Append("Usage: ");
		sb.Append(Path.GetFileNameWithoutExtension(Info.Filename));
		sb.Append(" ");
		var descmap = new Dictionary<string, string>();
		var remaining = width - sb.Length;
		int maxNameLen = 2;
		foreach (var m in mappings)
		{
			sba.Clear();
			if (remaining < 0)
			{
				sb.Append(Environment.NewLine);
				remaining = width;
			}
			var attr = _CmdArgAttr(m.Value)!;
			var desc = _GetCmdArgDesc(m.Value);
			var list = _GetCmdArgIsList(m.Value);
			var name = _GetCmdArgName(m.Value);
			var type = _CmdArgGetType(m.Value);
			if (!string.IsNullOrWhiteSpace(desc))
			{
				descmap.Add(m.Key, desc);
			}
			else
			{
				descmap.Add(m.Key, "");
			}
			if (!attr.Required)
			{
				sba.Append("[ ");
			}
			if (attr.Name != "<default>")
			{
				if ((m.Key.Length + 1) > maxNameLen)
				{
					maxNameLen = m.Key.Length + 1;
				}

				sba.Append('/');
				sba.Append(attr.Name);
				// doesn't have arguments:
				if (type != typeof(bool))
				{
					sba.Append(' ');
				}
			}
			else
			{
				if ((m.Key.Length) > maxNameLen)
				{
					maxNameLen = m.Key.Length;
				}

			}
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
				for (int i = 1; i < 3; ++i)
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
				sba.Append(itemName + "(N)> ");
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
			else if (type != typeof(bool))
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
					remaining = width - 4;
				}
				else
				{
					remaining -= sba.Length;
				}
				sb.Append(sba);
				sba.Clear();
			}
			else
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
		if (!string.IsNullOrWhiteSpace(Info.Name) && Info.Version != null)
		{
			w.WriteLine();
			w.Write(Info.Name);
			w.Write(" v");
			w.WriteLine(Info.Version.ToString());
			printedName = true;
		}
		if (!string.IsNullOrWhiteSpace(Info.Description))
		{
			w.WriteLine(WordWrap(Info.Description, width, 4));
			w.WriteLine();
		}
		else if (printedName)
		{
			Console.WriteLine();
		}
		foreach (var m in mappings)
		{
			if (m.Key != "<default>")
			{
				w.Write('/');
				w.Write(m.Key);
				w.Write(new string(' ', maxNameLen + 1 - m.Key.Length));
			}
			else
			{
				w.Write(m.Key);
				w.Write(new string(' ', maxNameLen + 2 - m.Key.Length));
			}
			var d = _GetCmdArgDesc(m.Value);
			if (!string.IsNullOrWhiteSpace(d))
			{
				w.WriteLine(WordWrap(d!, width, 4, maxNameLen + 2).Trim());
			}
			else
			{
				w.WriteLine();
			}
		}
		w.Write("/?");
		w.Write(new string(' ', maxNameLen));
		w.WriteLine(WordWrap("Displays this screen and exits", width, 4, maxNameLen + 2).Trim());
		w.WriteLine();
	}
	/// <summary>
	/// Indicates whether outputfile doesn't exist or is old
	/// </summary>
	/// <param name="inputfile">The master file to check the date of</param>
	/// <param name="outputfile">The output file which is compared against <paramref name="inputfile"/></param>
	/// <returns>True if <paramref name="outputfile"/> doesn't exist or is older than <paramref name="inputfile"/></returns>
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
	/// <summary>
	/// Indicates whether outputfile doesn't exist or is old
	/// </summary>
	/// <param name="input">The input reader to check the date of</param>
	/// <param name="output">The output writer which is compared against <paramref name="input"/></param>
	/// <returns>True if the file behind <paramref name="output"/> doesn't exist or is older than the file behind <paramref name="input"/> or if any are not files.</returns>
	public static bool IsStale(TextReader input, TextWriter output)
	{
		var result = true;
		var inputfile = GetFilename(input);
		if (inputfile == null)
		{
			return result;
		}
		var outputfile = GetFilename(output);
		if (outputfile == null)
		{
			return result;
		}
		// File.Exists doesn't always work right
		try
		{
			if (File.GetLastWriteTimeUtc(outputfile) >= File.GetLastWriteTimeUtc(inputfile))
				result = false;
		}
		catch { }
		return result;
	}
	/// <summary>
	/// Indicates whether <paramref name="output"/>'s file doesn't exist or is old
	/// </summary>
	/// <param name="inputs">The master files to check the date of</param>
	/// <param name="output">The output file which is compared against each of the <paramref name="inputs"/></param>
	/// <returns>True if <paramref name="output"/> doesn't exist or is older than <paramref name="inputs"/> or if any don't refer to a file</returns>
	public static bool IsStale(IEnumerable<TextReader> inputs, TextWriter output)
	{
		var result = true;
		foreach (var input in inputs)
		{
			result = false;
			if (IsStale(input, output))
			{
				result = true;
				break;
			}
		}
		return result;
	}
	/// <summary>
	/// Gets the filename for a <see cref="TextReader"/>if available
	/// </summary>
	/// <param name="t">The <see cref="TextReader"/> to examine</param>
	/// <returns>The filename, if available, or null</returns>
	public static string? GetFilename(TextReader t)
	{
		var sr = t as StreamReader;
		string? result = null;
		if (sr != null)
		{
			var fstm = sr.BaseStream as FileStream;
			if (fstm != null)
			{
				result = fstm.Name;
			}
		}
		if (!string.IsNullOrEmpty(result))
		{
			return result;
		}
		return null;
	}
	/// <summary>
	/// Gets the filename for a <see cref="TextWriter"/>if available
	/// </summary>
	/// <param name="t">The <see cref="TextWriter"/> to examine</param>
	/// <returns>The filename, if available, or null</returns>
	public static string? GetFilename(TextWriter t)
	{
		var dtw = t as _DemandTextWriter;
		if (dtw != null)
		{
			return dtw.Name;
		}
		var sw = t as StreamWriter;
		string? result = null;
		if (sw != null)
		{
			var fstm = sw.BaseStream as FileStream;
			if (fstm != null)
			{
				result = fstm.Name;
			}
		}
		if (!string.IsNullOrEmpty(result))
		{
			return result;
		}
		return null;
	}
	/// <summary>
	/// Performs word wrapping
	/// </summary>
	/// <param name="text">The text to wrap</param>
	/// <param name="width">The width of the display. Tries to approximate if zero</param>
	/// <param name="indent">The indent for successive lines, in number of spaces</param>
	/// <param name="startOffset">The starting offset of the first line where the text begins</param>
	/// <returns>Wrapped text</returns>
	public static string WordWrap(string text, int width = 0, int indent = 0, int startOffset = 0)
	{
		if (width == 0)
		{
			width = Console.WindowWidth / 2;
		}
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
				if (result.Length > 0)
				{
					result.Append(Environment.NewLine);
					if (indent > 0)
					{
						result.Append(new string(' ', indent));
					}
					first = false;

				}
				result.Append(actualLine.ToString());
				actualLine.Clear();
				actualWidth = indent;
			}

		}
		if (actualLine.Length > 0)
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

		}
		return result.ToString().TrimEnd();
	}
	public static int Main(string[] args)
	{
		if (args.Length == 1 && args[0] == "/?")
		{
			PrintUsage();
			return 0;
		}
		Dictionary<string, MemberInfo> mappings = _CmdArgsReflect();
#if !DEBUG
		var parsedArgs = false;
#endif
		try
		{
			_CmdArgsCrack(args, mappings);
#if !DEBUG
			parsedArgs = true;
#endif
			Run();
		}
#if !DEBUG

		catch (Exception ex)
		{
			if (!parsedArgs)
			{
				PrintUsage();
			}
			return _ReportError(ex);
		}
#endif
		finally
		{
			foreach (var m in mappings.Values)
			{
				var vals = _CmdArgGetValues(m);
				if (vals != null)
				{
					foreach (var v in vals)
					{
						var disp = v as IDisposable;
						if (disp != null && !object.ReferenceEquals(v, Console.In) && !object.ReferenceEquals(v, Console.Out) && !object.ReferenceEquals(v, Console.Error))
						{
							disp!.Dispose();
						}
					}
				}
			}
		}
		return 0;
	}
#if !DEBUG
	private static int _ReportError(Exception ex)
	{
		Console.Error.WriteLine("Error: " + ex.Message);
		return ex.HResult;
	}
#endif
}
