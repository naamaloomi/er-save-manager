using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace er_save_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string erSavePath;
        private string activeSavePath = string.Empty;
        private List<Save> saves = new List<Save>();

        public MainWindow()
        {
            erSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\EldenRing";
            InitializeComponent();
            tbErSavePath.Text = erSavePath;
            UpdateSavesList();
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                tbErSavePath.Text = dialog.SelectedPath;
                erSavePath = dialog.SelectedPath;
                UpdateSavesList();
            }
        }

        private void UpdateSavesList()
        {
            var (activeSaveDir, backupSavedirs) = GetSaveDirs();
            saves = new List<Save>();
            var id = 1;

            foreach (var saveDir in backupSavedirs)
            {
                saves.Add(new Save() { Name = saveDir, Id = id });
                id++;
            }

            tbActiveSavePath.Text = activeSaveDir;
            lbSavesList.ItemsSource = saves;
            lbSavesList.Items.Refresh();
        }

        private void SelectSave(object sender, RoutedEventArgs e)
        {
            var idAsString = ((Button)sender).DataContext.ToString() ?? throw new Exception("Unknown save id");
            var selectedId = Int32.Parse(idAsString);
            DeactivateSave();
            ActivateSave(selectedId);
            UpdateSavesList();
        }

        private void NewSave(object sender, RoutedEventArgs e)
        {
            CopyDirectory(saves[0].Name, GetFirstAvailableSlot(), false);
            UpdateSavesList();
        }

        private void DeactivateSave()
        {
            var activeSave = saves[0].Name;
            var saveSlot = GetFirstAvailableSlot();
            Directory.Move(activeSave, saveSlot);
        }

        private void ActivateSave(int id)
        {
            var activeSaveName = GetBaseSaveName();
            var saveSlot = saves.First(d => d.Id == id).Name;
            Directory.Move(saveSlot, activeSaveName);
        }

        private string GetFirstAvailableSlot()
        {
            // First save is always the active one
            for (int i = 1; i < saves.Count; i++)
            {
                var saveName = saves[i].Name;
                var saveSlot = GetSaveName(i);
                if (saveSlot != saveName)
                {
                    return saveSlot;
                }
            }
            // If we can't find an available save file number the next number in line is
            // the same as the length of the array since the index starts at 0
            return GetSaveName(saves.Count);
        }

        private string GetSaveName(int i)
        {
            return GetBaseSaveName() + "-" + i.ToString();
        }

        private string GetBaseSaveName()
        {
            return saves[0].Name;
        }

        private (string, IEnumerable<string>) GetSaveDirs()
        {
            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(erSavePath);
            }
            catch (IOException)
            {
                return (string.Empty, new List<string>());
            }

            var activeSaveDir = dirs.First(d => GetDirName(d).Length == 17);
            var backupSaveDirs = dirs.Where(d => GetDirName(d).Length == 19);

            return (activeSaveDir, backupSaveDirs);
        }

        private string GetDirName(string path)
        {
            return new DirectoryInfo(path).Name;
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
