using System;
using System.Collections.Generic;
using System.Threading;

namespace LDC.Generator.Kamelon.Results
{
    public class LDCFileInfo
    {
        public string SolutionPath { get; set; } = "";

        public string ProjectName { get; set; } = "";

        public string ProjectPath => SolutionPath + ProjectName + "\\";

        public List<string> Folders { get; set; } = new List<string>();

        public string FolderName => string.Join("\\", Folders);

        public string FolderPath => ProjectPath + FolderName + "\\";

        public string Extension { get; set; } = "";

        public string FileName { get; set; } = "";

        public string FilePath => FolderPath + File;

        public string File
        {
            get { return FileName + "." + Extension; }
            set
            {
                var pieces = value.Split('.');

                if (pieces.Length >= 2)
                {
                    FileName = pieces[0];
                    Extension = pieces[1];
                }
                else if (pieces.Length == 1)
                {
                    FileName = pieces[0];
                    Extension = "";
                }
                else
                {
                    FileName = "";
                    Extension = "";
                }

            }
        }

        public string FullPath => FilePath;

        public string Namespace => ProjectName + "." + string.Join(".", Folders) + "." + FileName;

        public void parseFile(string path)
        {
            var pathPieces = path.Split('\\');

            if (pathPieces.Length >= 1)
            {
                File = pathPieces[pathPieces.Length - 1];

                for (var i = 0; i < pathPieces.Length - 1; i++)
                {
                    Folders.Add(pathPieces[i]);
                }
            }
        }

        public static LDCFileInfo clone(LDCFileInfo fileInfo)
        {
            return
                new LDCFileInfo()
                {
                    SolutionPath = fileInfo.SolutionPath,
                    ProjectName = fileInfo.ProjectName,
                    Folders = fileInfo.Folders,
                    Extension = fileInfo.Extension,
                    FileName = fileInfo.FileName
                };
        }
    }
}