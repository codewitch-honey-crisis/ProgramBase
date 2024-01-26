# Program.Base.cs

A drop in file to add core CLI functionality to your projects.

```cs
static partial class Program
{
	/*
	 * foo.txt bar.txt /output foobar.cs /id 5860F36D-6207-47F9-9909-62F2B403BBA8 /ips 192.168.0.104 192.168.0.200 /ifstale /count 5 /enum static /indices 5 6 7 8
	 */
	[CmdArg("<default>",true,"The input files",ElementName ="infile")]
	public static string[] Input = null;
	[CmdArg("output", false, "The output file",ElementName ="outfile")]
	public static string Output = null;
	[CmdArg("id", false, "The guid id",ElementName ="guid")]
	public static Guid Id = Guid.Empty;
	[CmdArg("ips", false, "The ip addesses",ElementName ="address")]
	public static List<IPAddress> Ips = null;
	[CmdArg("ifstale", false, "Only regenerate if input has changed")]
	public static bool IfStale = false;
	[CmdArg("count", false, "The count",ElementName ="number")]
	public static int Count = 0;
	[CmdArg("enum", false, "The binding flags",ElementName ="flag")]
	public static List<BindingFlags> Enum = null;
	[CmdArg("indices", false, "The indices",ElementName ="index")]
	public static List<int> Indices = null;
	static int Run()
	{
		
		return 0;
	}
}
```
That will parse arguments into those fields. It accepts strings, collections, arrays and bool switches, or anything TypeConverter can convert, or that has a static Parse() method that takes a string.

There is also a `WordWrap()` function that will wrap text, `PrintUsage()` will print the usage screen, and `IsStale()` will check if a file doesn't exist or another file is newer. There is `WriteProgress()` which writes an indeterminate progress indicator and `WriteProgressBar()` which writes a progress bar.

Here is a complete CLI app (using Program.Base.cs) that will word wrap text to the specified width
```cs
using System;
using System.IO;
internal partial class Program
{
	[CmdArg(Name = "<default>", Description = "The input text file to wrap. Defaults to <stdin>", ElementName = "infile")]
	static TextReader Input = Console.In;
	[CmdArg(Name = "output", Description = "The ouput text file to create. Defaults to <stdout>", ElementName = "outfile")]
	static TextWriter Output = Console.Out;
	[CmdArg(Name = "width", Description = "The width to wrap. Defaults based on console window size", ElementName = "columns")]
	static int Width = (int)Math.Floor((double)Console.WindowWidth / 1.5);
	[CmdArg(Name = "ifstale", Description = "Skip if the input file is older than the output file")]
	static bool IfStale = false;
	static void Run()
	{
		if (!IfStale || IsStale(Input, Output))
		{
			Output.WriteLine(WordWrap(Input.ReadToEnd(), Width));
		} else
		{
			Console.Error.WriteLine("Skipped execution because \"{0}\" did not change", GetFilename(Input));
		}
	}
}
```
This application presents the following using screen:
```
Usage: wrap [ <infile> ] [ /output <outfile> ] [ /width
    <columns> ] [ /ifstale ]

wrap v1.0.0.0
Word wraps input

<default>  The input text file to wrap. Defaults to <stdin>
/output    The ouput text file to create. Defaults to
    <stdout>
/width     The width to wrap. Defaults based on console
    window size
/ifstale   Skip if the input file is older than the output
    file
/?         Displays this screen and exits
```

That's all built for you from the above. Note the automatic file management.