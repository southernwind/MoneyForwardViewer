using Microsoft.EntityFrameworkCore;

using MoneyForwardViewer.DataBase.Tables;

namespace MoneyForwardViewer.DataBase {
	public enum DbType {
		SqLite,
		SqlServer
	}
	public class MoneyForwardViewerDbContext : DbContext {
		private readonly string _dbConnectionString;
		private readonly DbType _dbType;

		public DbSet<MfTransaction> MfTransactions {
			get;
			set;
		}

		public DbSet<MfAsset> MfAssets {
			get;
			set;
		}

		public MoneyForwardViewerDbContext(DbType dbType, string dbConnectionString) {
			this._dbConnectionString = dbConnectionString;
			this._dbType = dbType;
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			// Primary Keys
			modelBuilder.Entity<MfTransaction>().HasKey(x => x.TransactionId);
			modelBuilder.Entity<MfAsset>().HasKey(x => new { x.Date, x.Institution, x.Category });
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			switch (this._dbType) {
				case DbType.SqLite:
					optionsBuilder.UseSqlite(this._dbConnectionString);
					break;
				case DbType.SqlServer:
					optionsBuilder.UseSqlServer(this._dbConnectionString);
					break;
			}
		}
	}
}