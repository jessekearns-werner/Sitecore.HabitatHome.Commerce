using System.Text.RegularExpressions;

/*===============================================
================= HELPER METHODS ================
===============================================*/

public class Configuration
{
    private MSBuildToolVersion _msBuildToolVersion;    

    public string WebsiteRoot {get;set;}
    public string InstanceUrl {get;set;}
    public string SolutionName {get;set;}
    public string ProjectFolder {get;set;}
    public string BuildConfiguration {get;set;}
    public bool RunCleanBuilds {get;set;}
    public string MessageStatisticsApiKey {get;set;}
    public string BuildToolVersions 
    {
        set 
        {
            if(!Enum.TryParse(value, out this._msBuildToolVersion))
            {
                this._msBuildToolVersion = MSBuildToolVersion.Default;
            }
        }
    }

    public string SourceFolder => $"{ProjectFolder}\\src";
    public string FoundationSrcFolder => $"{SourceFolder}\\Foundation";
    public string FeatureSrcFolder => $"{SourceFolder}\\Feature";
    public string ProjectSrcFolder => $"{SourceFolder}\\Project";

    public string SolutionFile => $"{ProjectFolder}\\{SolutionName}";
    public MSBuildToolVersion MSBuildToolVersion => this._msBuildToolVersion;
    public string BuildTargets => this.RunCleanBuilds ? "Clean;Build" : "Build";
    public string SitecoreLibrariesPath => $"{WebsiteRoot}\\bin";
    public string SitecoreLibModuleCommerce => $"{ProjectFolder}\\lib\\Modules\\Commerce";
}

public void PublishProjects(string rootFolder, string websiteRoot)
{
    var projects = GetFiles($"{rootFolder}\\**\\website\\*.csproj");

    foreach (var project in projects)
    {
        MSBuild(project, cfg => InitializeMSBuildSettings(cfg)
                                   .WithTarget(configuration.BuildTargets)
                                   .WithProperty("DeployOnBuild", "true")
                                   .WithProperty("DeployDefaultTarget", "WebPublish")
                                   .WithProperty("WebPublishMethod", "FileSystem")
                                   .WithProperty("DeleteExistingFiles", "false")
                                   .WithProperty("publishUrl", websiteRoot)
                                   .WithProperty("BuildProjectReferences", "false"));
    }
}

public FilePathCollection GetTransformFiles(string rootFolder)
{
    Func<IFileSystemInfo, bool> exclude_obj_bin_folder = fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains("/obj/") && !fileSystemInfo.Path.FullPath.Contains("/bin/");

    var xdtFiles = GetFiles($"{rootFolder}/**/*.xdt", exclude_obj_bin_folder);

    return xdtFiles;
}

public void Transform(string rootFolder) {
    var xdtFiles = GetTransformFiles(rootFolder);

    foreach (var file in xdtFiles)
    {
        Information($"Applying configuration transform:{file.FullPath}");
        var fileToTransform = Regex.Replace(file.FullPath, ".+website/(.+)/*.xdt", "$1");
        var sourceTransform = $"{configuration.WebsiteRoot}\\{fileToTransform}";
        if (FileExists(sourceTransform))
        {
            XdtTransformConfig(sourceTransform			            // Source File
                            , file.FullPath			                // Tranforms file (*.xdt)
                            , sourceTransform);		                // Target File
        }
        
    }
}

public void RebuildIndex(string indexName)
{
    cakeConsole.WriteLine($"Rebuild {indexName}");
    var url = $"{configuration.InstanceUrl}utilities/indexrebuild.aspx?index={indexName}";
    string responseBody = HttpGet(url);
}

public MSBuildSettings InitializeMSBuildSettings(MSBuildSettings settings)
{
    settings.SetConfiguration(configuration.BuildConfiguration)
            .SetVerbosity(Verbosity.Minimal)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetPlatformTarget(PlatformTarget.MSIL)
            .UseToolVersion(configuration.MSBuildToolVersion)
            .WithRestore();
    return settings;
}

public void CreateFolderIfNotExist(string folderPath)
{
    if (!DirectoryExists(folderPath))
    {
        CreateDirectory(folderPath);
    }
}

public void WriteError(string errorMessage)
{
    cakeConsole.ForegroundColor = ConsoleColor.Red;
    cakeConsole.WriteError(errorMessage);
    cakeConsole.ResetColor();
}