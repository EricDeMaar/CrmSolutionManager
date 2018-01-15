﻿using System;
using System.Xml.Serialization;

namespace SolutionManager.Logic.Configuration
{
    [Serializable]
    public class CrmCredentials
    {
        [XmlElement]
        public string OrganizationUri { get; set; }

        [XmlElement]
        public string UserName { get; set; }

        [XmlElement]
        public string Password { get; set; }

        [XmlElement]
        public string DomainName { get; set; }

        public bool HasValidUri()
        {
            if (OrganizationUri == null)
            {
                return false;
            }

            Uri ignored;
            return Uri.TryCreate(OrganizationUri, UriKind.Absolute, out ignored);
        }
    }
}