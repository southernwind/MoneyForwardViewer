using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoneyForwardViewer.DataBase;
using MoneyForwardViewer.DataBase.Tables;
using MoneyForwardViewer.Scraper;

namespace MoneyForwardViewer.AzureFunctions {
	public static class ImportMoneyForwardData {

		private static IConfigurationRoot Configuration {
			get;
		}

		static ImportMoneyForwardData() {
			var builder = new ConfigurationBuilder()
				.AddJsonFile("local.settings.json", true)
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		[FunctionName("ImportMoneyForwardData")]
		public static async Task Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, ILogger log) {
			var id = Configuration.GetValue<string>("MAIL");
			log.LogInformation($"アカウント[{id}]の情報を取得してデータベースに保存します。");
			var password = Configuration.GetValue<string>("PASSWORD");
			var mfs = new MoneyForwardScraper(id, password);
			// 取引履歴取得
			var mfTransactions = (await mfs.GetTransactions()).OrderBy(x => x.Date).ToArray();
			log.LogInformation($"取引履歴{mfTransactions.Count()}件");
			// 資産推移取得
			var mfAssets =
				(await mfs.GetAssets())
					.OrderBy(x => x.Date)
					.GroupBy(x => new { x.Date, x.Institution, x.Category })
					.Select(x => new MfAsset {
						Date = x.Key.Date,
						Institution = x.Key.Institution,
						Category = x.Key.Category,
						Amount = x.Sum(a => a.Amount)
					})
					.ToArray();
			log.LogInformation($"資産推移{mfAssets.Count()}件");
			var dbContext = GetDbContext();

			await using var transaction = await dbContext.Database.BeginTransactionAsync();
			// 取引履歴登録
			var ids = mfTransactions.Select(x => x.TransactionId).ToArray();
			var deleteTransactionList = dbContext.MfTransactions.Where(t => ids.Contains(t.TransactionId));
			dbContext.MfTransactions.RemoveRange(deleteTransactionList);
			await dbContext.MfTransactions.AddRangeAsync(mfTransactions);
			// 資産推移登録
			var dates = mfAssets.Select(x => x.Date).ToArray();
			var deleteAssetList = dbContext.MfAssets.Where(a => dates.Contains(a.Date));
			dbContext.MfAssets.RemoveRange(deleteAssetList);
			await dbContext.MfAssets.AddRangeAsync(mfAssets);

			await dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		private static MoneyForwardViewerDbContext GetDbContext() {
			// DataBase
			var dbContext = new MoneyForwardViewerDbContext(DbType.SQLServer, Configuration.GetConnectionString("DefaultSqlConnection"));
			dbContext.Database.EnsureCreated();
			return dbContext;
		}
	}
}
