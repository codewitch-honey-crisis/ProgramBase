#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
#region CmdArgAttribute
/// <summary>
/// Indicates the field or property is a command line argument
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
internal sealed class CmdArgAttribute : System.Attribute
{
	/// <summary>
	/// The name on the command line or null or empty to use a camel cased member name. Ignored for ordinal arguments
	/// </summary>
	public string Name { get; set; } = null;
	/// <summary>
	/// The ordinal position of the argument. -1 if the name is used
	/// </summary>
	public int Ordinal { get; set; } = -1;
	/// <summary>
	/// True if it is required
	/// </summary>
	public bool Required { get; set; } = false;
	/// <summary>
	/// A description of the field for the using screen
	/// </summary>
	public string Description { get; set; } = null;
	/// <summary>
	/// The name for value items
	/// </summary>
	public string ItemName { get; set; } = null;
	/// <summary>
	/// Constructs a new instance
	/// </summary>
	/// <param name="name">The name of the switch on the command line or ignored for ordinal arguments</param>
	/// <param name="ordinal">The ordinal position if the argument or -1 if it's named.</param>
	/// <param name="required">True if it is required</param>
	/// <param name="description">A description of the field for the using screen</param>
	/// <param name="itemName">The name for value items</param>
	public CmdArgAttribute(string name = null, int ordinal = -1, bool required = false, string description = null, string itemName = null) { Name = name; Required = required; Description = description; ItemName = itemName; }
}
#endregion // CmdArgAttribute
#region ProgramInfo
/// <summary>
/// Provides basic information about the executing program
/// </summary>
internal struct ProgramInfo
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
	public readonly string Description;
	/// <summary>
	/// A copyright of the assembly used for the using screen
	/// </summary>
	public readonly string Copyright;
	/// <summary>
	/// The version of the assembly used for the using screen
	/// </summary>
	public readonly Version Version;
	/// <summary>
	/// The command line prefix that started this application.
	/// </summary>
	/// <remarks>For hosted applications this may be different than the filename.</remarks>
	public readonly string CommandLinePrefix;

	public ProgramInfo(string codeBase, string filename, string name, string description, string copyright, Version version, string commandLinePrefix)
	{
		CodeBase = codeBase;
		Filename = filename;
		Name = name;
		Description = description;
		Copyright = copyright;
		Version = version;
		CommandLinePrefix = commandLinePrefix;
	}
}
#endregion // ProgramInfo
partial class Program
{
	internal static ProgramInfo Info;
	#region Basic Program Info
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
	static string _GetCopyright()
	{
		try
		{
			foreach (var attr in Assembly.GetExecutingAssembly().CustomAttributes)
			{
				if (typeof(AssemblyCopyrightAttribute) == attr.AttributeType)
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
	#endregion // Basic Program Info
	#region _DeferredTextWriter
	private sealed class _DeferredTextWriter : TextWriter
	{
		readonly string _name;
		StreamWriter _writer = null;
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
		public _DeferredTextWriter(string path)
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
			if (disposing)
			{
				if (_writer != null)
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
		public override void Write(string value)
		{
			EnsureWriter();
			_writer.Write(value);
		}
		public override void WriteLine(string value)
		{
			EnsureWriter();
			_writer.WriteLine(value);
		}
	}
	#endregion // _DeferredTextWriter
	#region _ArgInfo
	private struct _ArgInfo
	{
		public string Name;
		public int Ordinal;
		public string Description;
		public string ItemName;
		public MemberInfo Member;
		public bool IsOptional;
		public bool IsProperty;
		public bool IsTextReader;
		public bool IsTextWriter;
		public bool IsFileSystemInfo;
		public bool IsDirectoryInfo;
		public bool IsFileInfo;
		public bool HasArgument {
			get { return ElementType != typeof(bool); }
		}
		public Type Type;
		public Type ElementType;
		public bool IsCollection {
			get {
				return ColAdd != null && ColClear != null;
			}
		}
		public MethodInfo ColAdd;
		public MethodInfo ColClear;
		public MethodInfo Parse;
		public TypeConverter Converter;
		public bool IsArray;
		public object InitialValue;
		public object GetMemberValue()
		{
			if (IsProperty)
			{
				return ((PropertyInfo)Member).GetValue(null);
			}
			return ((FieldInfo)Member).GetValue(null);
		}
		public void SetMemberValue(object value)
		{
			if (IsProperty)
			{
				((PropertyInfo)Member).SetValue(null, value);
			}
			else
			{
				((FieldInfo)Member).SetValue(null, value);
			}
		}
		public object BeginList()
		{
			if (IsArray)
			{
				var arr = Array.CreateInstance(ElementType, 0);
				SetMemberValue(arr);
				return arr;
			}
			else if (IsCollection)
			{
				var col = GetMemberValue();
				if (col != null)
				{
					ColClear.Invoke(col, new object[0]);
				}
				else
				{
					col = Activator.CreateInstance(this.Type);
					SetMemberValue(col);
				}
				return col;
			}
			throw new InvalidOperationException("The argument is not a list");
		}
		public object AddToList(string input)
		{
			if (IsArray)
			{
				Array arr = (Array)GetMemberValue();
				var newArr = Array.CreateInstance(ElementType, arr.Length + 1);
				arr.CopyTo(newArr, 0);
				newArr.SetValue(InstantiateItem(input), arr.Length);
				SetMemberValue(newArr);
				return newArr;

			}
			else if (IsCollection)
			{
				var col = GetMemberValue();
				ColAdd.Invoke(col, new object[] { InstantiateItem(input) });
				return col;
			}
			throw new InvalidOperationException("The argument is not a list");
		}
		void _DestroyItem(object obj)
		{
			if (obj != null)
			{
				if (IsTextReader)
				{
					if (!object.ReferenceEquals(obj, Console.In))
					{
						((TextReader)obj).Close();
					}
				}
				else if (IsTextWriter)
				{
					if (!object.ReferenceEquals(obj, Console.Out) && !object.ReferenceEquals(obj, Console.Error))
					{
						((TextWriter)obj).Close();
					}
				}
				else
				{
					var disp = obj as IDisposable;
					if (disp != null)
					{
						disp.Dispose();
					}
				}
			}
		}
		public void Destroy()
		{
			var obj = GetMemberValue();
			if (IsArray || IsCollection)
			{
				var en = obj as System.Collections.IEnumerable;
				if (en != null)
				{
					foreach (var item in en)
					{
						_DestroyItem(item);
					}
				}
			}
			else
			{
				_DestroyItem(obj);
			}
			var disp = obj as IDisposable;
			if (disp != null)
			{
				disp.Dispose();
			}

		}
		public object InstantiateItem(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				throw new FormatException("Bad data passed for argument");
			}
			if (Converter != null)
			{
				try
				{
					return Converter.ConvertFrom(input);
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format("Error parsing <{0}> - could not comprehend \"{1}\"", ItemName, input, ex));
				}
			}
			if (IsTextReader)
			{
				try
				{
					return new StreamReader(input);
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format("Error opening <{0}> \"{1}\"", ItemName, input, ex));
				}
			}
			if (IsTextWriter)
			{
				return new _DeferredTextWriter(input);
			}
			if(IsFileInfo)
			{
				return new FileInfo(input);
			} else if(IsDirectoryInfo)
			{
				return new DirectoryInfo(input);
			} else if(IsFileSystemInfo)
			{
				if(input.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar } )==input.Length-1)
				{
					return new DirectoryInfo(input);
				} else
				{
					try
					{
						if (Directory.Exists(input))
						{
							return new DirectoryInfo(input);
						}
					}
					catch
					{

					}
					return new FileInfo(input);
				}
			}
			if (Parse != null)
			{
				try
				{
					return Parse.Invoke(null, new object[] { input });
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format("Error parsing <{0}> - could not comprehend \"{1}\"", ItemName, input, ex));
				}
			}
			throw new ArgumentException("Invalid input for argument <{0}>", ItemName);
		}
	}
	#endregion // _ArgInfo
	#region _GetUserExitCode
	static int _GetUserExitCode()
	{
		var mia = typeof(Program).GetMembers(BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);
		for (var i = 0; i < mia.Length; i++)
		{
			var m = mia[i];
			if (m.Name == "ExitCode")
			{
				var prop = m as PropertyInfo;
				var field = m as FieldInfo;
				if (prop != null)
				{
					if (prop.CanRead && typeof(int) == prop.PropertyType && prop.GetAccessors().Length == 0)
					{
						return (int)prop.GetValue(null);
					}
				}
				else if (field != null)
				{
					if (typeof(int) == field.FieldType)
					{
						return (int)field.GetValue(null);
					}
				}
			}
		}
		return 0;
	}
	#endregion // _GetUserExitCode
	private static readonly char[] _RestrictedChars = new char[] { ' ', '\r', '\t', '\n', '\n', '\b', '\"' };
	#region IsStale
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
	/// <param name="inputfile">The master file to check the date of</param>
	/// <param name="outputfile">The output file which is compared against <paramref name="inputfile"/></param>
	/// <returns>True if <paramref name="outputfile"/> doesn't exist or is older than <paramref name="inputfile"/></returns>
	public static bool IsStale(FileSystemInfo inputfile, FileSystemInfo outputfile)
	{
		var result = true;
		// File.Exists doesn't always work right
		try
		{
			if (File.GetLastWriteTimeUtc(outputfile.FullName) >= File.GetLastWriteTimeUtc(inputfile.FullName))
				result = false;
		}
		catch { }
		return result;
	}
	/// <summary>
	/// Indicates whether <paramref name="outputfile"/>'s file doesn't exist or is old
	/// </summary>
	/// <param name="inputfiles">The master files to check the date of</param>
	/// <param name="outputfile">The output file which is compared against each of the <paramref name="inputfiles"/></param>
	/// <returns>True if <paramref name="outputfile"/> doesn't exist or is older than <paramref name="inputfiles"/> or if any don't refer to a file</returns>
	public static bool IsStale(IEnumerable<FileSystemInfo> inputfiles, FileSystemInfo outputfile)
	{
		var result = true;
		foreach (var input in inputfiles)
		{
			result = false;
			if (IsStale(input, outputfile))
			{
				result = true;
				break;
			}
		}
		return result;
	}
	/// <summary>
	/// Indicates whether <paramref name="outputfile"/>'s file doesn't exist or is old
	/// </summary>
	/// <param name="inputfiles">The master files to check the date of</param>
	/// <param name="outputfile">The output file which is compared against each of the <paramref name="inputfiles"/></param>
	/// <returns>True if <paramref name="outputfile"/> doesn't exist or is older than <paramref name="inputfiles"/> or if any don't refer to a file</returns>
	public static bool IsStale(IEnumerable<FileInfo> inputfiles, FileInfo outputfile)
	{
		var result = true;
		foreach (var input in inputfiles)
		{
			result = false;
			if (IsStale(input, outputfile))
			{
				result = true;
				break;
			}
		}
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
	#endregion // IsStale
	#region GetFilename
	/// <summary>
	/// Gets the filename for a <see cref="TextReader"/>if available
	/// </summary>
	/// <param name="t">The <see cref="TextReader"/> to examine</param>
	/// <returns>The filename, if available, or null</returns>
	public static string GetFilename(TextReader t)
	{
		var sr = t as StreamReader;
		string result = null;
		if (sr != null)
		{
			FileStream fstm = sr.BaseStream as FileStream;
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
	public static string GetFilename(TextWriter t)
	{
		var dtw = t as _DeferredTextWriter;
		if (dtw != null)
		{
			return dtw.Name;
		}
		var sw = t as StreamWriter;
		string result = null;
		if (sw != null)
		{
			FileStream fstm = sw.BaseStream as FileStream;
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
	#endregion GetFilename
	#region _ReflectArguments
	private static IList<_ArgInfo> _ReflectArguments(Type type)
	{
		var mia = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
		var result = new List<_ArgInfo>(mia.Length);
		for (int i = 0; i < mia.Length; ++i)
		{
			var m = mia[i];
			var prop = m as PropertyInfo;
			var field = m as FieldInfo;
			if (field == null && prop == null) { continue; }
			var a = default(_ArgInfo);
			a.IsProperty = prop != null;
			var cmdArgAttr = m.GetCustomAttribute(typeof(CmdArgAttribute), true) as CmdArgAttribute;
			if (cmdArgAttr == null)
			{
				continue;
			}
			a.Name = cmdArgAttr.Name;
			a.Ordinal = cmdArgAttr.Ordinal;
			a.Description = cmdArgAttr.Description;
			a.Member = m;
			a.ItemName = cmdArgAttr.ItemName;
			a.IsOptional = cmdArgAttr.Required == false;
			// fetch the name off the member and camel case it
			if (string.IsNullOrWhiteSpace(a.Name))
			{
				switch (m.Name.Length)
				{
					case 0: // shouldn't happen
						a.Name = null;
						break;
					case 1:
					case 2:
						a.Name = m.Name.ToLowerInvariant();
						break;
					default:
						a.Name = m.Name.Substring(0, 2).ToLowerInvariant() + m.Name.Substring(2);
						break;
				}
			}

			a.Type = a.IsProperty ? prop.PropertyType : field.FieldType;
			a.ElementType = a.Type;
			a.ColClear = null;
			a.ColAdd = null;
			if (a.Type.IsArray)
			{
				a.IsArray = true;
				a.ElementType = a.Type.GetElementType();
			}
			else
			{
				a.IsArray = false;
				foreach (var it in a.Type.GetInterfaces())
				{
					if (!it.IsGenericType) continue;
					var tdef = it.GetGenericTypeDefinition();
					if (typeof(ICollection<>) == tdef)
					{
						a.ElementType = a.Type.GenericTypeArguments[0];
						a.ColAdd = it.GetMethod("Add", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[] { a.ElementType }, null);
						a.ColClear = it.GetMethod("Clear", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null);
					}
				}
			}

			if ((a.IsArray || a.IsCollection) && !a.HasArgument)
			{
				throw new InvalidProgramException(string.Format("bool arguments such as {0} cannot be arrays or collections because there's no way to indicate that", a.Member.Name));
			}
			else if (a.Ordinal > -1 && !a.HasArgument)
			{
				throw new InvalidProgramException(string.Format("bool arguments such as {0} cannot have ordinal positions because there's no way to indicate that", a.Member.Name));
			}
			if (!a.IsOptional && !a.HasArgument)
			{
				throw new InvalidProgramException(string.Format("bool arguments such as {0} cannot be required because that doesn't make sense", a.Member.Name));
			}
			a.InitialValue = a.IsProperty ? prop.GetValue(null) : field.GetValue(null);
			// find the type converter
			var tca = m.GetCustomAttribute(typeof(TypeConverterAttribute), true) as TypeConverterAttribute;
			if (tca != null)
			{
				var tct = Type.GetType(tca.ConverterTypeName);
				if (tct != null)
				{
					a.Converter = Activator.CreateInstance(tct) as TypeConverter;
					if (a.Converter != null && !a.Converter.CanConvertFrom(typeof(string)))
					{
						a.Converter = null;
					}
				}
			}
			if (a.Converter == null && !a.IsArray && !a.IsCollection)
			{
				if (a.InitialValue != null)
				{
					a.Converter = TypeDescriptor.GetConverter(a.InitialValue);
					if (a.Converter != null && !a.Converter.CanConvertFrom(typeof(string)))
					{
						a.Converter = null;
					}
				}
			}
			if (a.Type == typeof(bool) && ((bool)a.InitialValue))
			{
				throw new InvalidProgramException(string.Format("bool arguments such as {0} cannot default to true because that doesn't make sense.", a.Member.Name));
			}
			if (a.Converter == null)
			{
				a.Converter = TypeDescriptor.GetConverter(a.ElementType);
				if (a.Converter != null && !a.Converter.CanConvertFrom(typeof(string)))
				{
					a.Converter = null;
				}
			}
			// get a parse method if we must
			if (a.Converter == null)
			{
				var bt = a.ElementType;
				while (a.Parse == null && bt != null)
				{
					try
					{
						a.Parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public);
					}
					catch (AmbiguousMatchException)
					{
						a.Parse = bt.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
					}
					bt = bt.BaseType;
				}
			}
			// on the off chance this is both, favor TextReader over TextWriter
			a.IsTextReader = a.ElementType == typeof(TextReader);
			if (!a.IsTextReader)
			{
				a.IsTextWriter = a.ElementType == typeof(TextWriter);
				if (a.IsTextWriter && string.IsNullOrWhiteSpace(a.Description))
				{
					a.Description = "The output file";
					if (a.IsArray || a.IsCollection)
					{
						a.Description += "s";
						object first = null;
						var co = (System.Collections.IEnumerable)a.GetMemberValue();
						if (co != null)
						{
							foreach (object item in co)
							{
								first = item;
								break;
							}
						}
						if (first == Console.Out)
						{
							a.Description += " - defaults to <stdout>";
						}
						else if (first == Console.Error)
						{
							a.Description += " - defaults to <stderr>";
						}
						
					} else
					{
						var obj = a.GetMemberValue();
						if (obj==Console.Out)
						{
							a.Description += " - defaults to <stdout>";
						} else if(obj==Console.Error)
						{
							a.Description += " - defaults to <stderr>";
						}
					}
				}
			}
			else
			{
				a.IsTextWriter = false;
				if (a.IsTextReader && string.IsNullOrWhiteSpace(a.Description))
				{
					a.Description = "The output file";
					if (a.IsArray || a.IsCollection)
					{
						a.Description += "s";
						object first = null;
						var co = (System.Collections.IEnumerable)a.GetMemberValue();
						if (co != null)
						{
							foreach (object item in co)
							{
								first = item;
								break;
							}
						}
						if (first == Console.In)
						{
							a.Description += " - defaults to <stdin>";
						}
					} else
					{
						if (a.GetMemberValue()== Console.In)
						{
							a.Description += " - defaults to <stdin>";
						}
					}
					
				}
			}
			if(a.ElementType==typeof(DirectoryInfo))
			{
				a.IsDirectoryInfo = true;
			} else if(a.ElementType==typeof(FileInfo))
			{
				a.IsFileInfo = true;
			} else if (a.ElementType == typeof(FileSystemInfo))
			{
				a.IsFileSystemInfo = true;
			}

			if (string.IsNullOrWhiteSpace(cmdArgAttr.ItemName))
			{
				if (a.IsTextReader)
				{
					a.ItemName = "infile";
				}
				else if (a.IsTextWriter)
				{
					a.ItemName = "outfile";
				}
				else
				if (!a.IsArray && !a.IsCollection)
				{

					a.ItemName = a.Name;
				}
				else
				{
					a.ItemName = a.Name + "Item";
				}
			}
			for (int j = 0; j < result.Count; ++j)
			{
				var cmp = result[j];
				if (a.Ordinal > -1 && cmp.Ordinal == a.Ordinal)
				{
					throw new InvalidProgramException(string.Format("Duplicate ordinal specified: {0}", a.Ordinal));
				}
				// never null but we like to check
				if (0 > cmp.Ordinal && 0 > a.Ordinal && a.Name != null && cmp.Name == a.Name)
				{
					throw new InvalidProgramException(string.Format("Duplicate argument name \"{0}\"", a.Name));
				}
			}
			result.Add(a);
		}
		result.Sort((lhs, rhs) => {
			if (-1 < lhs.Ordinal)
			{
				if (rhs.Ordinal > -1)
				{
					return -1;
				}
				if (lhs.IsOptional)
				{
					if (rhs.IsOptional)
					{
						return 0;
					}
					return 1;
				}
				else
				{
					if (rhs.IsOptional)
					{
						return -1;
					}
					return 0;
				}
			}
			if (-1 < rhs.Ordinal) return 1;
			int cmp = rhs.Ordinal - lhs.Ordinal;
			if (cmp == 0)
			{
				if (lhs.IsOptional)
				{
					if (rhs.IsOptional)
					{
						return string.Compare(lhs.Name, rhs.Name);
					}
					return 1;
				}
				else
				{
					if (rhs.IsOptional)
					{
						return -1;
					}
					return string.Compare(lhs.Name, rhs.Name);
				}
			}
			return cmp;
		});
		for (int i = 0; i < result.Count - 1; ++i)
		{
			var info = result[i];
			if (info.Name.Contains("/") || info.Name.IndexOfAny(_RestrictedChars) > -1)
			{
				throw new InvalidProgramException(string.Format("The argument name for {0} is invalid.", info.Member.Name));
			}
			if (info.Ordinal > -1)
			{
				if (info.IsArray || info.IsCollection)
				{
					if (result[i + 1].Ordinal > -1)
					{
						throw new InvalidProgramException(string.Format("In order for an ordinal argument such as {0} to be an array or collection it must be the last ordinal argument in the list.", info.Member.Name));
					}
				}
				else if (info.IsOptional)
				{
					if (result[i + 1].Ordinal > -1)
					{
						throw new InvalidProgramException(string.Format("In order for an ordinal argument such as {0} to be optional it must be the last ordinal argument in the list.", info.Member.Name));
					}
				}
			}
			else
			{
				break;
			}
		}

		return result;
	}
	#endregion // _ReflectArguments
	#region _IndexOfArgInfo
	static int _IndexOfArgInfo(IList<_ArgInfo> infos,string prefix, string arg)
	{
		if (string.IsNullOrEmpty(arg) || !arg.StartsWith(prefix)) { return -1; }
		arg = arg.Substring(prefix.Length);
		if (string.IsNullOrWhiteSpace(arg)) { return -1; }
		for (int i = 0; i < infos.Count; ++i)
		{
			var info = infos[i];
			if (info.Name == arg)
			{
				return i;
			}
		}
		return -1;
	}
	#endregion // _IndexOfArgInfo
	#region _ParseArguments
	static void _ParseArguments(IList<KeyValuePair<bool,string>> args, string prefix,IList<_ArgInfo> infos)
	{
		// parse the ordinal arguments
		int argi = 0;
		int infosi = 0;
		var reqCount = 0;
		for (; reqCount < infos.Count; ++reqCount)
		{
			var info = infos[reqCount];
			if (info.Ordinal < 0 || info.IsOptional)
				break;
		}
		while (argi < args.Count && infosi < infos.Count)
		{
			var info = infos[infosi];
			if (info.Ordinal < 0)
			{
				break;
			}
			if (info.IsArray || info.IsCollection)
			{
				var arg = args[argi];
				// we can assume this is the last ordinal item in the list
				if (!arg.Key && arg.Value.StartsWith(prefix))
				{
					// expect at least one item
					if (!info.IsOptional)
					{
						throw new ArgumentException(string.Format("Missing required argument <{0}>", info.ItemName));
					}
				}
				else
				{
					info.BeginList();
					info.AddToList(arg.Value);
					++argi;
					while (argi < args.Count && (args[argi].Key || !args[argi].Value.StartsWith(prefix)))
					{
						var sarg = args[argi];
						info.AddToList(sarg.Value);
						++argi;
					}
				}
				++infosi;
			}
			else if (info.IsOptional)
			{
				var arg = args[argi];
				// we can assume this is the last ordinal item in the list
				if (arg.Key || !arg.Value.StartsWith(prefix))
				{
					info.SetMemberValue(info.InstantiateItem(arg.Value));
					++argi;
					++infosi;
				}
			}
			else
			{
				// required
				var arg = args[argi];
				if (!arg.Key && arg.Value.StartsWith(prefix))
				{
					throw new ArgumentException(string.Format("Missing required argument <{0}>", info.ItemName));
				}
				info.SetMemberValue(info.InstantiateItem(arg.Value));
				++argi;
				++infosi;
			}
		}
		if (argi < reqCount)
		{
			throw new ArgumentException(string.Format("Missing required argument <{0}>", infos[argi].ItemName));
		}
		HashSet<string> seen = new HashSet<string>(args.Count);
		while (argi < args.Count)
		{
			var arg = args[argi];
			if (arg.Key || !arg.Value.StartsWith(prefix))
			{
				throw new ArgumentException(string.Format("Unexpected value while looking for a {0} switch",prefix));
			}
			var infoIdx = _IndexOfArgInfo(infos,prefix, arg.Value);
			if (infoIdx < 0)
			{
				throw new ArgumentException(string.Format("Unrecognized switch: {0}", arg));
			}
			if (!seen.Add(arg.Value))
			{
				throw new ArgumentException(string.Format("Duplicate switch: {0}", arg));
			}
			var info = infos[infoIdx];
			++argi;
			arg = argi < args.Count ? args[argi] : new KeyValuePair<bool, string>(false,null);
			if (info.HasArgument)
			{
				if (info.IsArray || info.IsCollection)
				{
					if (!arg.Key && arg.Value.StartsWith(prefix))
					{
						throw new ArgumentException(string.Format("Expected <{0}> before {1}", info.ItemName, arg));
					}
					info.BeginList();
					info.AddToList(arg.Value);
					++argi;
					while (argi < args.Count && (args[argi].Key || !args[argi].Value.StartsWith(prefix)))
					{
						arg = args[argi];
						info.AddToList(arg.Value);
						++argi;

					}
				}
				else
				{
					info.SetMemberValue(info.InstantiateItem(arg.Value));
					++argi;
				}
			}
			else
			{
				// is a bool switch, the only type without an explicit argument
				info.SetMemberValue(true);
			}
		}
		for (int i = 0; i < infos.Count; ++i)
		{
			var info = infos[i];
			if (info.Ordinal < 0 && !info.IsOptional && !seen.Contains(prefix + info.Name))
			{
				throw new ArgumentException("The <{0}> is required but was not specified.", info.ItemName);
			}
		}
	}
	#endregion // _ParseArguments
	#region CrackCommandLine
	internal static List<KeyValuePair<bool,string>> CrackCommandLine(string commandLine, out string exename, char esc = '\\')
	{
		exename = null;
		var result = new List<KeyValuePair<bool,string>>();
		var i = 0;
		var inQuote = false;
		var sb = new StringBuilder();
		while (i < commandLine.Length)
		{
			if (!inQuote)
			{
				if (sb.Length == 0)
				{
					if (commandLine[i] == '"')
					{
						inQuote = true;
						++i;
						continue;
					}
				}
				var ws = false;
				while (char.IsWhiteSpace(commandLine[i]))
				{
					ws = true;
					++i;
				}
				if (ws)
				{
					if (sb.Length > 0)
					{
						if (exename == null)
						{
							exename = sb.ToString();
						}
						else
						{
							result.Add(new KeyValuePair<bool, string>(false, sb.ToString()));
						}
						sb.Clear();
					}
				}
				else
				{
					sb.Append(commandLine[i]);
					++i;
				}

			}
			else
			{
				if (i < commandLine.Length - 1 && commandLine[i] == esc && commandLine[i + 1] == '\"')
				{
					sb.Append('\"');
					i += 2;
				}
				else if (commandLine[i] == '\"')
				{
					if (exename == null)
					{
						exename = sb.ToString();
					}
					else
					{
						result.Add(new KeyValuePair<bool, string>(true, sb.ToString()));
					}
					sb.Clear();
					inQuote = false;
					++i;
					continue;
				}
				sb.Append(commandLine[i]);
				++i;
			}
		}
		if (sb.Length > 0)
		{
			if (exename == null)
			{
				exename = sb.ToString();
			}
			else
			{
				result.Add(new KeyValuePair<bool, string>(inQuote, sb.ToString()));
			}
		}
		return result;
	}
	#endregion // CrackCommandLine

	#region WordWrap
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
			width = Console.WindowWidth;
		}
		if (indent < 0) throw new ArgumentOutOfRangeException(nameof(indent));
		if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
		if (width > 0 && width < indent)
		{
			throw new ArgumentOutOfRangeException(nameof(width));
		}
		string[] words = text.Split(new string[] { " " },
			StringSplitOptions.None);

		StringBuilder result = new StringBuilder();
		double actualWidth = startOffset;
		for (int i = 0; i < words.Length; i++)
		{
			var word = words[i];
			if (i > 0)
			{
				if (actualWidth + word.Length >= width)
				{
					result.Append(Environment.NewLine);
					if (indent > 0)
					{
						result.Append(new string(' ', indent));
					}
					actualWidth = indent;
				}
				else
				{
					result.Append(' ');
					++actualWidth;
				}
			}
			result.Append(word);
			actualWidth += word.Length;
		}
		return result.ToString();
	}

	#endregion // WordWrap
	#region PrintUsage
	/// <summary>
	/// Prints the usage screen
	/// </summary>
	public static void PrintUsage()
	{
		var prefix = "--";
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			prefix = "/";
		}
		_PrintUsage(Console.Error,prefix, _ReflectArguments(typeof(Program)));
	}
	private static void _PrintUsage(TextWriter w, string prefix,IList<_ArgInfo> arguments)
	{
		var sb = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(Info.Name) && null != Info.Version)
		{

			sb.AppendFormat("{0} v{1}.{2}", Info.Name, Info.Version.Major, Info.Version.Minor);
			if (Info.Version.Build != 0 || Info.Version.Revision != 0)
			{
				sb.AppendFormat(" (build {0} rev. {1})", Info.Version.Build, Info.Version.Revision);
			}
			if (!string.IsNullOrWhiteSpace(Info.Copyright))
			{
				sb.Append(' ');
				sb.Append(Info.Copyright.Trim());
			}
			sb.AppendLine();
			if (!string.IsNullOrWhiteSpace(Info.Description))
			{
				sb.AppendLine();
				sb.Append("   ");
				sb.Append(Info.Description.Trim());
				sb.AppendLine();
			}
		}
		w.WriteLine(WordWrap(sb.ToString()));
		sb.Clear();
		sb.Append("Usage: ");
		sb.Append(Path.GetFileNameWithoutExtension(Info.Filename));
		sb.Append(' ');
		int nameLen = 4;
		for (int i = 0; i < arguments.Count; ++i)
		{
			if (i > 0)
			{
				sb.Append(' ');
			}
			var info = arguments[i];
			if (info.ItemName.Length > nameLen)
			{
				nameLen = info.ItemName.Length;
			}
			if (info.Ordinal < 0)
			{
				if (info.IsOptional)
				{
					sb.Append('[');
				}
				sb.Append(prefix);
				sb.Append(info.Name);
			}
			else
			{
				if (info.IsOptional)
				{
					sb.Append('[');
				}
			}
			if (info.HasArgument)
			{
				if (info.Ordinal < 0)
				{
					sb.Append(' ');
				}
				if (info.IsCollection || info.IsArray)
				{
					sb.Append("{<");
					sb.Append(info.ItemName);
					sb.Append("1>");
					if (info.IsOptional)
					{
						// the whole thing is already surrounded by brackes so we don't need any
						sb.Append(" <");
						sb.Append(info.ItemName);
						sb.Append("N>");
					}
					else
					{
						sb.Append(" [<");
						sb.Append(info.ItemName);
						sb.Append("N>]");
					}
					sb.Append('}');
				}
				else
				{
					sb.Append("<");
					sb.Append(info.ItemName);
					sb.Append(">");
				}
			}
			if (info.IsOptional)
			{
				sb.Append(']');
			}
		}
		w.WriteLine(WordWrap(sb.ToString(), Console.WindowWidth, 4));
		w.WriteLine();
		sb.Clear();
		for (int i = 0; i < arguments.Count; ++i)
		{
			sb.Clear();
			var info = arguments[i];
			sb.Append("  <");
			sb.Append(info.ItemName);
			sb.Append('>');
			sb.Append(new string(' ', (nameLen - info.ItemName.Length) + 1));
			if (!string.IsNullOrEmpty(info.Description))
			{
				sb.Append(info.Description.Trim());
			}
			else if (info.IsTextReader)
			{
				sb.Append("The input file");
				if (info.IsArray || info.IsCollection)
				{
					sb.Append('s');
				}
			}
			else if (info.IsTextWriter)
			{
				sb.Append("The output file");
				if (info.IsArray || info.IsCollection)
				{
					sb.Append('s');
				}
			}
			w.WriteLine(WordWrap(sb.ToString(), Console.WindowWidth, 4));
		}
		sb.Clear();
		if (0 > _IndexOfArgInfo(arguments, prefix,prefix+"help"))
		{
			if (nameLen == 0)
			{
				sb.Append("  ");
				sb.Append(prefix);
				sb.Append("help");
				sb.Append(new string(' ', (nameLen - 4) + 1));
				sb.AppendLine("Displays this screen and exits");
			}
			else
			{
				sb.AppendLine("- or -");
				sb.Append("  ");
				sb.Append(prefix);
				sb.Append("help");
				sb.Append(new string(' ', (nameLen - 4) + 1));
				sb.AppendLine("Displays this screen and exits");
			}
		}
		w.WriteLine(WordWrap(sb.ToString(), Console.WindowWidth, 4));
	}
	#endregion // _PrintUsage
	#region WriteProgress/WriteProgressBar
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
		_ProgressBuffer.Append('[');
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
	#endregion // WriteProgress/WriteProgressBar
	static int Main(string[] args)
	{
		var prefix = "--";
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			prefix = "/";
		}
		var cl = Environment.CommandLine;
		string exename;
		var clargs = CrackCommandLine(cl, out exename);
		var sb = new StringBuilder();
		sb.Append(exename); 
		if(clargs.Count>args.Length)
		{
			for(int i = 0;i<clargs.Count-args.Length;++i)
			{
				sb.Append(' ');
				var clarg = clargs[i];
				if(clarg.Key)
				{
					sb.Append('"');
					sb.Append(clarg.Value.Replace("\"", "\"\""));
					sb.Append('"');
				} else
				{
					sb.Append(clarg.Value);
				}
			}
		}
		Info = new ProgramInfo(_GetCodeBase(), Path.GetFileName(_GetCodeBase()), _GetName(), _GetDescription(), _GetCopyright(), _GetVersion(),sb.ToString());
		if (clargs.Count >= args.Length)
		{
			clargs = clargs.GetRange(clargs.Count - args.Length, args.Length);
		} else
		{
			clargs.Clear();
			for(int i = 0;i<args.Length;i++)
			{
				clargs.Add(new KeyValuePair<bool, string>(false, args[i]));
			}
		}
		
#if !DEBUG
		var parsedArgs = false;
#endif // !DEBUG
		IList<_ArgInfo> argInfos = null;
		try
		{
			argInfos = _ReflectArguments(typeof(Program));
			if (args.Length == 1 && (args[0] == prefix+"help" || args[0]==prefix+"?"))
			{
				_PrintUsage(Console.Out, prefix,argInfos);
				return 0;
			}
			_ParseArguments(clargs, prefix,argInfos);
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
				if (argInfos != null)
				{
					_PrintUsage(Console.Error,prefix, argInfos);
				}
			}
			Console.Error.WriteLine("Error: {0}", ex.Message);
			return ex.HResult;
		}
#endif
		finally
		{
			if (argInfos != null)
			{
				for (int i = 0; i < argInfos.Count; ++i)
				{
					argInfos[i].Destroy();
				}
			}
		}
		return _GetUserExitCode();
	}
}

