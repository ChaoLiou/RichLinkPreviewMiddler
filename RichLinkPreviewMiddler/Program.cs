using System;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.IO;
using Renci.SshNet;

namespace RichLinkPreviewMiddler
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootDirectory = Directory.GetCurrentDirectory();
            var structureJSON = Path.Combine(rootDirectory, "structure.json");
            var outputDirectory = Path.Combine(rootDirectory, "output");
            var templateTXT = Path.Combine(rootDirectory, "template.txt");

            var info = deserializeTo<RLPMInfo>(structureJSON);
            if (info != null)
            {
                generateHTML(info, templateTXT, outputDirectory);

                if (args.Any() && args.Length == 3)
                {
                    mirrorToServer(host: args[0], username: args[1], password: args[2], outputDirectory: outputDirectory);
                }
            }
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
                var content = string.Format(
                    templateContent, 
                    msg.title, 
                    msg.description, 
                    imgRelativePath, 
                    formatLink(info.config.host, msg.link));
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

        private static void mirrorToServer(string host, string username, string password, string outputDirectory)
        {
            var destinationRoot = "/var/www/html/probis/rlpm/";
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
