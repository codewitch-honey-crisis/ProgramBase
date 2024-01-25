internal partial class Program
{
	[CmdArg(Name = "<default>", Description = "The input text file to wrap. Defaults to <stdin>", ElementName = "infile")]
	static TextReader Input = Console.In;
	[CmdArg(Name = "output", Description = "The ouput text file to create. Defaults to <stdout>", ElementName = "outfile")]
	static TextWriter Output = Console.Out;
	[CmdArg(Name = "width", Description = "The width to wrap. Defaults based on console window size", ElementName = "columns")]
	static int Width = (int)Math.Floor((double)Console.WindowWidth / 1.5);
	static void Run()
	{
		Output.WriteLine(WordWrap(Input.ReadToEnd(),Width));
	}
}
