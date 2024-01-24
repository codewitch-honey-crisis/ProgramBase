using System;
using System.Net;
using System.Reflection;

public static partial class Program
{
	/* Try this command line:
	 * foo.txt bar.txt /output foobar.cs /id 5860F36D-6207-47F9-9909-62F2B403BBA8 /ips 192.168.0.104 192.168.0.200 /ifstale /count 5 /enum static /indices 5 6 7 8
	 */
	[CmdArg("<default>", true, "The input files", ElementName = "infile")]
	public static string[] Input = null;
	[CmdArg("output", false, "The output file", ElementName = "outfile")]
	public static string Output = null;
	[CmdArg("id", false, "The guid id", ElementName = "guid")]
	public static Guid Id = Guid.Empty;
	[CmdArg("ips", false, "The ip addesses", ElementName = "address")]
	public static List<IPAddress> Ips = null;
	[CmdArg("ifstale", false, "Only regenerate if input has changed")]
	public static bool IfStale = false;
	[CmdArg("count", false, "The count", ElementName = "number")]
	public static int Count = 0;
	[CmdArg("enum", false, "The binding flags", ElementName = "flag")]
	public static List<BindingFlags> Enum = null;
	[CmdArg("indices", false, "The indices", ElementName = "index")]
	public static List<int> Indices = null;
	static void Run()
	{
		
	}
}