using System.Windows;

using Microsoft.Data.Sqlite;

using MoneyForwardViewer.DataBase;
using MoneyForwardViewer.Views;

using Prism.Ioc;

namespace MoneyForwardViewer {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		protected override Window CreateShell() {
			return this.Container.Resolve<MainWindow>();
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry) {
			// DataBase
			var sb = new SqliteConnectionStringBuilder {
				DataSource = "./database.db"
			};
			var dbContext = new MoneyForwardViewerDbContext(DbType.SQLite, sb.ConnectionString);
			dbContext.Database.EnsureCreated();
			containerRegistry.RegisterInstance(dbContext);
		}
	}
}
