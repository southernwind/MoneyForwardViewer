using System.Data.Common;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using MoneyForwardViewer.DataBase.Tables;

namespace MoneyForwardViewer.DataBase {
	public class MoneyForwardViewerDbContext : DbContext {
		private readonly DbConnection _dbConnection;

		public DbSet<MfTransaction> MfTransactions {
			get;
			set;
		}

		public MoneyForwardViewerDbContext(DbConnection dbConnection) {
			this._dbConnection = dbConnection;
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			// Primary Keys
			modelBuilder.Entity<MfTransaction>().HasKey(x => x.TransactionId);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			switch (this._dbConnection) {
				case SqliteConnection conn:
					optionsBuilder.UseSqlite(conn);
					break;
			}
		}
	}
}