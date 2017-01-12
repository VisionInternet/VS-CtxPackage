using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System.Runtime.InteropServices;
using System.IO;
using System.CodeDom.Compiler;
using System.Windows.Forms;
using System.Xml;

namespace CtxPackageAll.CodeGenerators
{
    [Serializable]
    [GuidAttribute("B77273B0-0C29-4E33-93C8-433EF013033C")]
    public class T4CommonTextTemplatingFileGenerator : BaseCodeGeneratorWithSite
    {
        static readonly bool SupportSyncInstruction = !bool.FalseString.Equals(Configurations.Settings["SupportSyncInstruction"], StringComparison.InvariantCultureIgnoreCase);

        #region Properties
        public string TemplatePath { get; private set; }
        public EnvDTE.ProjectItem CurrentProjectItem { get; set; }
        public EnvDTE.OutputWindowPane CurrentOutputWindowPane { get; set; }
        #endregion

        #region Ctors

        public T4CommonTextTemplatingFileGenerator()
        {
            TemplatePath = Configurations.Settings["BaseTemplatePath"] ?? Path.Combine(Configurations.CurrentPath, "Templates");
        }

        #endregion
        
        #region Generate Code

        protected string GenerateCode(EnvDTE.ProjectItem projectItem)
        {
            Logger.Log("GenerateCode");

            var projectDirectory = projectItem.ContainingProject.FullName;
            var namespaceSuggestion = GetNamespaceSuggestion(projectItem);
            var templateFileFullName = GetTemplateFileFullName(projectItem);
            var inputDataFilename = projectItem.FileNames[1];
            var inputFileContent = File.ReadAllText(inputDataFilename);
            var needGenerateFile = true;

            if (SupportSyncInstruction)
            {
                var syncRegexExpression = "(/\\*@sync)(.*)?;\\*/";
                var syncMatches = System.Text.RegularExpressions.Regex.Matches(inputFileContent, syncRegexExpression);
                if (syncMatches.Count > 0)
                {
                    var replacedInputFileContent = System.Text.RegularExpressions.Regex.Replace(inputFileContent, syncRegexExpression, "");
                    foreach (System.Text.RegularExpressions.Match match in syncMatches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            var parameters = match.Groups[2].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var syncFileName = parameters[0].Trim(new Char[] { '"', ' ' });

                            if (parameters.Length >= 2)
                                needGenerateFile = !bool.FalseString.Equals(parameters[1], StringComparison.InvariantCultureIgnoreCase);

                            var needPromt = true;
                            if (parameters.Length >= 3)
                                needPromt = !bool.FalseString.Equals(parameters[2], StringComparison.InvariantCultureIgnoreCase);


                            var newProjectItemName = GetFilePath(inputDataFilename, syncFileName, projectDirectory);
                            if (File.Exists(newProjectItemName))
                            {
                                var newProjectItem = (projectItem.DTE.Solution as EnvDTE.SolutionClass).FindProjectItem(newProjectItemName);
                                var wasOpened = false;
                                if (newProjectItem != null)
                                {
                                    wasOpened = newProjectItem.IsOpen;
                                    if (projectItem.DTE.SourceControl != null)
                                    {
                                        projectItem.DTE.SourceControl.CheckOutItem(newProjectItemName);
                                    }
                                }

                                if (wasOpened && newProjectItem.Document != null)
                                {
                                    newProjectItem.Document.Close(EnvDTE.vsSaveChanges.vsSaveChangesYes);
                                }

                                using (var fileWriter = new System.IO.StreamWriter(newProjectItemName, false))
                                {
                                    fileWriter.Write(replacedInputFileContent);
                                }

                                //if (newProjectItem != null)
                                //{
                                //    newProjectItem.Save();
                                //}

                                if (needPromt)
                                {
                                    MessageBox.Show(string.Format("The file: {0} has been synced successful.", syncFileName));
                                }
                                else
                                {
                                    Output(projectItem.DTE, string.Format("The file: {0} has been synced successful.", syncFileName));
                                }
                            }
                            else
                            {
                                Output(projectItem.DTE, string.Format("Can't find The file: {0} which need to be synced.", syncFileName));
                            }
                        }
                    }
                }
            }

