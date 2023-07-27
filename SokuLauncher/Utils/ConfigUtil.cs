﻿using Microsoft.Win32;
using Newtonsoft.Json;
using SokuLauncher.Controls;
using SokuLauncher.Models;
using SokuLauncher.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SokuLauncher.Utils
{
    public class ConfigUtil
    {
        const string CONFIG_FILE_NAME = "SokuLauncher.json";
        const string DEFAULT_SOKU_FILE_NAME = "th123.exe";
        const string DEFAULT_SOKU_DIR = ".";
        const string SOKU_FILE_NAME_REGEX = @"th123(?:[\s\w-()]+)?\.exe";
        public ConfigModel Config { get; set; } = new ConfigModel();
        public string SokuDirFullPath
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Static.SelfFileDir, $"{Config.SokuDirPath}/"));
            }
        }

        public void ReadConfig()
        {
            string configFileName = Path.Combine(Static.SelfFileDir, CONFIG_FILE_NAME);

            if (!File.Exists(configFileName))
            {
                Config = GenerateConfig();
                if (Config.SokuDirPath == null)
                {
                    Config.SokuDirPath = DEFAULT_SOKU_DIR;
                }
                if (!string.IsNullOrWhiteSpace(Config.SokuFileName))
                {
                    SaveConfig();
                }
            }
            else
            {
                var json = File.ReadAllText(configFileName);

                Config = JsonConvert.DeserializeObject<ConfigModel>(json) ?? new ConfigModel();

                // default values
                if (string.IsNullOrWhiteSpace(Config.Language))
                {
                    Config.Language = GetLanguageCode(CultureInfo.CurrentCulture.Name);
                }
                Static.LanguageService.ChangeLanguagePublish(Config.Language);

                if (!CheckSokuDirAndFileExists(Config.SokuDirPath, Config.SokuFileName))
                {
                    Config.SokuDirPath = FindSokuDir() ?? DEFAULT_SOKU_DIR;
                    Config.SokuFileName = SelectSokuFile(Config.SokuDirPath);
                    if (Config.SokuFileName != null)
                    {
                        SaveConfig();
                    }
                }
            }

            if (!CheckSokuDirAndFileExists(Config.SokuDirPath, Config.SokuFileName))
            {
                if (string.IsNullOrWhiteSpace(Config.SokuFileName))
                {
                    if (MessageBox.Show(Static.LanguageService.GetString("ConfigUtil-GameFileNotFound-Message"),
                            Static.LanguageService.GetString("ConfigUtil-GameFileNotFound-Title"),
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        string fileName = OpenExeFileDialog(SokuDirFullPath);

                        if (fileName != null)
                        {
                            string selectedFileName = Path.GetFileName(fileName);
                            string selectedDirPath = Path.GetDirectoryName(fileName);
                            string relativePath = Static.GetRelativePath(selectedDirPath, Static.SelfFileDir);
                            if (!relativePath.StartsWith("../../"))
                            {
                                selectedDirPath = relativePath;
                            }

                            Config.SokuDirPath = selectedDirPath;
                            Config.SokuFileName = selectedFileName;
                            SaveConfig();
                        }
                    }
                }
                else
                {
                    SaveConfig();
                }
            }
        }

        public void SaveConfig()
        {
            string configFileName = Path.Combine(Static.SelfFileDir, CONFIG_FILE_NAME);

            var jsonString = JsonConvert.SerializeObject(Config);
            File.WriteAllText(configFileName, jsonString);
        }

        private ConfigModel GenerateConfig()
        {
            ConfigModel config = new ConfigModel();

            config.Language = GetLanguageCode(CultureInfo.CurrentCulture.Name);

            config.SokuDirPath = FindSokuDir();

            if (config.SokuDirPath != null)
            {
                config.SokuFileName = SelectSokuFile(config.SokuDirPath);
            }

            config.SokuModSettingGroups = new List<ModSettingGroupModel>
            {
                new ModSettingGroupModel
                {
                    Id = "1d059cd2-1e74-430b-b84f-1d3ad6b67f6c",
                    Name = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-Giuroll-Name"),
                    Desc = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-Giuroll-Desc"),
                    EnableMods = new List<string> { "Giuroll", "Giuroll-60F", "SokuLobbiesMod", "Autopunch" },
                    DisableMods = new List<string> { "Giuroll-62F", "SWRSokuRoll", "InGameHostlist" },
                    Cover = "%resources%/cover1.png"
                },
                new ModSettingGroupModel
                {
                    Id = "7d9b118d-5f7a-48b0-8e35-272f0e51f0d6",
                    Name = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-GiurollCN-Name"),
                    Desc = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-GiurollCN-Desc"),
                    EnableMods = new List<string> { "Giuroll-62F", "SokuLobbiesMod", "Autopunch" },
                    DisableMods = new List<string> { "Giuroll", "Giuroll-60F", "SWRSokuRoll", "InGameHostlist" },
                    Cover = "%resources%/cover2.png"
                },
                new ModSettingGroupModel
                {
                    Id = "3b9e5e71-6044-432c-b6c3-4c53d93e137d",
                    Name = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-SokuRoll-Name"),
                    Desc = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-SokuRoll-Desc"),
                    EnableMods = new List<string> { "SWRSokuRoll", "InGameHostlist", "Autopunch" },
                    DisableMods = new List<string> { "Giuroll", "Giuroll-60F", "Giuroll-62F", "SokuLobbiesMod" },
                    Cover = "%resources%/gearbackground.png",
                    CoverOverlayColor = "#6FA92E00"
                },
                new ModSettingGroupModel
                {
                    Id = "31a56390-1f5b-4442-b4e2-7b23ce5683d7",
                    Name = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-NoRoll-Name"),
                    Desc = Static.LanguageService.GetString("ConfigUtil-DefaultSokuModSettingGroups-NoRoll-Desc"),
                    EnableMods = new List<string> { "InGameHostlist", "Autopunch" },
                    DisableMods = new List<string> { "Giuroll", "Giuroll-60F", "Giuroll-62F", "SokuLobbiesMod", "SWRSokuRoll" },
                    Cover = "%resources%/gearbackground-r.png",
                    CoverOverlayColor = "#6F002EA9"
                },
            };
            config.VersionInfoUrl = "https://soku.latte.today/version.json";
            return config;
        }

        public bool CheckSokuDirAndFileExists(string sokuDir, string sokuFileName)
        {
            string sokuDirFullPath = Path.GetFullPath(Path.Combine(Static.SelfFileDir, $"{sokuDir}/"));
            if (string.IsNullOrWhiteSpace(sokuFileName) || string.IsNullOrWhiteSpace(sokuDir) || !Directory.Exists(sokuDirFullPath) || !File.Exists(Path.Combine(sokuDirFullPath, sokuFileName)))
            {
                return false;
            }
            return true;
        }

        private string FindSokuDir()
        {
            try
            {
                List<string> directoriesToSearch = new List<string> {
                        Static.SelfFileDir,
                        Path.Combine(Static.SelfFileDir, "..") }
                    .Concat(Directory.GetDirectories(Static.SelfFileDir, "*", SearchOption.TopDirectoryOnly))
                    .ToList();

                foreach (string directory in directoriesToSearch)
                {
                    string[] exeFiles = Directory.GetFiles(directory, "*.exe");
                    foreach (string file in exeFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        if (Regex.IsMatch(fileName, SOKU_FILE_NAME_REGEX))
                        {
                            return Static.GetRelativePath(directory, Static.SelfFileDir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Static.LanguageService.GetString("Common-ErrorMessageBox-Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public static string GetLanguageCode(string cultureName)
        {
            switch (cultureName)
            {
                case "zh-TW":
                case "zh-HK":
                case "zh-MO":
                case "zh-CHT":
                case "zh-Hant":
                case "zh-Hant-TW":
                case "zh-Hant-MO":
                case "zh-Hant-HK":
                    return "zh-Hant";
                case "zh-CN":
                case "zh-SG":
                case "zh-CHS":
                case "zh-Hans":
                case "zh-Hans-CN":
                case "zh-Hans-MO":
                case "zh-Hans-HK":
                case "zh-Hans-SG":
                    return "zh-Hans";
                case "ja":
                    return "ja";
                default:
                    return "en";
            }
        }

        public static List<string> FindSokuFiles(string directory)
        {
            List<string> result = new List<string>();

            try
            {
                string[] exeFiles = Directory.GetFiles(directory, "*.exe");
                foreach (string file in exeFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (Regex.IsMatch(fileName, SOKU_FILE_NAME_REGEX))
                    {
                        result.Add(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static string SelectSokuFile(string sokuDirPath)
        {
            var SokuFileNames = FindSokuFiles(sokuDirPath);
            if (SokuFileNames.Count > 1)
            {

                SelectorWindowViewModel swvm = new SelectorWindowViewModel
                {
                    Title = Static.LanguageService.GetString("ConfigUtil-SelectSokuFileWindow-Title"),
                    Desc = Static.LanguageService.GetString("ConfigUtil-SelectSokuFileWindow-Desc"),
                    SelectorNodeList = new System.Collections.ObjectModel.ObservableCollection<SelectorNodeModel>()
                };

                foreach (string fileName in SokuFileNames)
                {
                    var bitmapSource = Static.GetExtractAssociatedIcon(Path.Combine(sokuDirPath, fileName));
                    swvm.SelectorNodeList.Add(new SelectorNodeModel
                    {
                        Title = fileName,
                        Icon = bitmapSource
                    });
                }

                (swvm.SelectorNodeList.FirstOrDefault(x => x.Title == DEFAULT_SOKU_FILE_NAME) ?? swvm.SelectorNodeList.First()).Selected = true;
                SelectSokuFileWindow SelectSokuFileWindow = new SelectSokuFileWindow(swvm);
                SelectSokuFileWindow.ShowDialog();

                return swvm.SelectorNodeList.FirstOrDefault(x => x.Selected)?.Title ?? "";
            }
            else
            {
                return SokuFileNames.FirstOrDefault();
            }
        }

        public static string OpenExeFileDialog(string sokuDirPath)
        {

            string currentSokuDir = Path.GetFullPath(Path.Combine(Static.SelfFileDir, sokuDirPath));

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = currentSokuDir;

            openFileDialog.Filter = Static.LanguageService.GetString("Common-OpenExeFileDialog-Filter");

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }
    }
}
