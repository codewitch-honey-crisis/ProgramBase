# Program.Base.cs

A drop in file to add core CLI functionality to your projects.

```cs
/* Try this command line:
	 * foo.txt bar.txt /output foobar.cs /id 5860F36D-6207-47F9-9909-62F2B403BBA8 /ips 192.168.0.104 192.168.0.200 /ifstale /count 5 /enum static /indices 5 6 7 8
*/
static partial class Program
{
	[CmdArg(Ordinal = 0,Required =true,Description ="The input files")]
	public static TextReader[] Inputs = null;
	[CmdArg("output",Description = "The output file. Defaults to <stdout>")]
	public static TextWriter Output = Console.Out;
	[CmdArg("id", Description = "The guid id", ItemName = "guid")]
	public static Guid Id = Guid.Empty;
	[CmdArg("ips", Description = "The ip addesses", ItemName = "address")]
	public static List<IPAddress> Ips = new List<IPAddress>() { IPAddress.Any };
	[CmdArg("ifstale", Description = "Only regenerate if input has changed")]
	public static bool IfStale = false;
	[CmdArg("width", Description = "The width to wrap to", ItemName = "chars")]
	public static int Width = Console.WindowWidth/2;
	[CmdArg("enum", Description = "The binding flags", ItemName = "flag")]
	public static List<BindingFlags> Enum = null;
	[CmdArg("indices", Description = "The indices", ItemName = "index")]
	public static List<int> Indices = null;
	static void Run()
	{
		Console.Error.Write("Progress test: ");
		for (int i = 0; i < 10; ++i)
		{
			WriteProgress(i, i > 0, Console.Error);
			Thread.Sleep(100);
		}
		Console.Error.WriteLine();
		Console.Error.Write("Progress bar test: ");
		for (int i = 0; i <= 100; ++i)
		{
			WriteProgressBar(i, i > 0, Console.Error);
			Thread.Sleep(10);
		}
		Console.Error.WriteLine();
		// use our Inputs and Output
		// will be closed on exit
		var first = true;
		foreach (var input in Inputs) {
			if (!first)
			{
				Output.WriteLine();
			}
			else { first = false; }
			Output.Write(WordWrap(input.ReadToEnd(), Width, 0));
		}
	}
}
```
That will parse arguments into those fields. It accepts strings, collections, arrays and bool switches, or anything TypeConverter can convert, or that has a static Parse() method that takes a string.

There is also a `WordWrap()` function that will wrap text, `PrintUsage()` will print the usage screen, and `IsStale()` will check if a file doesn't exist or another file is newer. There is `WriteProgress()` which writes an indeterminate progress indicator and `WriteProgressBar()` which writes a progress bar.

Here is a complete CLI app (using Program.Base.cs) that will word wrap text to the specified width
```cs
using System;
using System.Collections.Generic;
using System.IO;
internal partial class Program
{
	[CmdArg(Ordinal =0, Description = "The input text file to wrap. Defaults to <stdin>")]
	static List<TextReader> Inputs = new List<TextReader>() { Console.In };
	[CmdArg(Name = "output", Description = "The ouput text file to create. Defaults to <stdout>")]
	static TextWriter Output = Console.Out;
	[CmdArg(Name = "width", Description = "The width to wrap. Defaults based on console window size", ItemName = "columns")]
	static int Width = (int)Math.Floor((double)Console.WindowWidth / 1.5);
	[CmdArg(Name = "ifstale", Description = "Skip if the input file is older than the output file")]
	static bool IfStale = false;
	static void Run()
	{
		if (!IfStale || IsStale(Inputs, Output))
		{
			foreach (var input in Inputs)
			{
				// do this because stdin requires it
				string line;
				while((line = input.ReadLine()) != null)
				{
					Output.WriteLine(WordWrap(line, Width));
				}
				
			}
		} else
		{
			Console.Error.WriteLine("Skipped execution because the inputs did not change");
		}
	}
}
```
This application presents the following using screen (on Windows):
```
wrap v1.0

   word wraps input

Usage: wrap [{<infile1> <infileN>}] [/ifstale] [/output <outfile>] [/width <columns>]

  <infile>  The input text file to wrap. Defaults to <stdin>
  <ifstale> Skip if the input file is older than the output file
  <outfile> The ouput text file to create. Defaults to <stdout>
  <columns> The width to wrap. Defaults based on console window size
- or -
  /?        Displays this screen and exits
```
and like this on other operating systems
```
wrap v1.0

   word wraps input

Usage: wrap [{<infile1> <infileN>}] [--ifstale] [--output <outfile>] [--width <columns>]

  <infile>  The input text file to wrap. Defaults to <stdin>
  <ifstale> Skip if the input file is older than the output file
  <outfile> The ouput text file to create. Defaults to <stdout>
  <columns> The width to wrap. Defaults based on console window size
- or -
  --help    Displays this screen and exits

  ```
That's all built for you from the above. Note the automatic file management.