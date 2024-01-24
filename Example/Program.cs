using System;
using System.Net;
using System.Reflection;
/* Try this command line:
	 * foo.txt bar.txt /output foobar.cs /id 5860F36D-6207-47F9-9909-62F2B403BBA8 /ips 192.168.0.104 192.168.0.200 /ifstale /count 5 /enum static /indices 5 6 7 8
*/
static partial class Program
{
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
		//Console.Write("Progress test: ");
		//for (int i = 0; i < 10; ++i)
		//{
		//	WriteProgress(i, i > 0, Console.Out);
		//	Thread.Sleep(100);
		//}
		//Console.WriteLine();
		//Console.Write("Progress bar test: ");
		//for (int i = 0; i <= 100; ++i)
		//{
		//	WriteProgressBar(i, i > 0, Console.Out);
		//	Thread.Sleep(10);
		//}
		//Console.WriteLine();
		Console.WriteLine(WordWrap("fringilla phasellus faucibus scelerisque eleifend donec pretium vulputate sapien nec sagittis aliquam malesuada bibendum arcu vitae elementum curabitur vitae nunc sed velit dignissim sodales ut eu sem integer vitae justo eget magna fermentum iaculis eu non diam phasellus vestibulum lorem sed risus ultricies tristique nulla aliquet enim tortor at auctor",40,0));
		
	}
}