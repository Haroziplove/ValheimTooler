using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ValheimAdminToolLauncher.Utils;

namespace ValheimAdminToolLauncher.Converters
{
    public class InstallStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Installer.InstallStatus installStatus = (Installer.InstallStatus)value;

            return installStatus == Installer.InstallStatus.IDLE ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
