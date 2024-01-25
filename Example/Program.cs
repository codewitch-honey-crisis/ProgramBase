using System;
using System.Net;
using System.Reflection;
/* Try this command line:
	 * foo.txt bar.txt /output foobar.cs /id 5860F36D-6207-47F9-9909-62F2B403BBA8 /ips 192.168.0.104 192.168.0.200 /ifstale /count 5 /enum static /indices 5 6 7 8
*/
static partial class Program
{
	[CmdArg("<default>", true, "The input files", ElementName = "infile")]
	public static TextReader[] Input = null;// new TextReader[] { Console.In };
	[CmdArg("output", false, "The output file", ElementName = "outfile")]
	public static TextWriter Output = Console.Out;
	[CmdArg("id", false, "The guid id", ElementName = "guid")]
	public static Guid Id = Guid.Empty;
	[CmdArg("ips", false, "The ip addesses", ElementName = "address")]
	public static List<IPAddress> Ips = new List<IPAddress>() { IPAddress.Any };
	[CmdArg("ifstale", false, "Only regenerate if input has changed")]
	public static bool IfStale = false;
	[CmdArg("width", false, "The width to wrap to", ElementName = "chars")]
	public static int Width = Console.WindowWidth/2;
	[CmdArg("enum", false, "The binding flags", ElementName = "flag")]
	public static List<BindingFlags> Enum = null;
	[CmdArg("indices", false, "The indices", ElementName = "index")]
	public static List<int> Indices = null;
	static void Run()
	{
		Console.Write("Progress test: ");
		for (int i = 0; i < 10; ++i)
		{
			WriteProgress(i, i > 0, Console.Out);
			Thread.Sleep(100);
		}
		Console.WriteLine();
		Console.Write("Progress bar test: ");
		for (int i = 0; i <= 100; ++i)
		{
			WriteProgressBar(i, i > 0, Console.Out);
			Thread.Sleep(10);
		}
		Console.WriteLine();
		foreach (var reader in Input) {
			Output.Write(WordWrap(reader.ReadToEnd(), Width, 0));
		}
	}
}