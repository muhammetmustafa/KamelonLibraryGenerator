using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LDC.Generator.Kamelon.Results;
using LDC.Generator.Kamelon.Utility;
using LDC.Generator.Kamelon.VSExtension.Utility;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace LDC.Generator.Kamelon.VSExtension
{
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidKamelonLibraryGeneratorPkgString)]
    public sealed class KamelonLibraryGeneratorPackage : Package
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly CodeGenerator codeGenerator = new CodeGenerator();
        
        private LDCFileInfo _selectedFile;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public KamelonLibraryGeneratorPackage()
        {

        }

        protected override void Initialize()
        {
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidKamelonLibraryGeneratorCmdSet, (int)PackageCommandIdList.LDC_GENERATE_KAMELON_GENERATE_LIBRARY_COMMAND);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                var dte = SdkHelperTool.GetActiveIde();
                dte?.Solution.SolutionBuild.Build(true);
                
                GenerateLibrary();

                showMessage("Katmanlar oluşturuldu.Namespaceleri kontrol etmeyi unutma ! " +
                "Kamelon Library Generator'u tercih ettiğiniz için teşekkür eder. İyi kodlamalar dileriz :)");
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Exception: " + exception.Message);
                Debug.WriteLine("Exception: " + exception.StackTrace);
                showMessage("Beklenmeyen hata oluştu! " + exception.Message);
            }
        }

        private void showMessage(string message)
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(0, ref clsid, "Kamelon Library Generator", message, string.Empty, 0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out result));
        }

        private void GenerateLibrary()
        {
            var project = SdkHelperTool.GetActiveProject();
            if (project == null)
            {
                throw new Exception("Aktif proje alma sırasında hata oluştu!");
            }

            var assemblyPath = SdkHelperTool.GetAssemblyPath(project);
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new Exception("Assembly yolu boş!");
            }

            _selectedFile = SdkHelperTool.GetSelectedFile(SdkHelperTool.GetActiveIde());
            
            Debug.WriteLine($"SolutionPath : '{_selectedFile.SolutionPath}'");
            Debug.WriteLine($"ProjectName : '{_selectedFile.ProjectName}'");
            Debug.WriteLine($"ProjectPath : '{_selectedFile.ProjectPath}'");
            Debug.WriteLine($"FolderName : '{_selectedFile.FolderName}'");
            Debug.WriteLine($"FolderPath : '{_selectedFile.FolderPath}'");
            Debug.WriteLine($"FileName : '{_selectedFile.FileName}'");
            Debug.WriteLine($"Extension : '{_selectedFile.Extension}'");
            Debug.WriteLine($"File : '{_selectedFile.File}'");
            Debug.WriteLine($"FilePath : '{_selectedFile.FilePath}'");
            Debug.WriteLine($"FullPath : '{_selectedFile.FullPath}'");
            Debug.WriteLine($"Namespace : '{_selectedFile.Namespace}'");

            var assemblyName = _selectedFile.File.Split('.').FirstOrDefault();

            Debug.WriteLine("AssemblyName of selected file: " + assemblyName);
            
            foreach (var type in Assembly.LoadFrom(assemblyPath).GetTypes())
            {
                Debug.WriteLine("TypeName: " + type.Name);

                if (type.Name != assemblyName)
                {
                    continue;
                }

                var instance = Activator.CreateInstance(type);

                generateDTO(type, instance);
                generateExtension(type, instance);
                generateMap(type, instance);
                generateRepository(type, instance);
                generateService(type, instance);
            }
        }

        private void generateDTO(Type type, object instance)
        {
            var dtoFields = ReflectionUtil.GetFields(instance, FieldStatus.Dto);
            var code = codeGenerator.GetDtoCode(type.Name, dtoFields).Replace("{test}", _selectedFile.Namespace);

            var codeFile = LDCFileInfo.clone(_selectedFile);
            codeFile.ProjectName = "LDC.Model.Dto";
            codeFile.FileName += "Dto";

            codeGenerator.CreateCodeFile(codeFile, code);
            addToProject(codeFile);
        }

        private void generateExtension(Type type, object instance)
        {
            var entityFields = ReflectionUtil.GetFields(instance, FieldStatus.Entity);
            var dtoFields = ReflectionUtil.GetFields(instance, FieldStatus.Dto);
            var code = codeGenerator.GetExtensionCode(type.Name, entityFields, dtoFields);

            var codeFile = LDCFileInfo.clone(_selectedFile);
            codeFile.ProjectName = "LDC.Assemblies.DomainExtentions";
            codeFile.FileName += "Extension";

            codeGenerator.CreateCodeFile(codeFile, code);
            addToProject(codeFile);
        }

        private void generateMap(Type type, object instance)
        {
            var mapFields = ReflectionUtil.GetFields(instance, FieldStatus.Map);
            var code = codeGenerator.GetMapCode(type.Name, mapFields);

            var codeFile = LDCFileInfo.clone(_selectedFile);
            codeFile.ProjectName = "LDC.Model.Map";
            codeFile.FileName += "Map";

            codeGenerator.CreateCodeFile(codeFile, code);
            addToProject(codeFile);
        }

        private void generateRepository(Type type, object instance)
        {
            var entityFields = ReflectionUtil.GetFields(instance, FieldStatus.Entity);
            var code = codeGenerator.GetRepositoryCode(type.Name, entityFields);

            var codeFile = LDCFileInfo.clone(_selectedFile);
            codeFile.ProjectName = "LDC.Assemblies.Repository";
            codeFile.FileName += "Repository";

            codeGenerator.CreateCodeFile(codeFile, code);
            addToProject(codeFile);
        }

        private void generateService(Type type, object instance)
        {
            var serviceFields = ReflectionUtil.GetFields(instance, FieldStatus.Service);
            var code = codeGenerator.GetServiceCode(type.Name, serviceFields);

            var codeFile = LDCFileInfo.clone(_selectedFile);
            codeFile.ProjectName = "LDC.Assemblies.Service";
            codeFile.FileName += "Service";

            codeGenerator.CreateCodeFile(codeFile, code);
            addToProject(codeFile);
        }

        private void addToProject(LDCFileInfo file)
        {
            var project = SdkHelperTool.GetProjectByName(file.ProjectName);
            if (project == null)
            {
                project = SdkHelperTool.NewProject(file.ProjectPath, file.ProjectName);

                if (project == null)
                {
                    project = SdkHelperTool.GetProjectByName(file.ProjectName);
                }
            }

            project.ProjectItems.AddFromFile(file.FullPath);
        }
    }
}
