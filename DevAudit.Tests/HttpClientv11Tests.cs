﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using DevAudit.AuditLibrary;
namespace DevAudit.Tests
{
    public class HttpClientv11Tests : HttpClientTests
    {
        protected override OSSIndexHttpClient http_client { get; } = new OSSIndexHttpClient("1.1");
        protected Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform = (artifacts) =>
        {
            List<OSSIndexArtifact> o = artifacts.ToList();
            foreach (OSSIndexArtifact a in o)
            {
                if (a.Search == null || a.Search.Count() != 4)
                {
                    throw new Exception("Did not receive expected Search field properties for artifact name: " + a.PackageName + " id: " +
                        a.PackageId + " project id: " + a.ProjectId + ".");
                }
                else
                {
                    Package package = new Package(a.Search[0], a.Search[1], a.Search[3], "");
                    a.Package = package;
                }
            }
            return o;
        };

        [Fact]
        public override async Task CanSearch()
        {
            Package q1 = new Package("msi", "Adobe Reader", "11.0.10", "");
            Package q2 = new Package("msi", "Adobe Reader", "10.1.1", "");
            
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("msi", q1, transform);
            Assert.NotEmpty(r1);
            Assert.True(r1.All(r => r.Package != null && !string.IsNullOrEmpty(r.Package.Name) && !string.IsNullOrEmpty(r.Package.Version)));
            IEnumerable<OSSIndexArtifact> r2 = await http_client.SearchAsync("msi", new List<Package>() { q1, q2 }, transform);
            Assert.NotEmpty(r2);
            Assert.True(r2.All(r => r.Package != null && !string.IsNullOrEmpty(r.Package.Name) && !string.IsNullOrEmpty(r.Package.Version)));
        }

        
        public override async Task CanGetProject()
        {
            Package q1 = new Package("bower", "jquery", "1.6.1", "");
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("bower", q1, transform);
            Assert.True(r1.Count() == 1);
            OSSIndexProject p1 = await http_client.GetProjectForIdAsync(r1.First().ProjectId);
            Assert.NotNull(p1);
            Assert.Equal(p1.Name, "JQuery");
            Assert.Equal(p1.HasVulnerability, true);
            Assert.Equal(p1.Vulnerabilities, "http://ossindex.net:8080/v1.1/project/8396559329/vulnerabilities");
        }
        
        [Fact]
        public async Task CanGetPackageVulnerability()
        {
            Package q1 = new Package("nuget", "DevAudit", "", "");
            IEnumerable<OSSIndexArtifact> r1 = await http_client.SearchAsync("nuget", q1, transform);
            Assert.True(r1.Count() > 0);
            List<OSSIndexPackageVulnerability> pv = await http_client.GetPackageVulnerabilitiesAsync(r1.First().PackageId);
            Assert.True(pv.Count > 0);
        }

        [Fact]
        public override async Task CanGetVulnerabilityForId()
        {
            IEnumerable<OSSIndexProjectVulnerability> v = await http_client.GetVulnerabilitiesForIdAsync("8396797903");
            Assert.NotEmpty(v);
            Assert.Equal(v.First().ProjectId, "8396797903");
        }

    }
}
