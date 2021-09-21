﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;

namespace DS4WinWPF
{
    public class ProfileEntity
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (name == value) return;
                name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler NameChanged;
        public event EventHandler ProfileSaved;
        public event EventHandler ProfileDeleted;

        public void DeleteFile()
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                string filepath = DS4Windows.Global.RuntimeAppDataPath + @"\Profiles\" + name + ".xml";
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    ProfileDeleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void SaveProfile(int deviceNum)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Global.Instance.Config.SaveProfile(deviceNum, name);
                Global.Instance.Config.CacheExtraProfileInfo(deviceNum);
            }
        }

        public void FireSaved()
        {
            ProfileSaved?.Invoke(this, EventArgs.Empty);
        }

        public void RenameProfile(string newProfileName)
        {
            string oldFilePath = Path.Combine(DS4Windows.Global.RuntimeAppDataPath,
                "Profiles", $"{name}.xml");

            string newFilePath = Path.Combine(DS4Windows.Global.RuntimeAppDataPath,
                "Profiles", $"{newProfileName}.xml");

            if (File.Exists(oldFilePath) && !File.Exists(newFilePath))
            {
                File.Move(oldFilePath, newFilePath);
                // Send NameChanged event so controls get updated with new name
                Name = newProfileName;
            }
        }
    }
}
