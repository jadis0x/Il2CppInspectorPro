﻿/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using Il2CppInspector; 
using Il2CppInspector.Cpp;
using Il2CppInspector.GUI;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;
using Il2CppInspector.Reflection;
using Ookii.Dialogs.Wpf;
using Path = System.IO.Path;
using Il2CppInspector.Cpp.UnityHeaders;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string bugReportSuffixText =Environment.NewLine + Environment.NewLine
                + "If you believe this is a bug in Il2CppInspector, please use the CLI version to generate the complete output and paste it when filing a bug report."
                + " Do not send a screenshot of this error!";

        public MainWindow() {
            InitializeComponent();

            // Allow XAML to access properties in the App class
            DataContext = ((App) Application.Current);

            // Subscribe to status update events
            ((App) Application.Current).OnStatusUpdate += OnStatusUpdate;

            // Find Unity paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            txtUnityPath.Text = Utils.FindPath($@"{programFiles}\Unity\Hub\Editor\*") ?? "<not set>";
            txtUnityScriptPath.Text = Utils.FindPath($@"{programFiles}\Unity\Hub\Editor\*\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-*\ScriptAssemblies") ?? "<not set>";

            // Populate script target combo box and select IDA by default
            cboPyTarget.ItemsSource = PythonScript.GetAvailableTargets();
            cboPyTarget.SelectedItem = (cboPyTarget.ItemsSource as IEnumerable<string>).First(x => x == "IDA");
        }

        /// <summary>
        /// Update the busy indicator message
        /// </summary>
        private void OnStatusUpdate(object sender, string e) => txtBusyStatus.Dispatcher.Invoke(() => txtBusyStatus.Text = e + "...");

        /// <summary>
        /// User clicked on a link
        /// </summary>
        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo {FileName = e.Uri.ToString(), UseShellExecute = true});
        }

        /// <summary>
        /// Open Manage Plugins dialog
        /// </summary>
        private void BtnPluginOptions_Click(object sender, RoutedEventArgs e) {
            var configDlg = new PluginManagerDialog();
            configDlg.Owner = this;
            configDlg.ShowDialog();
        }

        /// <summary>
        /// Open Load Options dialog
        /// </summary>
        private void BtnLoadOptions_Click(object sender, RoutedEventArgs e) {
            var loadOptionsDlg = new LoadOptionsDialog();
            loadOptionsDlg.Owner = this;
            loadOptionsDlg.ShowDialog();
        }

        /// <summary>
        /// Select global metadata file
        /// </summary>
        private async void BtnSelectMetadataFile_OnClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                Filter = "IL2CPP global metadata file|global-metadata.dat|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true) {
                await LoadMetadataAsync(openFileDialog.FileName);
            }
        }

        // Load the metadata file
        private async Task LoadMetadataAsync(string filename) {
            var app = (App) Application.Current;

            areaBusyIndicator.Visibility = Visibility.Visible;
            grdFirstPage.Visibility = Visibility.Hidden;

            if (await app.LoadMetadataAsync(filename)) {
                // Metadata loaded successfully
                btnSelectBinaryFile.Visibility = Visibility.Visible;
                areaBusyIndicator.Visibility = Visibility.Hidden;
            }
            else {
                areaBusyIndicator.Visibility = Visibility.Hidden;
                grdFirstPage.Visibility = Visibility.Visible;
                if (app.LastException is Exception e)
                    MessageBox.Show(this, e.Message + bugReportSuffixText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Select binary file
        /// </summary>
        private async void BtnSelectBinaryFile_OnClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                Filter = "Binary executable file (*.exe;*.dll;*.so;*.bin;*.prx;*.sprx)|*.exe;*.dll;*.so;*.bin;*.prx;*.sprx|Linux process map file (*.txt)|*.txt|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true) {
                await LoadBinaryAsync(openFileDialog.FileName);
            }
        }

        // Load the binary file
        private async Task LoadBinaryAsync(string filename) {
            var app = (App) Application.Current;

            areaBusyIndicator.Visibility = Visibility.Visible;
            btnSelectBinaryFile.Visibility = Visibility.Hidden;

            // Load the binary file
            if (await app.LoadBinaryAsync(filename)) {
                // Binary loaded successfully
                areaBusyIndicator.Visibility = Visibility.Hidden;

                lstImages.ItemsSource = app.AppModels;
                lstImages.SelectedIndex = 0;
            }
            else {
                areaBusyIndicator.Visibility = Visibility.Hidden;
                btnSelectBinaryFile.Visibility = Visibility.Visible;
                if (app.LastException is Exception e)
                    MessageBox.Show(this, e.Message + bugReportSuffixText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Select APK, AAB or IPA package files
        /// </summary>
        private async void BtnSelectPackageFile_OnClick(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                Filter = "Android/iOS Application Package (*.apk;*.aab;*.ipa;*.xapk;*.zip)|*.apk;*.aab;*.ipa;*.xapk;*.zip|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true) {
                await LoadPackageAsync(openFileDialog.FileNames);
            }
        }

        // Load the package file
        private async Task LoadPackageAsync(string filename) => await LoadPackageAsync(new[] { filename });
        private async Task LoadPackageAsync(IEnumerable<string> filenames) {
            var app = (App) Application.Current;

            areaBusyIndicator.Visibility = Visibility.Visible;
            grdFirstPage.Visibility = Visibility.Hidden;

            // Load the package
            if (await app.LoadPackageAsync(filenames)) {
                // Package loaded successfully
                areaBusyIndicator.Visibility = Visibility.Hidden;

                lstImages.ItemsSource = app.AppModels;
                lstImages.SelectedIndex = 0;
            }
            else {
                areaBusyIndicator.Visibility = Visibility.Hidden;
                grdFirstPage.Visibility = Visibility.Visible;
                if (app.LastException is Exception e)
                    MessageBox.Show(this, e.Message + bugReportSuffixText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reset binary and metadata files and start again
        /// </summary>
        private void BtnBack_OnClick(object sender, RoutedEventArgs e) {
            lstImages.ItemsSource = null;
            btnSelectBinaryFile.Visibility = Visibility.Hidden;
            grdFirstPage.Visibility = Visibility.Visible;
            ((App) Application.Current).ResetLoadOptions();
        }

        /// <summary>
        /// Select Unity version from asset file
        /// </summary>
        private void BtnUnityAsset_Click(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {
                Filter = "Unity asset files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try {
                var unityVersion = UnityVersion.FromAssetFile(openFileDialog.FileName);

                var done = false;
                foreach (UnityHeaders header in cboPyUnityVersion.Items) {
                    if (header.VersionRange.Contains(unityVersion)) {
                        cboPyUnityVersion.SelectedItem = header;
                        cboCppUnityVersion.SelectedItem = header;
                        cboJsonUnityVersion.SelectedItem = header;
                        done = true;
                        break;
                    }
                }

                if (done)
                    MessageBox.Show(this, $"Selected Unity version {unityVersion}", "Unity version detected", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(this, $"Asset file Unity version {unityVersion} is not compatible with the loaded IL2CPP application", "Incompatible Unity version", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException) {
                MessageBox.Show(this, "Could not determine Unity version from this asset file", "Unity version could not be detected", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// User has selected an image
        /// </summary>
        private void LstImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Selection has been removed?
            if (((ListBox)sender).SelectedItem == null) {
                trvNamespaces.ItemsSource = null;
                return;
            }

            // Get selected image
            var model = (AppModel)((ListBox)sender).SelectedItem;

            // Get namespaces
            var namespaces = model.TypeModel.Assemblies.SelectMany(x => x.DefinedTypes).GroupBy(t => t.Namespace).Select(n => n.Key);

            // Break namespaces down into a tree
            var namespaceTree = deconstructNamespaces(namespaces);

            // Uncheck the default exclusions
            foreach (var exclusion in Constants.DefaultExcludedNamespaces) {
                var parts = exclusion.Split('.');
                CheckboxNode node = null;
                foreach (var part in parts) {
                    node = (node?.Children ?? namespaceTree).FirstOrDefault(c => c.Name == part);
                    if (node == null)
                        break;
                }
                if (node != null)
                    node.IsChecked = false;
            }

            // Populate TreeView with namespace hierarchy
            trvNamespaces.ItemsSource = namespaceTree;

            // Populate Unity version combo boxes
            var prevIdaSelection = cboPyUnityVersion.SelectedItem;
            var prevCppSelection = cboCppUnityVersion.SelectedItem;
            var prevJsonSelection = cboJsonUnityVersion.SelectedItem;
            cboPyUnityVersion.Items.Clear();
            cboCppUnityVersion.Items.Clear();
            cboJsonUnityVersion.Items.Clear();
            foreach (var version in UnityHeaders.GuessHeadersForBinary(model.Package.Binary)) {
                cboPyUnityVersion.Items.Add(version);
                cboCppUnityVersion.Items.Add(version);
                cboJsonUnityVersion.Items.Add(version);
            }

            // Restore previous selection via value equality
            if (prevIdaSelection != null) {
                cboPyUnityVersion.SelectedItem = cboPyUnityVersion.Items.Cast<UnityHeaders>().FirstOrDefault(v => v.Equals(prevIdaSelection));
                cboCppUnityVersion.SelectedItem = cboCppUnityVersion.Items.Cast<UnityHeaders>().FirstOrDefault(v => v.Equals(prevCppSelection));
                cboJsonUnityVersion.SelectedItem = cboJsonUnityVersion.Items.Cast<UnityHeaders>().FirstOrDefault(v => v.Equals(prevJsonSelection));
            }

            // Prefer latest Unity versions if there was no previous selection or it's now invalid
            if (cboPyUnityVersion.SelectedItem == null)
                cboPyUnityVersion.SelectedIndex = cboPyUnityVersion.Items.Count - 1;
            if (cboCppUnityVersion.SelectedItem == null)
                cboCppUnityVersion.SelectedIndex = cboCppUnityVersion.Items.Count - 1;
            if (cboJsonUnityVersion.SelectedItem == null)
                cboJsonUnityVersion.SelectedIndex = cboJsonUnityVersion.Items.Count - 1;
        }

        private IEnumerable<CheckboxNode> deconstructNamespaces(IEnumerable<string> input) {
            if (!input.Any())
                return null;

            var rootAndChildren = input.Select(s => string.IsNullOrEmpty(s)? "<global namespace>" : s)
                                    .GroupBy(n => n.IndexOf(".") != -1 ? n.Substring(0, n.IndexOf(".")) : n).OrderBy(g => g.Key);

            return rootAndChildren.Select(i => new CheckboxNode {Name = i.Key, IsChecked = true, Children = deconstructNamespaces(
                i.Where(s => s.IndexOf(".") != -1).Select(s => s.Substring(s.IndexOf(".") + 1))
                )}).ToList();
        }

        /// <summary>
        /// Select Unity editor path
        /// </summary>
        private void BtnUnityPath_OnClick(object sender, RoutedEventArgs e) {
            var openFolderDialog = new VistaFolderBrowserDialog();
            if (txtUnityPath.Text != "<not set>")
                openFolderDialog.SelectedPath = txtUnityPath.Text;
            else {
                openFolderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }

            openFolderDialog.Description = "Select Unity editor path";
            openFolderDialog.UseDescriptionForTitle = true;

            while (openFolderDialog.ShowDialog() == true) {
                if (ValidateUnityPath(openFolderDialog.SelectedPath)) {
                    txtUnityPath.Text = openFolderDialog.SelectedPath;
                    break;
                }
            }
        }

        /// <summary>
        /// Select Unity script assemblies path
        /// </summary>
        private void BtnUnityScriptPath_OnClick(object sender, RoutedEventArgs e) {
            var openFolderDialog = new VistaFolderBrowserDialog();
            if (txtUnityScriptPath.Text != "<not set>")
                openFolderDialog.SelectedPath = txtUnityScriptPath.Text;

            openFolderDialog.Description = "Select Unity script assemblies path";
            openFolderDialog.UseDescriptionForTitle = true;

            while (openFolderDialog.ShowDialog() == true) {
                if (ValidateUnityAssembliesPath(openFolderDialog.SelectedPath)) {
                    txtUnityScriptPath.Text = openFolderDialog.SelectedPath;
                    break;
                }
            }
        }

        private bool ValidateUnityPath(string path) {
            if (File.Exists(path + @"\Editor\Data\Managed\UnityEditor.dll"))
                return true;
            MessageBox.Show(this, "Could not find Unity installation in this folder. Ensure the 'Editor' folder is a child of the selected folder and try again.", "Unity installation not found", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        private bool ValidateUnityAssembliesPath(string path) {
            if (File.Exists(path + @"\UnityEngine.UI.dll"))
                return true;
            MessageBox.Show(this, "Could not find Unity assemblies in this folder. Ensure the selected folder contains UnityEngine.UI.dll and try again.", "Unity assemblies not found", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        /// <summary>
        /// Save extracted or decrypted files
        /// </summary>
        private async void BtnSaveMetadata_OnClick(object sender, RoutedEventArgs e) {
            var package = ((AppModel) lstImages.SelectedItem).TypeModel.Package;
            var saveFileDialog = new SaveFileDialog {
                Filter = "IL2CPP metadata files (*.dat)|*.dat|All files (*.*)|*.*",
                FileName = "global-metadata.dat",
                CheckFileExists = false,
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog() == false)
                return;

            var outPath = saveFileDialog.FileName;
            await Task.Run(() => package.SaveMetadataToFile(outPath));
        }

        private async void BtnSaveBinary_OnClick(object sender, RoutedEventArgs e) {
            var package = ((AppModel) lstImages.SelectedItem).TypeModel.Package;
            var binaryName = package.BinaryImage.DefaultFilename;

            var saveFileDialog = new SaveFileDialog {
                Filter = "All files (*.*)|*.*",
                FileName = binaryName,
                CheckFileExists = false,
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog() == false)
                return;

            var outPath = saveFileDialog.FileName;
            await Task.Run(() => package.SaveBinaryToFile(outPath));
        }

        /// <summary>
        /// Perform export
        /// </summary>
        private async void BtnExport_OnClick(object sender, RoutedEventArgs e) {
            var model = (AppModel) lstImages.SelectedItem;

            var unityPath = txtUnityPath.Text;
            var unityAssembliesPath = txtUnityScriptPath.Text;

            var sortOrder = rdoSortIndex.IsChecked == true ? "index" :
                            rdoSortName.IsChecked == true ? "name" :
                            "unknown";
            var layout =    rdoLayoutSingle.IsChecked == true? "single" :
                            rdoLayoutAssembly.IsChecked == true? "assembly" :
                            rdoLayoutNamespace.IsChecked == true? "namespace" :
                            rdoLayoutClass.IsChecked == true? "class" :
                            rdoLayoutTree.IsChecked == true? "tree" :
                            "unknown";

            switch (this) {
                // C# prototypes and Visual Studio solution
                case { rdoOutputCSharp: var r, rdoOutputSolution: var s } when r.IsChecked == true || s.IsChecked == true:

                    var createSolution = rdoOutputSolution.IsChecked == true;

                    if (createSolution) {
                        if (!ValidateUnityPath(unityPath))
                            return;
                        if (!ValidateUnityAssembliesPath(unityAssembliesPath))
                            return;
                    }

                    // Get options
                    var excludedNamespaces = constructExcludedNamespaces((IEnumerable<CheckboxNode>) trvNamespaces.ItemsSource);

                    var writer = new CSharpCodeStubs(model.TypeModel) {
                        ExcludedNamespaces = excludedNamespaces.ToList(),
                        SuppressMetadata = cbSuppressMetadata.IsChecked == true,
                        MustCompile = cbMustCompile.IsChecked == true
                    };

                    var flattenHierarchy = cbFlattenHierarchy.IsChecked == true;
                    var separateAssemblyAttributesFiles = cbSeparateAttributes.IsChecked == true;

                    // Determine if we need a filename or a folder - file for single file, folder for everything else
                    var needsFolder = rdoOutputCSharp.IsChecked == false || rdoLayoutSingle.IsChecked == false;

                    var saveFolderDialog = new VistaFolderBrowserDialog {
                        Description = "Select save location",
                        UseDescriptionForTitle = true
                    };
                    var saveFileDialog = new SaveFileDialog {
                        Filter = "C# source files (*.cs)|*.cs|All files (*.*)|*.*",
                        FileName = "types.cs",
                        CheckFileExists = false,
                        OverwritePrompt = true
                    };

                    if (needsFolder && saveFolderDialog.ShowDialog() == false)
                        return;
                    if (!needsFolder && saveFileDialog.ShowDialog() == false)
                        return;


                    txtBusyStatus.Text = createSolution ? "Creating Visual Studio solution..." : "Exporting C# type definitions...";
                    areaBusyIndicator.Visibility = Visibility.Visible;

                    var outPath = needsFolder ? saveFolderDialog.SelectedPath : saveFileDialog.FileName;

                    await Task.Run(() => {
                        if (createSolution)
                            writer.WriteSolution(outPath, unityPath, unityAssembliesPath);
                        else
                            switch (layout, sortOrder) {
                                case ("single", "index"):
                                    writer.WriteSingleFile(outPath, t => t.Index);
                                    break;
                                case ("single", "name"):
                                    writer.WriteSingleFile(outPath, t => t.Name);
                                    break;

                                case ("namespace", "index"):
                                    writer.WriteFilesByNamespace(outPath, t => t.Index, flattenHierarchy);
                                    break;
                                case ("namespace", "name"):
                                    writer.WriteFilesByNamespace(outPath, t => t.Name, flattenHierarchy);
                                    break;

                                case ("assembly", "index"):
                                    writer.WriteFilesByAssembly(outPath, t => t.Index, separateAssemblyAttributesFiles);
                                    break;
                                case ("assembly", "name"):
                                    writer.WriteFilesByAssembly(outPath, t => t.Name, separateAssemblyAttributesFiles);
                                    break;

                                case ("class", _):
                                    writer.WriteFilesByClass(outPath, flattenHierarchy);
                                    break;

                                case ("tree", _):
                                    writer.WriteFilesByClassTree(outPath, separateAssemblyAttributesFiles);
                                    break;
                            }
                    });
                    break;

                // Python script
                case { rdoOutputPy: var r } when r.IsChecked == true:

                    var pySaveFileDialog = new SaveFileDialog {
                        Filter = "Python scripts (*.py)|*.py|All files (*.*)|*.*",
                        FileName = "il2cpp.py",
                        CheckFileExists = false,
                        OverwritePrompt = true
                    };

                    if (pySaveFileDialog.ShowDialog() == false)
                        return;

                    var pyOutFile = pySaveFileDialog.FileName;

                    areaBusyIndicator.Visibility = Visibility.Visible;
                    var selectedPyUnityVersion = ((UnityHeaders) cboPyUnityVersion.SelectedItem)?.VersionRange.Min;
                    var selectedPyTarget = (string) cboPyTarget.SelectedItem;
                    await Task.Run(() => {
                        OnStatusUpdate(this, $"Building application model for Unity {selectedPyUnityVersion}/GCC");
                        model.Build(selectedPyUnityVersion, CppCompilerType.GCC);

                        OnStatusUpdate(this, $"Generating {selectedPyTarget} Python script");
                        new PythonScript(model).WriteScriptToFile(pyOutFile, selectedPyTarget);
                    });
                    break;

                // C++ scaffolding
                case { rdoOutputCpp: var r } when r.IsChecked == true:

                    var cppSaveFolderDialog = new VistaFolderBrowserDialog {
                        Description = "Select save location",
                        UseDescriptionForTitle = true
                    };

                    if (cppSaveFolderDialog.ShowDialog() == false)
                        return;

                    var cppOutPath = cppSaveFolderDialog.SelectedPath;

                    areaBusyIndicator.Visibility = Visibility.Visible;
                    var selectedCppUnityVersion = ((UnityHeaders) cboCppUnityVersion.SelectedItem)?.VersionRange.Min;
                    var cppCompiler = (CppCompilerType) Enum.Parse(typeof(CppCompilerType), cboCppCompiler.SelectionBoxItem.ToString());
                    await Task.Run(() => {
                        OnStatusUpdate(this, $"Building application model for Unity {selectedCppUnityVersion}/{cppCompiler}");
                        model.Build(selectedCppUnityVersion, cppCompiler);

                        string solutionName = "";

                        Dispatcher.Invoke(() => {
                            solutionName = txtSolutionName.Text.Length > 3 ? txtSolutionName.Text.Trim() : "il2cpp-dll";
                        });

                        OnStatusUpdate(this, "Generating C++ scaffolding");
                        new CppScaffolding(model, solutionName).Write(cppOutPath);
                    });
                    break;

                // JSON metadata
                case { rdoOutputJSON: var r } when r.IsChecked == true:

                    var jsonSaveFileDialog = new SaveFileDialog {
                        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                        FileName = "metadata.json",
                        CheckFileExists = false,
                        OverwritePrompt = true
                    };

                    if (jsonSaveFileDialog.ShowDialog() == false)
                        return;

                    var jsonOutFile = jsonSaveFileDialog.FileName;

                    areaBusyIndicator.Visibility = Visibility.Visible;
                    var selectedJsonUnityVersion = ((UnityHeaders) cboJsonUnityVersion.SelectedItem)?.VersionRange.Min;
                    await Task.Run(() => {
                        OnStatusUpdate(this, $"Building application model for Unity {selectedJsonUnityVersion}/GCC");
                        model.Build(selectedJsonUnityVersion, CppCompilerType.GCC);

                        OnStatusUpdate(this, "Generating JSON metadata file");
                        new JSONMetadata(model).Write(jsonOutFile);
                    });
                    break;

                // .NET assembly shim DLLs
                case { rdoOutputDll: var r } when r.IsChecked == true:

                    var dllSaveFolderDialog = new VistaFolderBrowserDialog {
                        Description = "Select save location",
                        UseDescriptionForTitle = true
                    };

                    if (dllSaveFolderDialog.ShowDialog() == false)
                        return;

                    var dllOutPath = dllSaveFolderDialog.SelectedPath;
                    var suppressMetadata = cbSuppressDllMetadata.IsChecked == true;

                    areaBusyIndicator.Visibility = Visibility.Visible;
                    await Task.Run(() => {
                        OnStatusUpdate(this, "Generating .NET assembly shim DLLs");
                        new AssemblyShims(model.TypeModel) {
                            SuppressMetadata = suppressMetadata
                        }
                        .Write(dllOutPath, OnStatusUpdate);
                    });
                    break;
            }

            areaBusyIndicator.Visibility = Visibility.Hidden;
            MessageBox.Show(this, "Export completed successfully", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private IEnumerable<string> constructExcludedNamespaces(IEnumerable<CheckboxNode> nodes) {
            var ns = new List<string>();

            foreach (var node in nodes) {
                if (node.IsChecked == false)
                    ns.Add(node.FullName == "<global namespace>" ? "" : node.FullName);

                else if (node.Children != null)
                    ns.AddRange(constructExcludedNamespaces(node.Children));
            }
            return ns;
        }

        /// <summary>
        /// Display startup information dialog
        /// </summary>
        private void MainWindow_OnContentRendered(object sender, EventArgs e) {
            if (User.Default.ShowDecompilerWarning) {
                var taskDialog = new TaskDialog {
                    WindowTitle = "Information",
                    MainInstruction = "Welcome to Il2CppInspector",
                    Content = "NOTE: Il2CppInspector is not a decompiler. It can provide you with the structure of an application and function addresses for every method so that you can easily jump straight to methods of interest in your disassembler. It does not attempt to recover the entire source code of the application.",
                    VerificationText = "Don't show this message again"
                };
                taskDialog.Buttons.Add(new TaskDialogButton(ButtonType.Close));
                taskDialog.ShowDialog(this);

                if (taskDialog.IsVerificationChecked) {
                    User.Default.ShowDecompilerWarning = false;
                    User.Default.Save();
                }
            }
        }

        /// <summary>
        /// Surface to handle dropped files
        /// </summary>
        private async void MainWindow_OnDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);

                if (grdFirstPage.Visibility == Visibility.Visible) {
                    // Metadata or APK/AAB/IPA
                    if (files.Length == 1) {
                        switch (files[0].ToLower()) {
                            case var s when s.EndsWith(".dat"):
                                await LoadMetadataAsync(s);
                                break;

                            case var s when s.EndsWith(".apk") || s.EndsWith(".aab") || s.EndsWith(".ipa") || s.EndsWith(".xapk") || s.EndsWith(".zip"):
                                await LoadPackageAsync(s);
                                break;
                        }
                    }
                    // Metadata and binary/text
                    else if (files.Length == 2 && (files[0].ToLower().EndsWith(".dat") || files[1].ToLower().EndsWith(".dat"))) {
                        var metadataIndex = files[0].ToLower().EndsWith(".dat") ? 0 : 1;
                        var binaryIndex = 1 - metadataIndex;

                        await LoadMetadataAsync(files[metadataIndex]);

                        // Only load binary if metadata was successful
                        if (btnSelectBinaryFile.Visibility == Visibility.Visible)
                            await LoadBinaryAsync(files[binaryIndex]);
                    }
                    // Split APK (files.Length >= 2)
                    else {
                        await LoadPackageAsync(files);
                    }
                }
                // Binary (on 2nd page)
                else if (btnSelectBinaryFile.Visibility == Visibility.Visible)
                    if (files.Length == 1)
                        await LoadBinaryAsync(files[0]);
            }
        }

        private void SelectAllNamespaces_Click(object sender, RoutedEventArgs e) {
            var tree = (IEnumerable<CheckboxNode>) trvNamespaces.ItemsSource;

            // Breadth-first selection
            for (var level = tree; level.Any(); level = level.Where(node => node.Children != null).SelectMany(node => node.Children))
                foreach (var item in level)
                    item.IsChecked = true;
        }

        private void SelectNoneNamespaces_Click(object sender, RoutedEventArgs e) {
            var tree = (IEnumerable<CheckboxNode>) trvNamespaces.ItemsSource;

            // Only need to clear the top level because this will clear all the children automatically
            foreach (var topLevelItem in tree)
                topLevelItem.IsChecked = false;
        }
    }

    // Replacement for TreeViewItem that includes checkbox state
    internal class CheckboxNode : INotifyPropertyChanged
    {
        private bool? isChecked;
        private string name;
        private IEnumerable<CheckboxNode> children;
        private CheckboxNode parent; // Only needed for ancestor checkbox validation

        public string Name {
            get => name;
            set {
                if (value == name) return;
                name = value;
                OnPropertyChanged();
            }
        }

        public string FullName => (parent != null ? parent.FullName + "." : "") + Name;

        public IEnumerable<CheckboxNode> Children {
            get => children;
            set {
                if (Equals(value, children)) return;
                children = value;

                // Set parent for each child
                foreach (var child in children)
                    child.parent = this;

                OnPropertyChanged();
            }
        }

        public bool? IsChecked {
            get => isChecked;
            set {
                if (isChecked == value) return;
                isChecked = value;
                OnPropertyChanged();

                // Uncheck all children if needed
                if (isChecked == false && Children != null)
                    foreach (var child in Children)
                        child.IsChecked = false;

                // Process ancestors
                if (isChecked == true && parent != null && parent.isChecked != true)
                    parent.IsChecked = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
