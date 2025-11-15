namespace Application.Common.Settings;

public class ApplicationSettings
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public FileStorage FileStorage { get; set; } = new();
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public class FileStorage
{
    public string LocalPath { get; set; } = "wwwroot/uploads";
}