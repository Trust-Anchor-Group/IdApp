using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UpdateVersionInfo
{
    class Program
    {
        private static readonly string AssemblyVersionExpression = @"^\s*\[assembly:\s*(?<attribute>(?:System\.)?(?:Reflection\.)?AssemblyVersion(?:Attribute)?\s*\(\s*""(?<version>[^""]+)""\s*\)\s*)\s*\]\s*$";
        private static readonly string AssemblyFileVersionExpression = @"^\s*\[assembly:\s*(?<attribute>(?:System\.)?(?:Reflection\.)?AssemblyFileVersion(?:Attribute)?\s*\(\s*""(?<version>[^""]+)""\s*\)\s*)\s*\]\s*$";

        private static readonly Regex assemblyVersionRegEx = new(AssemblyVersionExpression, RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex assemblyFileVersionRegEx = new(AssemblyFileVersionExpression, RegexOptions.Multiline | RegexOptions.Compiled);

        static void Main(string[] args)
        {
            CommandLineArguments commandLine = new(args);

            try
            {
                if (ValidateCommandLine(commandLine))
                {

                    Version version = new(
                        commandLine.Major,
                        commandLine.Minor,
                        commandLine.Build.Value,
                        /*!!!
                        commandLine.Revision.HasValue ? commandLine.Revision.Value : 
                        */
                        0);

                    UpdateCSVersionInfo(commandLine.VersionCsPath, version);

                    if (!string.IsNullOrEmpty(commandLine.AndroidManifestPath))
                        UpdateAndroidVersionInfo(commandLine.AndroidManifestPath, version);

                    if (!string.IsNullOrEmpty(commandLine.TouchPListPath))
                        UpdateTouchVersionInfo(commandLine.TouchPListPath, version);
                }
            }
            catch (Exception e)
            {
                WriteHelp(commandLine, "An unexpected error was encountered:" + e.Message);
            }
        }

        private static void UpdateCSVersionInfo(string path, Version version)
        {
            string contents;
            using (StreamReader reader = new(path))
            {
                contents = reader.ReadToEnd();
            }

            contents = assemblyVersionRegEx.Replace(contents, "[assembly: System.Reflection.AssemblyVersion(\"" + version.ToString() + "\")]");
            if (assemblyFileVersionRegEx.IsMatch(contents))
                contents = assemblyFileVersionRegEx.Replace(contents, "[assembly: System.Reflection.AssemblyFileVersion(\"" + version.ToString() + "\")]");
			
            using StreamWriter writer = new(path, false);
			writer.Write(contents);
		}

        private static void UpdateAndroidVersionInfo(string path, Version version)
        {
            string versionBuildExpression = "android:versionCode=\"[.0-9]+\"";
            string versionNameExpression = "android:versionName=\"[.0-9]+\"";

            Regex versionBuildRegEx = new(versionBuildExpression, RegexOptions.Compiled);
            Regex versionNameRegEx = new(versionNameExpression, RegexOptions.Compiled);

            string contents;
            using (StreamReader reader = new(path))
            {
                contents = reader.ReadToEnd();
            }

            var versionName = $"{version.Major}.{version.Minor}";

            contents = versionBuildRegEx.Replace(contents, "android:versionCode=\"" + version.Build + "\"");
            contents = versionNameRegEx.Replace(contents, "android:versionName=\"" + versionName + "\"");

			using StreamWriter writer = new(path, false);
			writer.Write(contents);
		}

        private static void UpdateTouchVersionInfo(string path, Version version)
        {
            string versionBuildExpression = "^\\s*<key>CFBundleShortVersionString</key>\\s*<string>[.0-9]+</string>\\s*$";
            string versionNameExpression = "^\\s*<key>CFBundleVersion</key>\\s*<string>[.0-9]+</string>\\s*$";

            Regex versionBuildRegEx = new(versionBuildExpression, RegexOptions.Multiline | RegexOptions.Compiled);
            Regex versionNameRegEx = new(versionNameExpression, RegexOptions.Multiline | RegexOptions.Compiled);

            string contents;
            using (StreamReader reader = new(path))
            {
                contents = reader.ReadToEnd();
            }

            var versionName = $"{version.Major}.{version.Minor}";

            var match1 = versionBuildRegEx.Match(contents);

            if (match1.Success)
                contents = versionBuildRegEx.Replace(contents, "\t<key>CFBundleShortVersionString</key>\r\n\t<string>" + versionName + "</string>\r");

            var match2 = versionNameRegEx.Match(contents);

            if (match2.Success)
                contents = versionNameRegEx.Replace(contents, "\t<key>CFBundleVersion</key>\r\n\t<string>" + version.Build + "</string>\r");

			using StreamWriter writer = new(path, false);
			writer.Write(contents);
		}


        private static bool ValidateCommandLine(CommandLineArguments commandLine)
        {
            if (commandLine.ShowHelp)
            {
                WriteHelp(commandLine);
                return false;
            }

            StringBuilder errors = new();

            if (commandLine.Major < 0)
                errors.AppendLine("You must supply a positive major version number.");

            if (commandLine.Minor < 0)
                errors.AppendLine("You must supply a positive minor version number.");

            if (!commandLine.Build.HasValue)
                errors.AppendLine("You must supply a numeric build number.");

            if (string.IsNullOrEmpty(commandLine.VersionCsPath) || !IsValidCSharpVersionFile(commandLine.VersionCsPath))
                errors.AppendLine("You must supply valid path to a writable C# file containing assembly version information.");

            if (!string.IsNullOrEmpty(commandLine.AndroidManifestPath) && !IsValidAndroidManifest(commandLine.AndroidManifestPath))
                errors.AppendLine("You must supply valid path to a writable android manifest file.");

            if (!string.IsNullOrEmpty(commandLine.TouchPListPath) && !IsValidTouchPList(commandLine.TouchPListPath))
                errors.AppendLine("You must supply valid path to a writable plist file containing version information.");

            if (errors.Length > 0)
            {
                WriteHelp(commandLine, "Invalid command line:\n" + errors.ToString());
                return false;
            }

            return true;
        }

        private static bool IsValidCSharpVersionFile(string path)
        {
            if (!File.Exists(path)) 
                return false;

            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) 
                return false;

            try
            {
                string contents;
                
                using (StreamReader reader = new(path))
                {
                    contents = reader.ReadToEnd();
                }

                if (assemblyVersionRegEx.IsMatch(contents))
                    return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }

            return false;
        }

        private static bool IsValidAndroidManifest(string path)
        {
            if (!File.Exists(path)) 
                return false;

            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) 
                return false;

            try
            {
                // <manifest ...
                XDocument doc = XDocument.Load(path);
				if (doc.Root is XElement rootElement && rootElement.Name == "manifest") 
                    return true;
			}
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }

            return false;
        }

        private static bool IsValidTouchPList(string path)
        {
            if (!File.Exists(path)) 
                return false;

            if ((new FileInfo(path).Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) 
                return false;

            try
            {
                //<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                XDocument doc = XDocument.Load(path);
                if (doc.DocumentType.Name == "plist")
                {
                    var shortVersionElement = doc.XPathSelectElement("plist/dict/key[string()='CFBundleShortVersionString']");
                    if (!(shortVersionElement is null))
						if (shortVersionElement.NextNode is XElement valueElement && valueElement.Name == "string") return true;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.Message);
            }

            return false;
        }

        private static void WriteHelp(CommandLineArguments commandLine, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
                Console.WriteLine(message);
            
            commandLine.WriteHelp(Console.Out);
        }
    }
}
