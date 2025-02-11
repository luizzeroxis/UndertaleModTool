using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Win32;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class MainWindow
    {
        public string ProjectPath { get; set; } = null;

        async Task<ProjectMetadata> LoadProjectMetadata(string projectPath)
        {
            try
            {
                return JsonSerializer.Deserialize<ProjectMetadata>(await File.ReadAllTextAsync(Path.Join(projectPath, "project.json")));
            }
            catch (Exception ex)
            {
                this.ShowError($"Error when reading project.json: {ex.Message}");
                return null;
            }
        }

        async void OpenProject()
        {
            // Select folder
            OpenFolderDialog dlg = new();
            dlg.Title = "Select a directory containing an UndertaleModTool project";

            if (dlg.ShowDialog(this) == false)
                return;
            string projectPath = dlg.FolderName;

            // Load project metadata file
            ProjectMetadata projectMetadata = await LoadProjectMetadata(projectPath);
            if (projectMetadata == null)
                return;

            // Load data.win
            string dataPath = Path.Join(projectPath, projectMetadata.DataFileName);
            await LoadFile(dataPath, true);

            // Load Code folder
            string codePath = Path.Join(projectPath, "Code");

            foreach (UndertaleCode code in Data.Code)
            {
                if (code is not null && code.ParentEntry == null)
                {
                    string codeGMLPath = Path.Join(codePath, code.Name.Content + ".gml");
                    if (File.Exists(codeGMLPath))
                        code.GML = await File.ReadAllTextAsync(codeGMLPath);
                }
            }

            ProjectPath = projectPath;
        }

        public async void SaveProject(bool saveAs)
        {
            // If save as, always creating new folder. If not, then override folder only if there is a project already.
            bool overridingExisting = saveAs ? false : (ProjectPath != null);
            string projectPath = ProjectPath;

            // From Command_Save:
            if (CanSave)
            {
                if (!CanSafelySave)
                    this.ShowWarning("Errors occurred during loading. High chance of data loss! Proceed at your own risk.");

                var result = await SaveCodeChanges();
                if (result == SaveResult.Error)
                {
                    this.ShowError("The changes in code editor weren't saved due to some error in \"SaveCodeChanges()\".");
                    return;
                }
            }

            if (!overridingExisting)
            {
                while (true)
                {
                    // Select folder
                    OpenFolderDialog dlg = new();
                    dlg.Title = "Select an empty directory for the UndertaleModTool project to be saved in";

                    if (dlg.ShowDialog(this) == false) return;
                    projectPath = dlg.FolderName;

                    // Check if folder is empty
                    if (Directory.EnumerateFileSystemEntries(projectPath).Any())
                        this.ShowError($"Directory {projectPath} is not empty");
                    else
                        break;
                }
            }

            string dataPathPrevious = null;
            string dataPathPreviousBackup = null;

            if (overridingExisting)
            {
                ProjectMetadata projectMetadataPrevious = await LoadProjectMetadata(projectPath);
                if (projectMetadataPrevious == null)
                    return;

                // Backup previous data file
                dataPathPrevious = Path.Join(projectPath, projectMetadataPrevious.DataFileName);
                dataPathPreviousBackup = dataPathPrevious + "bak";

                if (File.Exists(dataPathPreviousBackup))
                {
                    this.ShowError($"File {dataPathPreviousBackup} already exists");
                    return;
                }

                File.Move(dataPathPrevious, dataPathPreviousBackup);
            }

            ProjectMetadata projectMetadata = new();
            projectMetadata.DataFileName = Path.GetFileName(FilePath);

            // Save data file
            string dataPath = Path.Join(projectPath, projectMetadata.DataFileName);
            if (!await SaveFile(dataPath))
            {
                if (overridingExisting)
                {
                    // If overriding, recover backup of data file
                    File.Move(dataPathPreviousBackup, dataPathPrevious);
                }
                return;
            }

            if (overridingExisting)
            {
                // Delete backup of previous data file
                if (File.Exists(dataPathPreviousBackup))
                    File.Delete(dataPathPreviousBackup);
            }

            string codePath = Path.Join(projectPath, "Code");

            // TODO: make this safer
            // Delete Code folder
            if (overridingExisting)
            {
                if (Directory.Exists(codePath))
                    Directory.Delete(codePath, true);
            }

            // Create Code folder
            Directory.CreateDirectory(codePath);

            foreach (UndertaleCode code in Data.Code)
            {
                if (code is not null && code.ParentEntry == null && code.GML != null)
                    await File.WriteAllTextAsync(Path.Join(codePath, code.Name.Content + ".gml"), code.GML);
            }

            // Save project metadata file
            string projectMetaDataPath = Path.Join(projectPath, "project.json");
            await File.WriteAllTextAsync(projectMetaDataPath, JsonSerializer.Serialize(projectMetadata));

            ProjectPath = projectPath;
        }

        public class ProjectMetadata
        {
            public string DataFileName { get; set; } = "data.win";
        }
    }
}
