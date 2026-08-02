using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Timer = System.Timers.Timer;

namespace CameraControl.ViewModel
{
    public class BracketingViewModel : ViewModelBase
    {
        private ICameraDevice _camera;
        private ObservableCollection<string> _expLowList;
        private ObservableCollection<string> _expHighList;
        private ObservableCollection<string> _isoLowList;
        private ObservableCollection<string> _isoHighList;
        private ObservableCollection<string> _fLowList;
        private ObservableCollection<string> _fHighList;

        private string _error;
        private string _message;
        private Timer _timer = new Timer(100);
        private bool _isBusy;
        private string _curValue;

        public RelayCommand StartCommand { get; set; }
        public RelayCommand StopCommand { get; set; }

        public BracketingClass BracketingSettings
        {
            get { return ServiceProvider.Settings.DefaultSession.Braketing; }

        }

        public string Error
        {
            get { return _error; }
            set
            {
                _error = value;
                RaisePropertyChanged(() => Error);
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                RaisePropertyChanged(() => Message);
            }
        }

        public ICameraDevice Camera
        {
            get
            {
                if (_camera == null)
                    return ServiceProvider.DeviceManager.SelectedCameraDevice;
                return _camera;
            }
            set
            {
                _camera = value;
                RaisePropertyChanged(() => Camera);
            }
        }


        private ObservableCollection<string> _shutterLowList;
        private ObservableCollection<string> _shutterHighList;

        public int Mode
        {
            get { return BracketingSettings.Mode; }
            set
            {
                BracketingSettings.Mode = value;
                RaisePropertyChanged(() => Mode);
                RaisePropertyChanged(() => ExpVisibility);
                RaisePropertyChanged(() => ShutterVisibility);
                RaisePropertyChanged(() => FVisibility);
                RaisePropertyChanged(() => IsoVisibility);
                SetMessage();
            }
        }

        public Visibility ExpVisibility
        {
            get { return Mode == 0 ? Visibility.Visible : Visibility.Hidden; }
        }

        public Visibility ShutterVisibility
        {
            get { return Mode == 1 ? Visibility.Visible : Visibility.Hidden; }
        }

        public Visibility FVisibility
        {
            get { return Mode == 2 ? Visibility.Visible : Visibility.Hidden; }
        }

        public Visibility IsoVisibility
        {
            get { return Mode == 3 ? Visibility.Visible : Visibility.Hidden; }
        }

        #region Exp (Mode 0)
        public ObservableCollection<string> ExpLowList
        {
            get { return Camera.ExposureCompensation.Values; }
            set { _expLowList = value; }
        }

        public ObservableCollection<string> ExpHighList
        {
            get
            {
                if (_expHighList == null)
                    return Camera.ExposureCompensation.Values;
                return _expHighList;
            }
            set
            {
                _expHighList = value;
                RaisePropertyChanged(() => ExpHighList);
            }
        }

        public string ExpLow
        {
            get { return BracketingSettings.ExpLow; }
            set
            {
                BracketingSettings.ExpLow = value;
                RaisePropertyChanged(() => ExpLow);
                SetMessage();
            }
        }

        public string ExpHigh
        {
            get { return BracketingSettings.ExpHigh; }
            set
            {
                BracketingSettings.ExpHigh = value;
                RaisePropertyChanged(() => ExpHigh);
                SetMessage();
            }
        }

        public int ExpCaptureCount
        {
            get { return BracketingSettings.ExpCaptureCount; }
            set
            {
                BracketingSettings.ExpCaptureCount = value;
                RaisePropertyChanged(() => ExpCaptureCount);
                SetMessage();
            }
        }
        #endregion

        #region Shutter (Mode 1 - Manual Exposure Bracketing)
        public ObservableCollection<string> ShutterLowList
        {
            get { return Camera.ShutterSpeed.Values; }
            set { _shutterLowList = value; }
        }

        public ObservableCollection<string> ShutterHighList
        {
            get
            {
                if (_shutterHighList == null)
                    return Camera.ShutterSpeed.Values;
                return _shutterHighList;
            }
            set
            {
                _shutterHighList = value;
                RaisePropertyChanged(() => ShutterHighList);
            }
        }

        public string ShutterLow
        {
            get { return BracketingSettings.ExpLow; }
            set
            {
                BracketingSettings.ExpLow = value;
                RaisePropertyChanged(() => ShutterLow);
                SetMessage();
            }
        }

        public string ShutterHigh
        {
            get { return BracketingSettings.ExpHigh; }
            set
            {
                BracketingSettings.ExpHigh = value;
                RaisePropertyChanged(() => ShutterHigh);
                SetMessage();
            }
        }

        public int ShutterCaptureCount
        {
            get { return BracketingSettings.ExpCaptureCount; }
            set
            {
                BracketingSettings.ExpCaptureCount = value;
                RaisePropertyChanged(() => ShutterCaptureCount);
                SetMessage();
            }
        }
        #endregion

        #region F (Mode 2)
        public ObservableCollection<string> FLowList
        {
            get { return _fLowList ?? (_fLowList = Camera.FNumber.Values); }
            set { _fLowList = value; }
        }

