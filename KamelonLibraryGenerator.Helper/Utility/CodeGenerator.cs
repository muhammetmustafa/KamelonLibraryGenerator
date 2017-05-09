using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LDC.Generator.Kamelon.Resources;
using LDC.Generator.Kamelon.Results;

namespace LDC.Generator.Kamelon.Utility
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeGenerator
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region UtilityMethods

        public bool CreateCodeFile(LDCFileInfo file, string content)
        {
            try
            {
                if (!Directory.Exists(file.FolderPath))
                {
                    Directory.CreateDirectory(file.FolderPath);
                    Debug.WriteLine($"Folder '{file.FolderPath}' created!");
                }
                else
                {
                    Debug.WriteLine($"Folder '{file.FolderPath}' already exists!");
                }

                if (File.Exists(file.FullPath))
                {
                    Debug.WriteLine($"File '{file.File}' already exists!");
                    return true;
                }

                content = content.Replace("||namespace||", file.Namespace);

                using (var writer = new StreamWriter(file.FullPath))
                {
                    writer.WriteLine(content);
                }

                Debug.WriteLine($"File '{file.File}' created!");
                return true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Exception " + exception.Message);
                Debug.WriteLine(exception.StackTrace);
                return false;
            }
        }
        
        #endregion
        
        public string GetDtoCode(string name, Dictionary<string, string> properties)
        {
            var code = new StringBuilder();

            foreach (var property in properties)
            {
                code.Append($"public {property.Value} {property.Key}");
                code.Append("{ get; set; }\n\n");
            }

            return LibraryResource.Dto.Replace("{0}", code.ToString()).Replace("$", name);
        }
        
        public string GetExtensionCode(string name, Dictionary<string, string> entitymembers, Dictionary<string, string> dtomembers)
        {
            var code = LibraryResource.DataExtension;

            var retVal = dtomembers.Where(dtomember => !dtomember.Key.Contains("Id"))
                .Aggregate(string.Empty, (current, dtomember) => 
                $"entity.{dtomember.Key} = dto.{entitymembers.FirstOrDefault(p => p.Key.Contains(dtomember.Key)).Key};\n"
            );

            code = code.Replace("{ToEntity}", retVal);

            var tmp = new StringBuilder();

            foreach (var dtomember in dtomembers)
            {
                if (!dtomember.Key.Contains("Id"))
                {
                    tmp.Append($"dto.{dtomember.Key} = entity.{entitymembers.FirstOrDefault(p => p.Key.Contains(dtomember.Key)).Key};\n");
                }
                else
                {
                    var _0 = dtomember.Key;
                    var _1 = entitymembers.FirstOrDefault(p => p.Key.Contains(dtomember.Key.Replace("Id", ""))).Key;

                    tmp.Append($"dto.{_0} = entity.{_1} != null ? entity.{_1}.Id : dto.{_0};\n");
                }
            }
            
            return code.Replace("{ToDto}", tmp.ToString()).Replace("$", name);
        }
        
        
        public string GetMapCode(string name, Dictionary<string, string> entitymap)
        {
            var code = new StringBuilder();

            foreach (var member in entitymap)
            {
                if (member.Key.Contains("#"))
                {
                    var _0 = member.Key.Replace("#", "");

                    code.Append($"References(m => m.{_0}).Column(\"{_0}Id\").ForeignKey(\"FK$_{_0}Id\").Cascade.All();\n");
                }
                else
                {
                    code.Append($"Map(m => m.{member.Key});\n");
                }

            }

            return LibraryResource.Map.Replace("{Maps}", code.ToString()).Replace("$", name);
        }
        
        public string GetRepositoryCode(string name, Dictionary<string, string> entitymap)
        {
            return LibraryResource.Repository.Replace("$", name);
        }
        
        public string GetServiceCode(string name, Dictionary<string, string> entitymap)
        {
            var code = LibraryResource.Service;

            var variableForRepository = PrepareVariableforRepository(entitymap);
            code = code.Replace("{VariableforRepository}", variableForRepository);

            var variableDefinitionsforRepository = PrepareVariableDefinitionsforRepository(entitymap);
            code = code.Replace("{VariableDefinitionsforRepository}", variableDefinitionsforRepository);

            var variableAssignmentforRepository = PrepareVariableAssignmentforRepository(entitymap);
            code = code.Replace("{VariableAssignmentforRepository}", variableAssignmentforRepository);

            var entityLoaderforRepository = PrepareEntityLoaderforRepository(entitymap);
            code = code.Replace("{EntityLoaderforRepository}", entityLoaderforRepository);

            return code.Replace("$", name);
        }

        private string PrepareVariableforRepository(Dictionary<string, string> members)
        {
            return members.Where(member => member.Key.Contains("#"))
                .Aggregate(string.Empty, (current, member) =>
                {
                    var _0 = member.Key.Replace("#", "");

                    return $"{current} private readonly I{_0}Repository _{_0}Repository;\n";
                });
        }

        private string PrepareVariableDefinitionsforRepository(Dictionary<string, string> members)
        {
            return members.Where(member => member.Key.Contains("#"))
                .Aggregate(string.Empty, (current, member) =>
                {
                    var _0 = member.Key.Replace("#", "");

                    return $"{current}, I{_0}Repository {_0}Repository ";
                });
        }

        private string PrepareVariableAssignmentforRepository(Dictionary<string, string> members)
        {
            return members.Where(member => member.Key.Contains("#"))
                .Aggregate(string.Empty, (current, member) =>
                {
                    var _0 = member.Key.Replace("#", "");

                    return $"current_{_0}Repository = {_0}Repository;";
                });
        }

        private string PrepareEntityLoaderforRepository(Dictionary<string, string> members)
        {
            return members.Where(member => member.Key.Contains("#"))
                .Aggregate(string.Empty, (current, member) =>
                {
                    var _0 = member.Key.Replace("#", "");
                    
                    return $"{current}if (dto.{_0}Id.ToLong() > 0) \n entity.{_0} = _{_0}Repository.Load(dto.{_0}Id);";

                });
        }
        
    }
}
