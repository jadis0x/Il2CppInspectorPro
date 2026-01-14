using Spectre.Console;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal sealed class InteractiveCommand(PortProvider portProvider) : BaseCommand<InteractiveCommand.Settings>(portProvider)
{
    public sealed class Settings : CommandSettings;

    protected override async Task<int> ExecuteAsync(CliClient client, Settings settings)
    {
        await Task.Delay(1000);
        await AnsiConsole.AskAsync<string>("meow?");
        return 0;
    }
}