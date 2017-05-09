using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using LDC.Generator.Kamelon.Extensions;
using LDC.Generator.Kamelon.Results;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace LDC.Generator.Kamelon.VSExtension.Utility
{
    public static class SdkHelperTool
    {
        public static DTE2 GetActiveIde()
        {
            return Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        public static Project GetActiveProject()
        {
            Project activeProject = null;

            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;

            if (dte == null)
            {
                return null;
            }

            var activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects?.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject;
        }

        public static Project GetProjectByName(string projectName)
        {
            return Projects().FirstOrDefault(p => p.Name == projectName);
        }

        public static Project NewProject(string path, string projectName)
        {
            var solution = (Solution2)GetActiveIde().Solution;
            var template = solution.GetProjectTemplate("ClassLibrary.zip", "CSharp");
            return solution.AddFromTemplate(template, path, projectName);
        }

        public static string GetAssemblyPath(Project vsProject)
        {
            var fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            var outputPath = vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var outputDir = Path.Combine(fullPath, outputPath);
            var outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            var assemblyPath = Path.Combine(outputDir, outputFileName);
            return assemblyPath;
        }

        public static IList<Project> Projects()
        {
            var projects = GetActiveIde().Solution.Projects;
            var list = new List<Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        public static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            var list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }


        public static LDCFileInfo GetSelectedFile(DTE2 dte)
        {
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;
            var items = solutionExplorer.SelectedItems as object[];
            var item = items?[0] as UIHierarchyItem;
            
            var projectItem = item?.Object as ProjectItem;
            if (projectItem == null)
            {
                throw new Exception("Seçilen elemanın atanmasında hata oluştu");
            }
            
            var selectedFile = new LDCFileInfo();
            var solutionFile = dte.Solution.Properties.Item("Name").Value + ".sln";
            Debug.WriteLine($"solutionFile : '{solutionFile}'");

            selectedFile.SolutionPath = dte.Solution.Properties.Item("Path").Value.ToString().Replace(solutionFile, "");
            selectedFile.ProjectName = projectItem.ContainingProject.Properties.Item("AssemblyName").Value.ToString();

            var fullPath = projectItem.Properties.Item("FullPath").Value.ToString();
            Debug.WriteLine($"fullPath: '{fullPath}'");

            var fileFolder = fullPath.Replace(selectedFile.ProjectPath, "");
            Debug.WriteLine($"fileFolder: '{fileFolder}'");

            selectedFile.parseFile(fileFolder);

            return selectedFile;
        }


        public static void AddFromFile(Project project, LDCFileInfo path, string file)
        {
            var projctitem = ExistProjectFolder(project, path);
            projctitem.AddFromFileCopy(file);
        }

        public static ProjectItems ExistProjectFolder(Project project, LDCFileInfo paths)
        {
            if (project == KamelonProjects.DtoProject)
            {
                for (var i = 1; i < project.ProjectItems.Count; i++)
                {
                    var items = project.ProjectItems.Item(i);
                    if (items.Name != "EntityDtos")
                        continue;

                    for (var k = 1; k < items.ProjectItems.Count; k++)
                    {
                        var item = items.ProjectItems.Item(k);
                        if (item.Name == paths.FolderName)
                            return item.ProjectItems;
                    }

                    try
                    {
                        items.ProjectItems.AddFolder(paths.FolderName);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    return project.ProjectItems;
                }

                return project.ProjectItems;
            }

            for (var i = 1; i < project.ProjectItems.Count; i++)
            {
                var items = project.ProjectItems.Item(i);
                if (items.Name == paths.FolderName)
                    return items.ProjectItems;
            }

            try
            {
                project.ProjectItems.AddFolder(paths.FolderName);
            }
            catch (Exception)
            {
                // ignored
            }

            return project.ProjectItems;
        }

        public static List<string> GetAllProjectFiles(ProjectItems projectItems, string extension)
        {
            var returnValue = new List<string>();
            foreach (ProjectItem projectItem in projectItems)
            {
                for (short i = 1; i <= projectItems.Count; i++)
                {
                    var fileName = projectItem.FileNames[i];
                    var s = Path.GetExtension(fileName);
                    if (s != null && s.ToLower() == extension)
                        returnValue.Add(fileName);
                }

                try
                {
                    returnValue.AddRange(GetAllProjectFiles(projectItem.ProjectItems, extension));
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return returnValue;
        }

        public static List<string> GetFilesNotInProject(Project project)
        {
            var returnValue = new List<string>();
            var startPath = Path.GetDirectoryName(project.FullName);
            var projectFiles = GetAllProjectFiles(project.ProjectItems, ".cs");

            if (startPath == null)
            {
                return returnValue;
            }

            returnValue.AddRange(Directory.GetFiles(startPath, "*.cs", SearchOption.AllDirectories).Where(file => !projectFiles.Contains(file)));
            return returnValue;
        }

        //path is a list of folders from the root of the project.
        public static void AddFolder(Project project, string newFolder)
        {
            project.ProjectItems.AddFolder(newFolder);
        }

        //path is a list of folders from the root of the project.
        public static void DeleteFileOrFolder(Project project, List<string> path, string item)
        {
            var pi = path.Aggregate(project.ProjectItems, (current, t) => current.Item(t).ProjectItems);

            pi.Item(item).Delete();
        }

        public static string GetRootNameSpace(Project project)
        {
            return project.Properties.Item("RootNamespace").Value.ToString();
        }
    }
}