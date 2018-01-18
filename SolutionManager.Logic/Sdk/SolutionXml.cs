using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SolutionManager.Logic.Sdk
{
    public class SolutionXml
    {
        [Serializable]
        [XmlRoot("ImportExportXml")]
        public class ImportExportXml
        {
            [XmlElement("SolutionManifest")]
            public SolutionManifest SolutionManifest { get; set; }
        }

        [Serializable]
        [XmlRoot("SolutionManifest")]
        [XmlType(AnonymousType = true)]
        public class SolutionManifest
        {
            [XmlElement("UniqueName")]
            public string UniqueName { get; set; }

            [XmlElement("Managed")]
            public int Managed { get; set; }

            // Public get / set for XmlSerializer.
            [XmlElement("Version")]
            public string VersionAsString { get; set; }

            public Version Version
            {
                get
                {
                    return Version.Parse(VersionAsString);
                }
            }
        }
    }
}
