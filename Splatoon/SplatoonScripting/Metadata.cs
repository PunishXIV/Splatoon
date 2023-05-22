#nullable enable
namespace Splatoon.SplatoonScripting;

public class Metadata
{
    /// <summary>
    /// Optional single digit version of a script, will be displayed in the list of scripts.
    /// </summary>
    public uint Version { get; }

    /// <summary>
    /// Optional author of a script, will be displayed in the list of scripts.
    /// </summary>
    public string? Author { get; }

    /// <summary>
    /// Optional description of a script, will be displayed in the list of scripts.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Optional website of script's origin.
    /// </summary>
    public string? Website { get; }

    /// <summary>
    /// URL that contains information for auto-update in CSV format (FullName,Version,URL). If there is a higher version available, script will be automatically updated if it comes from trusted URL, otherwise user will be prompted to install an update.
    /// </summary>
    [Obsolete("This function has not been implemented yet")]
    public string UpdateURL { get; set; } = "https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/update.csv";

    /// <summary>
    /// Blacklist will be checked before script is loaded; if script's name and it's version are present in it, script will be automatically disabled. An user can still choose to manually reenable it, however. 
    /// </summary>
    [Obsolete("This function has not been implemented yet")] 
    public string BlacklistURL { get; set; } = "https://github.com/NightmareXIV/Splatoon/raw/master/SplatoonScripts/blacklist.csv";

    public Metadata(uint version, string? author, string? description, string? website)
    {
        Version = version;
        Author = author;
        Description = description;
        Website = website;
    }

    public Metadata(uint version, string? author, string? description)
    {
        Version = version;
        Author = author;
        Description = description;
    }

    public Metadata(uint version, string? author)
    {
        Version = version;
        Author = author;
    }

    public Metadata(uint version)
    {
        Version = version;
    }

    public Metadata()
    {
    }
}
