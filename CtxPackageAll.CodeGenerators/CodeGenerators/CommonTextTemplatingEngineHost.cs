using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CtxPackageAll.CodeGenerators
{
    [Serializable]
    public class CommonTextTemplatingEngineHost : ITextTemplatingEngineHost
    {
        #region Implement ITextTemplatingEngineHost

        //Called by the Engine to enquire about 
        //the processing options you require. 
        //If you recognize that option, return an 
        //appropriate value. 
        //Otherwise, pass back NULL.
        //--------------------------------------------------------------------
        public object GetHostOption(string optionName)
        {
            object returnObj = null;
            switch (optionName)
            {
                case "CacheAssemblies":
                    returnObj = true;
                    break;
                default:
                    break;
            }
            return returnObj;
        }

        //The engine calls this method based on the optional include directive
        //if the user has specified it in the text template.
        //This method can be called 0, 1, or more times.
        //---------------------------------------------------------------------
        //The included text is returned in the context parameter.
        //If the host searches the registry for the location of include files,
        //or if the host searches multiple locations by default, the host can
        //return the final path of the include file in the location parameter.
        //---------------------------------------------------------------------
        public bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            Logger.Log("LoadIncludeText");

            content = string.Empty;
            location = string.Empty;
            //If the argument is the fully qualified path of an existing file,
            //then we are done.
            //----------------------------------------------------------------
            if (File.Exists(requestFileName))
            {
                content = File.ReadAllText(requestFileName);
                return true;
            }
            //This can be customized to search specific paths for the file.
            //This can be customized to accept paths to search as command line
            //arguments.
            //----------------------------------------------------------------
            else
            {
                return false;
            }
        }

        //The engine calls this method when it is done processing a text
        //template to pass any errors that occurred to the host.
        //The host can decide how to display them.
        //---------------------------------------------------------------------
        public void LogErrors(CompilerErrorCollection _errors)
        {
            errors = _errors;
        }
        //This is the application domain that is used to compile and run
        //the generated transformation class to create the generated text output.
        //----------------------------------------------------------------------
        public AppDomain ProvideTemplatingAppDomain(string content)
        {
            //This host will provide a new application domain each time the 
            //engine processes a text template.
            //-------------------------------------------------------------
            return AppDomain.CurrentDomain;// AppDomain.CreateDomain("Generation App Domain");
            //This could be changed to return the current appdomain, but new 
            //assemblies are loaded into this AppDomain on a regular basis.
            //If the AppDomain lasts too long, it will grow indefintely, 
            //which might be regarded as a leak.
            //This could be customized to cache the application domain for 
            //a certain number of text template generations (for example, 10).
            //This could be customized based on the contents of the text 
            //template, which are provided as a parameter for that purpose.
        }

        //The engine calls this method to resolve assembly references used in
        //the generated transformation class project and for the optional 
        //assembly directive if the user has specified it in the text template.
        //This method can be called 0, 1, or more times.
        //---------------------------------------------------------------------
        public string ResolveAssemblyReference(string assemblyReference)
        {                      

            if (assemblyReference.ToLower().Substring(assemblyReference.Length - 4, 4) != ".dll")
            {
                assemblyReference += ".dll";
            }
            //If the argument is the fully qualified path of an existing file,
            //then we are done. (This does not do any work.)
            //----------------------------------------------------------------
            if (File.Exists(assemblyReference))
            {
                return assemblyReference;
            }

            //Maybe the assembly is in the same folder as the text template that 
            //called the directive.
            //----------------------------------------------------------------
            string candinate = Path.Combine(Path.GetDirectoryName(this.TemplateFile), assemblyReference);
            if (File.Exists(candinate))
            {
                return candinate;
            }

            string programFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string[] basePaths = new string[] { Path.Combine(windowsPath,@"Microsoft.NET\Framework\v4.0.30319"), 
                                                Path.Combine(windowsPath,@"Microsoft.NET\Framework\v3.5"),
                                                Path.Combine(windowsPath,@"Microsoft.NET\Framework\v3.0"),
                                                Path.Combine(windowsPath,@"Microsoft.NET\Framework\v2.0.50727"),
                                                GetVSPublicAssembliesPath()};
            foreach (var path in basePaths)
            {
                string filePath = Path.Combine(path, assemblyReference);
                if (File.Exists(filePath))
                    return filePath;
            }

            try
            {
                Assembly assem = Assembly.Load(assemblyReference);
                if (assem != null)
                    return assem.Location;
            }
            catch (Exception error)
            { }

            //This can be customized to search specific paths for the file
            //or to search the GAC.
            //----------------------------------------------------------------
            //This can be customized to accept paths to search as command line
            //arguments.
            //----------------------------------------------------------------
            //If we cannot do better, return the original file name.
            return "";
        }

        //The engine calls this method based on the directives the user has 
        //specified in the text template.
        //This method can be called 0, 1, or more times.
        //---------------------------------------------------------------------
        public Type ResolveDirectiveProcessor(string processorName)
        {
            //This host will not resolve any specific processors.
            //Check the processor name, and if it is the name of a processor the 
            //host wants to support, return the type of the processor.
            //---------------------------------------------------------------------
            if (string.Compare(processorName, "XYZ", StringComparison.OrdinalIgnoreCase) == 0)
            {
                //return typeof();
            }
            //This can be customized to search specific paths for the file
            //or to search the GAC
            //If the directive processor cannot be found, throw an error.
            throw new Exception("Directive Processor not found");
        }

        //If a call to a directive in a text template does not provide a value
        //for a required parameter, the directive processor can try to get it
        //from the host by calling this method.
        //This method can be called 0, 1, or more times.
        //---------------------------------------------------------------------
        public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
        {
            if (directiveId == null)
            {
                throw new ArgumentNullException("the directiveId cannot be null");
            }
            if (processorName == null)
            {
                throw new ArgumentNullException("the processorName cannot be null");
            }
            if (parameterName == null)
            {
                throw new ArgumentNullException("the parameterName cannot be null");
            }
            //Code to provide "hard-coded" parameter values goes here.
            //This code depends on the directive processors this host will interact with.
            //If we cannot do better, return the empty string.

            if (directiveId == "directiveId")
            {
                if (processorName == "namespaceDirectiveProcessor")
                {
                    if (parameterName == "namespaceHint")
                    {
                        return NamespaceSuggestion;
                    }
                }
            }
            
            return String.Empty;
        }

        //A directive processor can call this method if a file name does not 
        //have a path.
        //The host can attempt to provide path information by searching 
        //specific paths for the file and returning the file and path if found.
        //This method can be called 0, 1, or more times.
        //---------------------------------------------------------------------
        public string ResolvePath(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("the file name cannot be null");
            }
            //If the argument is the fully qualified path of an existing file,
            //then we are done
            //----------------------------------------------------------------
            if (File.Exists(fileName))
            {
                return fileName;
            }
             
            string[] candidatePaths = new string[] { 
                Path.GetDirectoryName(CurrentProjectItem.FileNames[1]),
                Path.GetDirectoryName(this.TemplateFile)
            };
            //Maybe the file is in the same folder as the text template that 
            //called the directive.
            //----------------------------------------------------------------
            foreach (var candidatePath in candidatePaths)
            {
                string candidate = Path.Combine(candidatePath, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            //Look more places.
            //----------------------------------------------------------------
            //More code can go here...
            //If we cannot do better, return the original file name.
            return fileName;
        }

        //The engine calls this method to change the extension of the 
        //generated text output file based on the optional output directive 
        //if the user specifies it in the text template.
        //---------------------------------------------------------------------
        public void SetFileExtension(string extension)
        {
            //The parameter extension has a '.' in front of it already.
            //--------------------------------------------------------
            outputFileExtension = extension;
        }

        //The engine calls this method to change the encoding of the 
        //generated text output file based on the optional output directive 
        //if the user specifies it in the text template.
        //----------------------------------------------------------------------
        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            outputFileEncoding = encoding;
        }

        //The host can provide standard assembly references.
        //The engine will use these references when compiling and
        //executing the generated transformation class.
        public IList<string> StandardAssemblyReferences
        {
            get
            {
                return new string[]
                {
                    //If this host searches standard paths and the GAC,
                    //we can specify the assembly name like this.
                    //---------------------------------------------------------
                    //"System"

                    //Because this host only resolves assemblies from the 
                    //fully qualified path and name of the assembly,
                    //this is a quick way to get the code to give us the
                    //fully qualified path and name of the System assembly.
                    //---------------------------------------------------------
                    typeof(System.Uri).Assembly.Location
                };
            }
        }

        //The host can provide standard imports or using statements.
        //The engine will add these statements to the generated 
        //transformation class.
        public IList<string> StandardImports
        {
            get
            {
                return new string[] { "System" };
            }
        }

        /// <summary>
        /// Get or Set the path and file name of the text template that is being processed
        /// </summary>
        public string TemplateFile
        {
            get;
            set;
        }

        #endregion

        #region Properties

        private string outputFileExtension = ".txt";
        public string OutputFileExtension
        {
            get
            {
                return outputFileExtension;
            }
        }

        private Encoding outputFileEncoding = Encoding.UTF8;
        public Encoding OutputFileEncoding
        {
            get
            {
                return outputFileEncoding;
            }
        }

        private CompilerErrorCollection errors;
        public CompilerErrorCollection Errors
        {
            get
            {
                return errors;
            }
        }

        public string NamespaceSuggestion
        {
            get;
            set;
        }

        public EnvDTE.ProjectItem CurrentProjectItem
        {
            get;
            set;
        }

        #endregion

        #region Other Methods

        private string GetVSPublicAssembliesPath()
        {
            RegistryKey key = null;
            try
            {
                key = Registry.LocalMachine.OpenSubKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0");
            }
            catch (Exception ex)
            { 
            }
            if (key == null)
            {
                try
                {
                    key = Registry.CurrentUser.OpenSubKey(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\10.0");
                }
                catch (Exception ex)
                { }
            }
            if (key != null)
            {
                try
                {
                    object value = key.GetValue("InstallDir");
                    if (value != null)
                    {
                        return Path.Combine(value.ToString(), "PublicAssemblies");
                    }
                }
                catch (Exception e)
                { }
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies");
        }

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);
        static bool CheckLibrary(string fileName) { return LoadLibrary(fileName) == IntPtr.Zero; }
        
        #endregion
    }
}
