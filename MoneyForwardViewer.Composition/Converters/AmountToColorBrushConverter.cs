using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MoneyForwardViewer.Composition.Converters {
	public class AmountToColorBrushConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value is int amount) {
				return amount >= 0 ? new SolidColorBrush(Colors.DarkBlue) : new SolidColorBrush(Colors.Red);
			}
			throw new InvalidOperationException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new InvalidOperationException();
			
		}
	}
}