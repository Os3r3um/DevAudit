﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;
using Devsense.PHP.Syntax;
namespace DevAudit.AuditLibrary
{
    public abstract class PHPCodeProject : CodeProject
    {
        #region Constructors
        public PHPCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler) : 
            base(project_options, message_handler, new Dictionary<string, string[]>(), "PHP")
        { }
        public PHPCodeProject(Dictionary<string, object> project_options, EventHandler<EnvironmentEventArgs> message_handler, string analyzer_type) : 
            base(project_options, message_handler, new Dictionary<string, string[]>(), analyzer_type)
        { }
        #endregion
      
        #region Protected overriden methods
        protected override async Task GetWorkspaceAsync()
        {
            await base.GetWorkspaceAsync();
            object psu_lock = new object();
            this.HostEnvironment.Status("Parsing PHP source files.");
            Stopwatch sw = new Stopwatch();
            sw.Start();            
            DirectoryInfo wd = this.WorkspaceDirectory.GetAsSysDirectoryInfo();
            List<FileInfo> PHPFiles = wd.GetFiles("*.php*", SearchOption.AllDirectories).Concat(wd.GetFiles("*.module", SearchOption.AllDirectories).Concat(wd.GetFiles("*.inc", SearchOption.AllDirectories))).ToList();
            this.YamlFiles = wd.GetFiles("*.yml", SearchOption.AllDirectories).ToList();
            Dictionary<FileInfo, PHPAuditSourceUnit> PHPSourceUnits = new Dictionary<FileInfo, PHPAuditSourceUnit>(PHPFiles.Count);
            Parallel.ForEach(PHPFiles, (f) =>
            {
                try
                {
                    PHPAuditSourceUnit su = new PHPAuditSourceUnit(this.HostEnvironment, File.ReadAllText(f.FullName, Encoding.UTF8), f);
                    if (su != null)
                    {
                        lock (psu_lock)
                        {
                            PHPSourceUnits.Add(f, su);
                        }
                    }
                    else
                    {
                        this.HostEnvironment.Warning("Could not parse PHP file {0}.", f.FullName);
                    }
                }
                catch (IOException ioe)
                {
                    this.HostEnvironment.Error("I/O exception thrown attempting to read PHP file {0}.", f.FullName);
                    this.HostEnvironment.Error(ioe);
                    throw ioe;
                }
                catch (Exception e)
                {
                    this.HostEnvironment.Error("Exception thrown attempting to read PHP file {0}.", f.FullName);
                    this.HostEnvironment.Error(e);
                    throw e;
                }
            });
            this.WorkSpace = new Dictionary<string, List<FileInfo>>(3)
            {
                {"PHP", PHPFiles },
                {"YAML", YamlFiles },
                {"JSON", wd.GetFiles("*.json", SearchOption.AllDirectories).ToList() }

            };
            this.Project = PHPSourceUnits;
            sw.Stop();
            this.HostEnvironment.Success("Parsed {0} PHP files in {1} ms.", PHPSourceUnits.Count(), sw.ElapsedMilliseconds);
        }
        #endregion

        #region Public properties
        public List<FileInfo> YamlFiles { get; protected set; }
        #endregion

    }
}
