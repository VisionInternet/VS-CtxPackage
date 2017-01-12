using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Specialized;

namespace CtxPackageAll.CodeGenerators
{
    public static class Configurations
    {
        public static readonly string CurrentPath = null;
        public static readonly NameValueCollection Settings = null;
        public static readonly NameValueCollection TemplatesMapping = null;
        public static readonly NameValueCollection ExtensionsMapping = null;

        static Configurations()
        {
            var assemblyLocation = typeof(T4CommonTextTemplatingFileGenerator).Assembly.Location;
            var assemblyFileName = Path.GetFileName(assemblyLocation);
            CurrentPath = assemblyLocation.Substring(0, assemblyLocation.LastIndexOf(assemblyFileName));

            Settings = new NameValueCollection();
            try
            {
                var settingsFile = Path.Combine(CurrentPath, "Settings.config");
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(settingsFile);
                foreach (XmlNode node in xmlDoc.SelectNodes("Configurations/Settings/add"))
                {
                    var key = node.Attributes["key"].Value;
                    var value = node.Attributes["value"].Value;
                    if (key != null && !Settings.AllKeys.Contains(key))
                    {
                        Settings.Add(key, value);
                    }
                }
                TemplatesMapping = new NameValueCollection();
                foreach (XmlNode node in xmlDoc.SelectNodes("Configurations/Templates/add"))
                {
                    var key = node.Attributes["key"].Value;
                    var value = node.Attributes["value"].Value;
                    if (key != null && !Settings.AllKeys.Contains(key))
                    {
                        TemplatesMapping.Add(key, value);
                    }
                }
                ExtensionsMapping = new NameValueCollection();
                foreach (XmlNode node in xmlDoc.SelectNodes("Configurations/Extensions/add"))
                {
                    var key = node.Attributes["key"].Value;
                    var value = node.Attributes["value"].Value;
                    if (key != null && !Settings.AllKeys.Contains(key))
                    {
                        ExtensionsMapping.Add(key, value);
                    }
                }
            }
            catch(Exception e) 
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
