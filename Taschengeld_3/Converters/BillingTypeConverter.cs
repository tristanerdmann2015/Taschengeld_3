using System.Globalization;
using Taschengeld_3.Models;

namespace Taschengeld_3.Converters;

public class BillingTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BillingType billingType)
        {
            return billingType switch
            {
                BillingType.PerHour => "pro Stunde",
                BillingType.PerCount => "pro Stück",
                _ => "Unbekannt"
            };
        }
        return "Unbekannt";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str switch
            {
                "pro Stunde" => BillingType.PerHour,
                "pro Stück" => BillingType.PerCount,
                _ => BillingType.PerHour
            };
        }
        return BillingType.PerHour;
    }
}
