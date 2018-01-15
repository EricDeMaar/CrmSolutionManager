using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SolutionManager.App.Helpers
{
    public static class SolutionHelper
    {
        public static SolutionData ReadVersionFromZip(string solutionName)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var solution = Path.Combine(baseDirectory, @"Solutions\", solutionName);

            using (var zipfile = new ZipArchive(File.OpenRead(solution), ZipArchiveMode.Read))
            {
                var solutionInfo = zipfile.Entries.First(x => x.Name == "solution.xml");

                using (var dataStream = solutionInfo.Open())
                {
                    var xml = XElement.Load(new StreamReader(dataStream)).CreateReader();

                    var data = new XmlSerializer(typeof(SolutionXml.ImportExportXml)).Deserialize(xml) as SolutionXml.ImportExportXml;

                    return new SolutionData()
                    {
                        UniqueName = data.SolutionManifest.UniqueName,
                        Version = data.SolutionManifest.Version,
                    };
                }
            }
        }
    }
}
