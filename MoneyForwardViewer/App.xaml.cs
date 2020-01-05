using System.Windows;

using MoneyForwardViewer.Scraper;

namespace MoneyForwardViewer {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		protected override void OnStartup(StartupEventArgs e) {
			var mfs = new MoneyForwardScraper("id", "pw");
			var task = mfs.GetTransactions();
			base.OnStartup(e);
		}
	}
}
