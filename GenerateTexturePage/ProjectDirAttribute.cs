namespace GenerateTexturePage;

[System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
sealed class ProjectCompileDirectoryAttribute : System.Attribute
{
    public string ProjectCompileDirectory { get; }
    public ProjectCompileDirectoryAttribute(string configurationLocation)
    {
        this.ProjectCompileDirectory = configurationLocation;
    }
}
