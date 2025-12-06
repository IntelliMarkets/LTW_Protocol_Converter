using System.CommandLine;
using LittleTreeConverter.Cli;

var rootCommand = CommandBuilder.Build();
return await rootCommand.InvokeAsync(args);
