﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    public class DisconnectBTViewModel : NotifyDataErrorBase
    {
        private double holdInterval;
        public double HoldInterval { get => holdInterval; set => holdInterval = value; }

        public void LoadAction(SpecialAction action)
        {
            holdInterval = action.DelayTime;
        }

        public void SaveAction(SpecialAction action, bool edit = false)
        {
            Global.Instance.SaveAction(action.Name, action.Controls, 5, $"{holdInterval.ToString("#.##", Global.ConfigFileDecimalCulture)}", edit);
        }

        public override bool IsValid(SpecialAction action)
        {
            ClearOldErrors();

            bool valid = true;
            List<string> holdIntervalErrors = new List<string>();

            if (holdInterval < 0 || holdInterval > 60)
            {
                holdIntervalErrors.Add("Interval not valid");
                errors["HoldInterval"] = holdIntervalErrors;
                RaiseErrorsChanged("HoldInterval");
            }

            return valid;
        }

        public override void ClearOldErrors()
        {
            if (errors.Count > 0)
            {
                errors.Clear();
                RaiseErrorsChanged("HoldInterval");
            }
        }
    }
}
