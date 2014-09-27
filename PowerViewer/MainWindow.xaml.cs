using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PowerViewer
{

    /// <summary>
    /// Wrapper of POWER_DATA_ACCESSOR enum.
    /// </summary>
    [Flags]
    enum PowerDataAccessor : short
    {
        AccessAcPowerSettingIndex = 0,
        AccessDcPowerSettingIndex = 1,
        AccessScheme = 16,
        AccessSubgroup = 17,
        AccessIndividualSetting = 18,
        AccessActiveScheme = 19,
        AccessCreateScheme = 20
    }

    /// <summary>
    /// Wrapper for native power management functions.
    /// </summary>
    class LibWrap
    {
        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr rootPowerKey, IntPtr schemeGuid,
            IntPtr subGroupOfPowerSettingsGuid, PowerDataAccessor accessorFlags,
            UInt32 index, ref Guid buffer, ref UInt32 bufferSize);

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerGetActiveScheme(IntPtr userRootPowerKey, ref IntPtr activePolicyGuid);

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto)]
        public static extern UInt32 PowerReadFriendlyName(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subGroupOfPowerSettingsGuid,
            IntPtr powerSettingGuid,
            IntPtr buffer,
            ref int bufferSize);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void Power_Viewer_Loaded(object sender, RoutedEventArgs e)
        {
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
            this.Refresh();
        }

        void SystemParameters_StaticPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PowerLineStatus":
                    this.Refresh();
                    break;

                default:
                    break;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.Refresh();
        }

        private void Refresh()
        {
            maxBatteryLifeLabel.IsEnabled = true;
            currentBatteryLifeLabel.IsEnabled = true;
            Battery.Visibility = System.Windows.Visibility.Visible;
            maxBatteryLifeGraphic.Visibility = System.Windows.Visibility.Visible;
            currentBatteryLifeGraphic.Visibility = System.Windows.Visibility.Visible;
            Plug.Visibility = System.Windows.Visibility.Hidden;
            doubleDash1.Visibility = System.Windows.Visibility.Hidden;
            doubleDash2.Visibility = System.Windows.Visibility.Hidden;

            if (SystemParameters.PowerLineStatus == System.Windows.PowerLineStatus.Online)
            {
                maxBatteryLifeLabel.IsEnabled = false;
                maxBatteryLife.Text = "";

                currentBatteryLifeLabel.IsEnabled = false;
                currentBatteryLife.Text = "";

                powerStatus.Text = "Charging";

                Battery.Visibility = System.Windows.Visibility.Hidden;
                Plug.Visibility = System.Windows.Visibility.Visible;

                maxBatteryLifeGraphic.Visibility = System.Windows.Visibility.Hidden;
                currentBatteryLifeGraphic.Visibility = System.Windows.Visibility.Hidden;

                doubleDash1.Visibility = System.Windows.Visibility.Visible;
                doubleDash2.Visibility = System.Windows.Visibility.Visible;


                return;
            }
            else
                powerStatus.Text = "On battery";

            //Computing maximum battery life.
            int seconds = System.Windows.Forms.SystemInformation.PowerStatus.BatteryFullLifetime;
            int hours, minutes;
            if (seconds == -1)
            {
                maxBatteryLife.Text = "Unknown";
            }
            else
            {
                minutes = seconds / 60;
                hours = minutes / 60;
                minutes = minutes % 60;

                maxBatteryLife.Text = hours.ToString() + ":" + minutes.ToString();
            }
            //Computing remaining battery life.
            seconds = System.Windows.Forms.SystemInformation.PowerStatus.BatteryLifeRemaining;

            if (seconds == -1)
            {
                currentBatteryLife.Text = "Unknown";
            }
            else
            {
                minutes = seconds / 60;
                hours = minutes / 60;
                currentBatteryLife.Text = hours.ToString() + ":" + minutes.ToString();
            }
        }

        private void togglePowerModes_Click(object sender, RoutedEventArgs e)
        {
            IntPtr activeGuid = IntPtr.Zero;

            LibWrap.PowerGetActiveScheme(IntPtr.Zero, ref activeGuid);
            
            int bufferSize = 150;
            IntPtr pactiveName = Marshal.AllocHGlobal(bufferSize);
            
            LibWrap.PowerReadFriendlyName(IntPtr.Zero, activeGuid, IntPtr.Zero, IntPtr.Zero, pactiveName, ref bufferSize);
            Marshal.FreeHGlobal(activeGuid);
            
            string activeName = Marshal.PtrToStringUni(pactiveName);
            Marshal.FreeHGlobal(pactiveName);

            Guid nextPlan;

            if (activeName == "Power saver")
                nextPlan = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");        //Balanced plan
            else
                nextPlan = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");        //Power saver

            LibWrap.PowerSetActiveScheme(IntPtr.Zero, ref nextPlan);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Built by J. Y. Sandilya. All rights reserved.");
        }
    }
}
