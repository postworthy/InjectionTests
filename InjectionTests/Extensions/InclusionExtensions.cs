using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InjectionTests.Extensions
{
    public static class InclusionExtensions
    {
        private static Version xxeVersion = Version.Parse("4.5.2");
        public static bool HasDbCommands(this MethodDefinition method)
        {
            if (method?.Body?.Variables?.Any(l => l.VariableType.Name.Contains("IDbCommand") || l.VariableType.Name.Contains("DbCommand") || l.VariableType.Name.Contains("OracleCommand") || l.VariableType.Name.Contains("OleDbCommand")) == true)
                return true;
            else if (method?.Parameters?.Any(l => l.ParameterType.Name.Contains("IDbCommand") || l.ParameterType.Name.Contains("DbCommand") || l.ParameterType.Name.Contains("OracleCommand") || l.ParameterType.Name.Contains("OleDbCommand")) == true)
                return true;
            else return false;
        }

        public static bool HasLdapCommands(this MethodDefinition method)
        {
            if (method?.Body?.Variables?.Any(l => l.VariableType.FullName.Contains("System.DirectoryServices")) == true)
                return true;
            else if (method?.Parameters?.Any(l => l.ParameterType.FullName.Contains("System.DirectoryServices")) == true)
                return true;
            else return false;
        }

        public static bool HasFileSystemCommands(this MethodDefinition method)
        {
            if (method?.Body?.Variables?.Any(l => (l.VariableType.FullName.Contains("System.IO") && (l.VariableType.FullName.Contains("Drive") || l.VariableType.FullName.Contains("StreamReader") || l.VariableType.FullName.Contains("BinaryReader") || l.VariableType.FullName.Contains("StreamWriter") || l.VariableType.FullName.Contains("BinaryWriter") || l.VariableType.FullName.Contains("File") || l.VariableType.FullName.Contains("Directory") || l.VariableType.FullName.Contains("Path")))) == true)
                return true;
            else if (method?.Parameters?.Any(l => l.ParameterType.FullName.Contains("System.IO") && (l.ParameterType.FullName.Contains("Drive") || l.ParameterType.FullName.Contains("StreamReader") || l.ParameterType.FullName.Contains("BinaryReader") || l.ParameterType.FullName.Contains("StreamWriter") || l.ParameterType.FullName.Contains("BinaryWriter") || l.ParameterType.FullName.Contains("File") || l.ParameterType.FullName.Contains("Directory") || l.ParameterType.FullName.Contains("Path"))) == true)
                return true;
            else return false;
        }

        public static bool HasAssemblyCommands(this MethodDefinition method)
        {
            if (method?.Body?.Variables?.Any(l => l.VariableType.FullName.Contains("System.Reflection.Assembly")) == true)
                return true;
            else if (method?.Parameters?.Any(l => l.ParameterType.FullName.Contains("System.Reflection.Assembly")) == true)
                return true;
            else return false;
        }

        public static bool HasProcessCommands(this MethodDefinition method)
        {
            if (method?.Body?.Variables?.Any(l => l.VariableType.FullName.Contains("System.Diagnostics.Process")) == true)
                return true;
            else if (method?.Parameters?.Any(l => l.ParameterType.FullName.Contains("System.Diagnostics.Process")) == true)
                return true;
            else return false;
        }
        public static bool HasXXECommands(this MethodDefinition method)
        {
            var version = Version.Parse((method?.Module?.Assembly?.CustomAttributes?.Where(x => x.AttributeType.FullName.Contains("TargetFrameworkAttribute"))?.FirstOrDefault()?.Properties?.FirstOrDefault().Argument.Value?.ToString().Split(' ').LastOrDefault() ?? "0.0") + ".0");

            if (version.CompareTo(xxeVersion) <= 0)
            {
                if (method?.Body?.Variables?.Any(l => l.VariableType.FullName.Contains("System.Xml.XmlDocument") || l.VariableType.FullName.Contains("System.Xml.XmlTextReader") || l.VariableType.FullName.Contains("System.Xml.Xpath.XPathNavigator")) == true)
                    return true;
                else if (method?.Parameters?.Any(l => l.ParameterType.FullName.Contains("System.Xml.XmlDocument") || l.ParameterType.FullName.Contains("System.Xml.XmlTextReader") || l.ParameterType.FullName.Contains("System.Xml.Xpath.XPathNavigator")) == true)
                    return true;
                else return false;
            }
            return false;
        }
    }
}
