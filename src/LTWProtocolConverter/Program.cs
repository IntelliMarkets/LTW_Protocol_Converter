using System.CommandLine;
using LTWProtocolConverter.Cli;

var rootCommand = CommandBuilder.Build();
return await rootCommand.InvokeAsync(args);
