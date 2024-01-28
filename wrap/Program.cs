﻿using System;
using System.Collections.Generic;
using System.IO;
internal partial class Program
{
	// you don't need this field, but if you have it
	// you can control the exit code. The warning
	// is disabled because the compiler doesn't know
	// that the field is being reflected upon.
	#pragma warning disable CS0414
	private static int ExitCode = 0;
	#pragma warning restore
	[CmdArg(Ordinal = 0)] // the description and itemname are filled
	static List<TextReader> Inputs = new List<TextReader>() { Console.In };
	[CmdArg(Name = "output")] // the description and itemname are filled
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
