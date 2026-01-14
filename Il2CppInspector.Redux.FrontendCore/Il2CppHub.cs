using Microsoft.AspNetCore.SignalR;

namespace Il2CppInspector.Redux.FrontendCore;

public class Il2CppHub : Hub
{
    private const string ContextKey = "context";

    private UiContext State
    {
        get
        {
            if (!Context.Items.TryGetValue(ContextKey, out var context)
                || context is not UiContext ctx)
            {
                Context.Items[ContextKey] = ctx = new UiContext();
            }

            return ctx;
        }
    }

    private UiClient Client => new(Clients.Caller);

    public async Task OnUiLaunched()
    {
        await State.InitializeAsync(Client);
    }

    public async Task SubmitInputFiles(List<string> inputFiles)
    {
        await State.LoadInputFilesAsync(Client, inputFiles);
    }

    public async Task QueueExport(string exportTypeId, string outputDirectory, Dictionary<string, string> settings)
    {
        await State.QueueExportAsync(Client, exportTypeId, outputDirectory, settings);
    }

    public async Task StartExport()
    {
        await State.StartExportAsync(Client);
    }

    public async Task<IEnumerable<string>> GetPotentialUnityVersions()
    {
        return await State.GetPotentialUnityVersionsAsync();
    }

    public async Task ExportIl2CppFiles(string outputDirectory)
    {
        await State.ExportIl2CppFilesAsync(Client, outputDirectory);
    }
    public async Task<string> GetInspectorVersion()
    {
        return await UiContext.GetInspectorVersionAsync();
    }

    public async Task SetSettings(InspectorSettings settings)
    {
        await State.SetSettingsAsync(Client, settings);
    }
}