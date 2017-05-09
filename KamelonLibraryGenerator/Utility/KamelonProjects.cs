using EnvDTE;

namespace LDC.Generator.Kamelon.VSExtension.Utility
{
    /// <summary>
    /// Projeye ait katmanlar
    /// </summary>
    public static class KamelonProjects
    {
        public static Project EntityProject => SdkHelperTool.GetProjectByName("LDC.Model.Entity");

        public static Project DtoProject => SdkHelperTool.GetProjectByName("LDC.Model.Dto");

        public static Project MapProject => SdkHelperTool.GetProjectByName("LDC.Model.Map");

        public static Project DomainExtensionProject => SdkHelperTool.GetProjectByName("LDC.Assemblies.DomainExtentions");

        public static Project RepositoryProject => SdkHelperTool.GetProjectByName("LDC.Assemblies.Repository");

        public static Project ServiceProject => SdkHelperTool.GetProjectByName("LDC.Assemblies.Service");
    }
}