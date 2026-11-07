namespace Qotoba.Workbench.Service;

public sealed class WorkbenchOptions
{
    public const string Section = "Workbench";

    /// <summary>Folder for the database and the dev token. Default: %USERPROFILE%\.qotoba-workbench.</summary>
    public string? DataDir { get; set; }

    /// <summary>Where the local Host listens. The Service never touches disk itself.</summary>
    public string HostUrl { get; set; } = "http://localhost:5291";

    /// <summary>Name of this machine as the Workbench shows it. Default: the computer name, lower case.</summary>
    public string? MachineId { get; set; }

    /// <summary>Folder holding index.html, app.js and the CSS. Default: ../Qotoba.Workbench.Ui in development, wwwroot when published.</summary>
    public string? UiDir { get; set; }

    public string Owner { get; set; } = "Owner";
    public string Programme { get; set; } = "Qotoba";
    public string TimeZone { get; set; } = "Africa/Lagos";
}