        public ObservableCollection<string> FHighList
        {
            get
            {
                if (_fHighList == null)
                    return Camera.FNumber.Values;
                return _fHighList;
            }
            set
            {
                _fHighList = value;
                RaisePropertyChanged(() => FHighList);
            }
        }

        public string FLow
        {
            get { return BracketingSettings.FLow; }
            set
            {
                BracketingSettings.FLow = value;
                RaisePropertyChanged(() => FLow);
                SetMessage();
            }
        }

        public string FHigh
        {
            get { return BracketingSettings.FHigh; }
            set
            {
                BracketingSettings.FHigh = value;
                RaisePropertyChanged(() => FHigh);
                SetMessage();
            }
        }

        public int FCaptureCount
        {
            get { return BracketingSettings.FCaptureCount; }
            set
            {
                BracketingSettings.FCaptureCount = value;
                RaisePropertyChanged(() => FCaptureCount);
                SetMessage();
            }
        }
        #endregion

        #region ISO (Mode 3)
        public ObservableCollection<string> IsoLowList
        {
            get { return Camera.IsoNumber.Values; }
            set { _isoLowList = value; }
        }

        public ObservableCollection<string> IsoHighList
        {
            get
            {
                if (_isoHighList == null)
                    return Camera.IsoNumber.Values;
                return _isoHighList;
            }
            set
            {
                _isoHighList = value;
                RaisePropertyChanged(() => IsoHighList);
            }
        }

        public string IsoLow
        {
            get { return BracketingSettings.IsoLow; }
            set
            {
                BracketingSettings.IsoLow = value;
                RaisePropertyChanged(() => IsoLow);
                SetMessage();
            }
        }

        public string IsoHigh
        {
            get { return BracketingSettings.IsoHigh; }
            set
            {
                BracketingSettings.IsoHigh = value;
                RaisePropertyChanged(() => IsoHigh);
                SetMessage();
            }
        }

