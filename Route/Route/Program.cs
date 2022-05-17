using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Route
{
    public class Program
    {
        private static Dictionary<string, string> _imports = new Dictionary<string, string>();
        private static List<string> _writtenAlready = new List<string>();
        private static List<string> _htmlFileNameList = new List<string>();
        private static int _applicationId;
        private static string _rootFolder;
        private static int _id = 100001;
        private static bool _useId;

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            _applicationId = int.Parse(args[0]);
            if (int.TryParse(args[1], out _id))
                _useId = true;
            _rootFolder = args[2];
            string[] allfiles = Directory.GetFiles(_rootFolder, "*.module.ts", SearchOption.AllDirectories);
            var sql = new StringBuilder();
            foreach (string file in allfiles)
            {
                sql.Append(ProcessAppModule(file));
            }
            Clipboard.SetText(sql.ToString());
            Console.WriteLine("SQL Copied To Clipboard");
        }

        static string ProcessAppModule(string fileName)
        {
            var sql = new StringBuilder();
            Console.WriteLine(fileName);
            foreach (string line in File.ReadLines(fileName))
            {
                if (line.StartsWith("import"))
                {
                    var file = Between(line, "'", "'");
                    if (file != null && file.StartsWith("@"))
                        continue;
                    var name = Between(line, "{", "}");
                    if (file != null && name != null && !_imports.ContainsKey(name))
                        _imports[name] = file.Replace(";", string.Empty);
                    continue;
                }
                if (line.Contains("path:"))
                {
                    var component = Between(line, "component:", "}");
                    var route = Between(line, "'", "'");

                    if (string.IsNullOrEmpty(route))
                        continue;

                    if (route != null)
                    {
                        string htmlFileName;
                        // string pageName = "";

                        if (_imports.ContainsKey(component))
                        {
                            htmlFileName = Path.GetFileName($"{_imports[component]}.html");
                        }
                        else
                        {
                            continue;
                        }

                        if (route.Contains("/:"))
                            route = route.Substring(0, route.IndexOf("/:"));

                        if (route != null && !route.Contains("*"))
                        {
                            if (!_writtenAlready.Contains(route))
                            {
                                if (_htmlFileNameList.Contains(htmlFileName))
                                    htmlFileName = $"{htmlFileName} {_id}";
                                sql.Append($"INSERT INTO [App].[Function] ([Id], [Name], [ApplicationId], [Visible], [InsertedBy], [InsertedOn], [ModifiedBy], [ModifiedOn], [RouteUrl]) VALUES ({GetId()}, '{htmlFileName}', {_applicationId}, 1, suser_name(), getdate(), null, null, '{route}')\r\n");
                                _id++;
                                _writtenAlready.Add(route);
                                _htmlFileNameList.Add(htmlFileName);
                            }
                        }
                    }
                }
            }
            return sql.ToString();
        }

        static string Between(string str, string firstString, string lastString)
        {
            int pFrom = str.IndexOf(firstString) + firstString.Length;
            int pTo = str.LastIndexOf(lastString);
            if (pTo < pFrom)
                pTo = str.Length;
            var result = str.Substring(pFrom, pTo - pFrom);
            if (result != null)
            {
                result = result.Replace("'", string.Empty).Replace("\"", string.Empty).Replace(" ", string.Empty);
            }
            else
            {
                return String.Empty;
            }
            return result;
        }

        static string GetId()
        {
            if (_useId)
                return _id.ToString();
            else
                return "(select max(id) + 1 from [App].[Function])";
        }
    }
}
