using System;
using System.Collections.Generic;
using System.IO;
internal partial class Program
{
	#pragma warning disable CS0414
	private static int ExitCode = 0;
	#pragma warning restore
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
			ExitCode = 1;
		}
	}
}