            if(needGenerateFile)
            {
                var host = new CommonTextTemplatingEngineHost();
                var engine = new Microsoft.VisualStudio.TextTemplating.Engine();
                host.CurrentProjectItem = projectItem;
                host.TemplateFile = templateFileFullName;
                host.NamespaceSuggestion = namespaceSuggestion;

                //Read the text template
                string input = File.ReadAllText(templateFileFullName);

                input = input.Replace("$inputDataFilename$", inputDataFilename);
                input = input.Replace("$namespace$", namespaceSuggestion);
                input = input.Replace("$projectFilePath$", projectDirectory);


                //return NamespaceSuggestion +"  "+ edmxDataFilename;
                //Transform the text template

                string output = engine.ProcessTemplate(input, host);
                if (host.Errors != null && host.Errors.Count > 0)
                {
                    output += "//" + inputDataFilename + Environment.NewLine;
                    output += "//" + namespaceSuggestion + Environment.NewLine;

                    foreach (CompilerError error in host.Errors)
                    {
                        output += Environment.NewLine + error.ErrorText;
                    }
                }

                output = System.Text.RegularExpressions.Regex.Replace(output, "(\n[ ]*){2,}", "\n");
                return output;
            }
            return null;
        }
        #endregion

        #region Implement BaseCodeGeneratorWithSite abstract methods

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            Logger.Log("GenerateCode byte");