        public int IsoCaptureCount
        {
            get { return BracketingSettings.IsoCaptureCount; }
            set
            {
                BracketingSettings.IsoCaptureCount = value;
                RaisePropertyChanged(() => IsoCaptureCount);
                SetMessage();
            }
        }
        #endregion

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChanged(() => IsBusy);
                RaisePropertyChanged(() => IsFree);
            }
        }

        public bool IsFree
        {
            get { return !IsBusy; }
        }

        public string CurValue
        {
            get { return _curValue; }
            set
            {
                _curValue = value;
                RaisePropertyChanged(() => CurValue);
            }
        }

        public int Counter { get; set; }
        public List<string> Values { get; set; }
        public string DefValue { get; set; }

        public BracketingViewModel()
        {
            _timer.Elapsed += _timer_Elapsed;
            if (!IsInDesignMode)
                SetMessage();
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Camera == null)
                return;

            try
            {
                _timer.Stop();

                int waitCount = 0;
                while (Camera.IsBusy && waitCount < 60)
                {
                    Thread.Sleep(50);
                    waitCount++;
                }

                Thread.Sleep(200);

                string targetVal = Values[Counter];

                bool setSuccess = false;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    switch (Mode)
                    {
                        case 0:
                            Camera.ExposureCompensation.Value = targetVal;
                            CurValue = targetVal;
                            waitCount = 0;
                            while (Camera.ExposureCompensation.Value != targetVal && waitCount < 40)
                            {
                                Thread.Sleep(100);
                                waitCount++;
                            }
                            if (Camera.ExposureCompensation.Value == targetVal) setSuccess = true;
                            break;
                        case 1:
                            Camera.ShutterSpeed.Value = targetVal;
                            CurValue = targetVal;
                            waitCount = 0;
                            while (Camera.ShutterSpeed.Value != targetVal && waitCount < 40)
                            {
                                Thread.Sleep(100);
                                waitCount++;
                            }
                            if (Camera.ShutterSpeed.Value == targetVal) setSuccess = true;
                            break;
                        case 2:
                            Camera.FNumber.Value = targetVal;
                            CurValue = targetVal;
                            waitCount = 0;
                            while (Camera.FNumber.Value != targetVal && waitCount < 40)
                            {
                                Thread.Sleep(100);
                                waitCount++;
                            }
                            if (Camera.FNumber.Value == targetVal) setSuccess = true;
                            break;
                        case 3:
                            Camera.IsoNumber.Value = targetVal;
                            CurValue = targetVal;
                            waitCount = 0;
                            while (Camera.IsoNumber.Value != targetVal && waitCount < 40)
                            {
                                Thread.Sleep(100);
                                waitCount++;
                            }
                            if (Camera.IsoNumber.Value == targetVal) setSuccess = true;
                            break;
                    }
                    if (setSuccess) break;
                    Thread.Sleep(300);
                }

                Thread.Sleep(300);

                waitCount = 0;
                while (Camera.IsBusy && waitCount < 40)
                {
                    Thread.Sleep(50);
                    waitCount++;
                }

                CameraHelper.Capture(Camera);
                Counter++;

                if (Counter >= Values.Count)
                {
                    Stop();
                    return;
                }

                Thread.Sleep(500);
                _timer.Start();
            }
            catch (Exception ex)
            {
                StaticHelper.Instance.SystemMessage = ex.Message;
                Stop();
            }
        }

        public void Start()
        {
            try
            {
                Error = "";
                ServiceProvider.WindowsManager.ExecuteCommand(CmdConsts.NextSeries);
                switch (Mode)
                {
                    case 0:
                        if (Camera.Mode.Value == "M")
                        {
                            Error = "In M mode, use Manual Exposure Bracketing (Shutter Speed) for Canon cameras.";
                            return;
                        }
                        DefValue = Camera.ExposureCompensation.Value;
                        break;
                    case 1:
                        if (!Camera.ShutterSpeed.IsEnabled)
                        {
                            Error = TranslationStrings.LabelWrongValue;
                            return;
                        }
                        DefValue = Camera.ShutterSpeed.Value;
                        break;
                    case 2:
                        if (!Camera.FNumber.IsEnabled)
                        {
                            Error = TranslationStrings.LabelWrongFNumber;
                            return;
                        }
                        DefValue = Camera.FNumber.Value;
                        break;
                    case 3:
                        if (Camera.Mode.Value != "M")
                        {
                            Error = TranslationStrings.LabelBracketingMMode;
                        }
                        DefValue = Camera.IsoNumber.Value;
                        break;
                }
                Counter = 0;
                IsBusy = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Log.Error("Unable to start bracketing ", ex);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            CurValue = "";
            int waitCount = 0;
            while (Camera != null && Camera.IsBusy && waitCount < 40)
            {
                Thread.Sleep(100);
                waitCount++;
            }
            Thread.Sleep(1000);
            try
            {
                switch (Mode)
                {
                    case 0:
                        Camera.ExposureCompensation.Value = DefValue;
                        break;
                    case 1:
                        Camera.ShutterSpeed.Value = DefValue;
                        break;
                    case 2:
                        Camera.FNumber.Value = DefValue;
                        break;
                    case 3:
                        Camera.IsoNumber.Value = DefValue;
                        break;
                }
            }
            catch (Exception)
            {
            }
            IsBusy = false;
        }

        public void SetMessage()
        {
            Error = "";
            Message = "";
            switch (Mode)
            {
                case 0:
                    {
                        var vals = GetValues(ExpLowList != null ? ExpLowList.ToList() : null, ExpLow, ExpHigh, ExpCaptureCount);
                        if (vals == null || vals.Count == 0)
                            return;
                        Values = vals;
                        foreach (var val in vals)
                        {
                            Message += (val + ", ");
                        }
                    }
                    break;
                case 1:
                    {
                        var vals = GetValues(ShutterLowList != null ? ShutterLowList.ToList() : null, ShutterLow, ShutterHigh, ShutterCaptureCount);
                        if (vals == null || vals.Count == 0)
                            return;
                        Values = vals;
                        foreach (var val in vals)
                        {
                            Message += (val + ", ");
                        }
                    }
                    break;
                case 2:
                    {
                        var vals = GetValues(FLowList != null ? FLowList.ToList() : null, FLow, FHigh, FCaptureCount);
                        if (vals == null || vals.Count == 0)
                            return;
                        Values = vals;
                        foreach (var val in vals)
                        {
                            Message += (val + ", ");
                        }
                    }
                    break;
                case 3:
                    {
                        var vals = GetValues(IsoLowList != null ? IsoLowList.ToList() : null, IsoLow, IsoHigh, IsoCaptureCount);
                        if (vals == null || vals.Count == 0)
                            return;
                        Values = vals;
                        foreach (var val in vals)
                        {
                            Message += (val + ", ");
                        }
                    }
                    break;
            }
        }

        public List<string> GetValues(IList<string> vals, string low, string high, int count)
        {
            var res = new List<string>();
            if (vals == null || vals.Count == 0)
            {
                Error = TranslationStrings.LabelWrongValue;
                return null;
            }
            if (string.IsNullOrEmpty(low))
            {
                Error = TranslationStrings.LabelNoLowValueError;
                return null;
            }
            if (string.IsNullOrEmpty(high))
            {
                Error = TranslationStrings.LabelNoHighValueError;
                return null;
            }
            var il = vals.IndexOf(low);
            var ih = vals.IndexOf(high);
            if (il < 0 || ih < 0 || count < 2)
            {
                Error = TranslationStrings.LabelWrongValue;
                return null;
            }
            if (il == ih)
            {
                res.Add(vals[il]);
                return res;
            }

            int availableSteps = Math.Abs(ih - il) + 1;
            count = Math.Min(count, availableSteps);
            count = Math.Max(count, 2);

            for (int j = 0; j < count; j++)
            {
                double ratio = (double)j / (count - 1);
                int index = (int)Math.Round(il + ratio * (ih - il));
                if (index < 0) index = 0;
                if (index >= vals.Count) index = vals.Count - 1;
                string val = vals[index];
                if (!res.Contains(val))
                {
                    res.Add(val);
                }
            }
            return res;
        }

    }
}
