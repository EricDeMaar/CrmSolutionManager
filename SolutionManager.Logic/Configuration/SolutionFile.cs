using System;
using System.Xml.Serialization;

namespace SolutionManager.Logic.Configuration
{
    [Serializable]
    [XmlType(AnonymousType = true)]
    public class SolutionFile
    {
        [XmlAttribute("name")]
        public string FileName { get; set; }

        [XmlAttribute("uniqueName")]
        public string UniqueName { get; set; }

        [XmlAttribute("continueOnError")]
        public bool ContinueOnError { get; set; }

        [XmlAttribute("action")]
        public ActionType Action { get; set; }

        [XmlAttribute("writeToZipFile")]
        public string WriteToZipFile { get; set; }

        [XmlAttribute("exportAsManaged")]
        public bool ExportAsManaged { get; set; }

        [XmlElement("CrmCredentials")]
        public CrmCredentials CrmCredentials { get; set; }

        [XmlElement("ImportSettings")]
        public SolutionImportSettings ImportSettings { get; set; }

        public bool Validate()
        {
            if (this.CrmCredentials == null)
            {
                Console.WriteLine("Credentials were not specified for solution.");

                return false;
            }

            if (!this.CrmCredentials.HasValidUri())
            {
                Console.WriteLine("No valid uri was given for solution.");

                return false;
            }

            if ((this.Action == ActionType.Delete || this.Action == ActionType.Export) && this.UniqueName == null)
                throw new Exception("UniqueName cannot be empty when ActionType = Delete OR ActionType = Export.");

            if (this.Action == ActionType.Import && ImportSettings == null)
                throw new Exception("ImportSettings cannot be empty when ActionType = Import");

            return true;
        }
    }

    public enum ActionType
    {
        Delete,
        Import,
        Export
    }
}