            var projectItem = GetService(typeof(EnvDTE.ProjectItem)) as EnvDTE.ProjectItem;
            var templateFileContent = String.Empty;
            var templateEncoding = Encoding.UTF8;
            CurrentProjectItem = projectItem;
            templateFileContent = GenerateCode(projectItem);
            return templateEncoding.GetBytes(templateFileContent); 
        }
        

        public override string GetDefaultExtension()
        {
            return GetExtension(CurrentProjectItem);
        }

        public string GetExtension(EnvDTE.ProjectItem projectItem)
        {
            if (projectItem == null)
                return ".cs";
            var inputDataFilename = projectItem.FileNames[1];
            return GetGeneratedFileExtension(inputDataFilename);
        }

        #endregion

        #region Other Methods
        private EnvDTE.ProjectItem GetProjectItem(EnvDTE.Solution solution, string projectItemFullPath)
        {
            var project = default(EnvDTE.Project);

            for (var index = 1; index <= solution.Projects.Count; index++)
            {
                if (!string.IsNullOrEmpty(solution.Projects.Item(index).FullName) && projectItemFullPath.StartsWith(solution.Projects.Item(index).FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    project = solution.Projects.Item(index); 
                    break;
                }
            }
            return project != null ? GetProjectItem(project, projectItemFullPath) : null;
        }
        private EnvDTE.ProjectItem GetProjectItem(EnvDTE.Project project, string projectItemFullPath)
        {
            projectItemFullPath = projectItemFullPath.ToLowerInvariant();
            if (projectItemFullPath.StartsWith(project.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                var segements = projectItemFullPath.Substring(project.FullName.Length).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var index = 0;
                var currentProjectItem = default(EnvDTE.ProjectItem);
                var currentProjectItemFullName = project.FullName.TrimEnd('\\');
                var currentSegement = segements[index];
                var projectItems = project.ProjectItems;
                do
                {
                    projectItems = index == 0 ? project.ProjectItems : currentProjectItem.ProjectItems;
                    currentSegement = segements[index];
                    currentProjectItemFullName = Path.Combine(currentProjectItemFullName, currentSegement);
                    for (var i = 1; i <= projectItems.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(projectItems.Item(i).FileNames[1]) && projectItems.Item(i).FileNames[1].TrimEnd('\\').Equals(currentProjectItemFullName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentProjectItem = projectItems.Item(i);
                            break;
                        }
                    }
                    if (currentProjectItem == null)
                        break;
                } while (++index < segements.Length);
                return currentProjectItem;
            }
            return null;
        }

        private string GetNamespaceSuggestion(EnvDTE.ProjectItem projectItem)
        {
            return projectItem.ContainingProject.Name +"." + Path.GetFileNameWithoutExtension(projectItem.Name);
        }

        private string GetTemplateFileFullName(EnvDTE.ProjectItem projectItem)
        {
            var templateFileName = GetTemplatFileName(projectItem.FileNames[0]);
            return Path.Combine(TemplatePath, templateFileName);
        }

        /// <summary>
        /// Will return the suffix from the first '.'; e.g.: if file name is: D:\a.tt.cs, will return .tt.cs;
        /// </summary>
        /// <param name="projectItemFileName"></param>
        /// <returns></returns>
        private string GetProjectItemSuffixExtensions(string projectItemFileName)
        {
            if (string.IsNullOrEmpty(projectItemFileName))
                return string.Empty;
           
            var firstDotIndex = Path.GetFileName(projectItemFileName).IndexOf('.');
            if (firstDotIndex >= 0)
                return Path.GetFileName(projectItemFileName).Substring(firstDotIndex);

            return string.Empty;
        }

        private string GetGeneratedFileExtension(string projectItemFileName)
        {
            return Configurations.ExtensionsMapping[Path.GetFileName(projectItemFileName).ToLowerInvariant()]
                ?? Configurations.ExtensionsMapping[GetProjectItemSuffixExtensions(projectItemFileName)]
                ?? Configurations.ExtensionsMapping[Path.GetExtension(projectItemFileName)]
                ?? ".cs";
        }

        private string GetTemplatFileName(string projectItemFileName)
        {
            var projectItemExtension = GetProjectItemSuffixExtensions(projectItemFileName);
            var templateFileName = Configurations.TemplatesMapping[Path.GetFileName(projectItemFileName).ToLowerInvariant()]
                                 ?? Configurations.TemplatesMapping[projectItemExtension]
                                 ?? Configurations.TemplatesMapping[Path.GetExtension(projectItemFileName)]
                                 ?? Configurations.TemplatesMapping["DefaultTemplateFileName"];
            templateFileName = templateFileName ?? "Template.tt";
            return templateFileName;
        }

        public string GetFilePath(string currentFileFullName, string importedFileName, string projectFullPath = "")
        {
            var currentFileDirectory = Path.GetDirectoryName(currentFileFullName);
            if (System.IO.File.Exists(importedFileName))
            {
                return importedFileName;
            }
            if (importedFileName.StartsWith("~/"))
            {
                return System.IO.Path.Combine(projectFullPath, importedFileName.Replace("/", "\\").Substring(2));
            }
            else if (importedFileName.StartsWith("../"))
            {
                var currentFileDirectoryInfo = new DirectoryInfo(currentFileDirectory);
                var virtualPathPathSegments = importedFileName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var currentDirectory = currentFileDirectoryInfo;
                foreach (var segment in virtualPathPathSegments)
                {
                    if (segment.Equals(".."))
                        currentDirectory = currentDirectory.Parent;
                    else
                    {
                        currentDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, segment));
                    }
                }
                return currentDirectory.FullName;
            }
            else if (importedFileName.StartsWith("/"))
            {
                return Path.Combine(currentFileDirectory, importedFileName.Replace("/", "\\").Substring(1));
            }
            else
            {
                return Path.Combine(currentFileDirectory, importedFileName.Replace("/", "\\"));
            }
        }

        private void Output(EnvDTE.DTE dte, string message)
        {
            EnvDTE.Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            if (window != null)
            {
                EnvDTE.OutputWindow outputWindow = (EnvDTE.OutputWindow)window.Object;

                EnvDTE.OutputWindowPane outputWindowPane = outputWindow.ActivePane;
                if (outputWindowPane == null)
                {
                    for (uint i = 1; i <= outputWindow.OutputWindowPanes.Count; i++)
                    {
                        if (outputWindow.OutputWindowPanes.Item(i).Name.Equals(EnvDTE.Constants.vsWindowKindOutput, StringComparison.CurrentCultureIgnoreCase))
                        {
                            outputWindowPane = outputWindow.OutputWindowPanes.Item(i);
                            break;
                        }
                    }
                }

                if (outputWindowPane == null)
                    outputWindowPane = outputWindow.OutputWindowPanes.Add(EnvDTE.Constants.vsWindowKindOutput);
                outputWindowPane.Activate();
                outputWindowPane.OutputString(message);
                outputWindowPane.OutputString("\r\n");
            }
        }
        #endregion

    }
}
