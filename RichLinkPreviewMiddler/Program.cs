using System;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.IO;
using Renci.SshNet;
using System.Text;
using System.Reflection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RichLinkPreviewMiddler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("\rInitializing...");
            var rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var structureJSON = Path.Combine(rootDirectory, "structure.json");
            var outputDirectory = Path.Combine(rootDirectory, "output");
            var templateTXT = Path.Combine(rootDirectory, "template.txt");
            var destinationOnServer = "/var/www/html/probis/rlpm/";

            consoleWriteWithColor("\rInitializing => Success", ConsoleColor.DarkGreen);
            consoleLog(new
            {
                DirectoryRoot = rootDirectory,
                JSONPath = "{DirectoryRoot}" + structureJSON.Replace(rootDirectory, string.Empty),
                OutputDirectory = "{DirectoryRoot}" + outputDirectory.Replace(rootDirectory, string.Empty),
                TemplatePath = "{DirectoryRoot}" + templateTXT.Replace(rootDirectory, string.Empty),
                DestinationOnServer = destinationOnServer
            });

            Console.Write("\r1. Deserializing the Content of {JSONPath}...");
            RLPMInfo info = null;

            try
            {
                info = deserializeTo<RLPMInfo>(structureJSON);
            }
            catch (Exception e)
            {
                consoleWriteWithColor("\r2. Deserializing the Content of {JSONPath} => Fail", ConsoleColor.DarkRed);
                consoleLog(e);
            }

            if (info != null)
            {
                consoleWriteWithColor("\r1. Deserializing the Content of {JSONPath} => Success", ConsoleColor.DarkGreen);
                consoleLog(new
                {
                    Count = info.messages.Count,
                    TemplatePath = "{TemplatePath}",
                    OutputDirectory = "{OutputDirectory}"
                });
                Console.Write("\r2. Generating HTML To {OutputDirectory}...");
                generateHTML(info, templateTXT, outputDirectory);
                consoleWriteWithColor("\r2. Generating HTML To {OutputDirectory} => Success", ConsoleColor.DarkGreen);

                if (args.Any() && args.Length == 3)
                {
                    consoleLog(new
                    {
                        Host = args[0],
                        UserName = args[1],
                        Password = args[2],
                        Source = "{OutputDirectory}",
                        Destination = "{DestinationOnServer}"
                    });
                    Console.Write("\r3. Mirroring to Server on {Host} w/ {UserName}/{Password}...");
                    mirrorToServer(host: args[0], username: args[1], password: args[2], outputDirectory: outputDirectory, destinationRoot: destinationOnServer);
                    consoleWriteWithColor("\r3. Mirroring to Server on {Host} w/ {UserName}/{Password} => Success", ConsoleColor.DarkGreen);
                }
            }

            Console.Read();
        }

        private static void consoleWriteWithColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void consoleLog(object obj)
        {
            Console.WriteLine(obj.ToOutput());
        }

        private static void generateHTML(RLPMInfo info, string templateTXT, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            foreach (var msg in info.messages)
            {
                var imgRelativePath = Path.Combine(info.config.imgroot, msg.image);
                var templateContent = File.ReadAllText(templateTXT);
                var link = formatLink(info.config.host, msg.link);
                var content = string.Format(
                    templateContent,
                    msg.title,
                    msg.description,
                    imgRelativePath,
                    link,
                    string.IsNullOrWhiteSpace(msg.script) ? ("location.href ='" + link + "';") : msg.script);
                File.WriteAllText(Path.Combine(outputDirectory, msg.filename), content);
            }
        }

        private static T deserializeTo<T>(string jsonpath)
        {
            if (File.Exists(jsonpath))
            {
                using (var fs = new FileStream(jsonpath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var json = File.ReadAllText(jsonpath);
                    var js = new DataContractJsonSerializer(typeof(T));
                    return (T)js.ReadObject(fs);
                }
            }
            else
            {
                return default(T);
            }
        }

        private static void mirrorToServer(string host, string username, string password, string outputDirectory, string destinationRoot)
        {
            using (var client = new SftpClient(host, username, password))
            {
                client.Connect();
                foreach (var file in Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories))
                {
                    var destination = destinationRoot + file.Replace(outputDirectory + "\\", string.Empty).Replace("\\", "/");
                    ensureDirectoryExists(client, destination);

                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        client.UploadFile(fs, destination, true);
                    }
                }
            }
        }

        private static void ensureDirectoryExists(SftpClient client, string destination)
        {
            var directory = "/";
            foreach (var splitPart in destination.Split('/').Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains(".")))
            {
                directory = Path.Combine(directory, splitPart).Replace("\\", "/");
                if (!client.Exists(directory))
                {
                    client.CreateDirectory(directory);
                }
            }
        }

        private static string formatLink(string @base, string relative)
        {
            return relative.StartsWith("http") ? relative : (new Uri(new Uri(@base), relative)).AbsoluteUri;
        }
    }
}
