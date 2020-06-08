using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
		public static async Task Timer([TimerTrigger("0 0/10 * * * *")] TimerInfo myTimer, ILogger log) {
			var to = DateTime.Now.Date;
			var from = to.Date.AddYears(-1);
			await ImportMoneyForwardDataCore(from, to, log);
		}

		[FunctionName("ImportMoneyForwardDataHttp")]
		public static async Task<IActionResult> Http([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log) {
			string from = req.Query["from"];
			string to = req.Query["to"];

			await ImportMoneyForwardDataCore(DateTime.Parse(from), DateTime.Parse(to), log);
			return new OkObjectResult("ok");
		}

		private static async Task ImportMoneyForwardDataCore(DateTime from, DateTime to, ILogger log) {
			log.LogInformation($"{from:yyyy/MM/dd}から{to:yyyy/MM/dd}の期間のデータを取得します。");
			var id = Configuration.GetValue<string>("MAIL");
			log.LogInformation($"アカウント[{id}]の情報を取得してデータベースに保存します。");
			var password = Configuration.GetValue<string>("PASSWORD");
			var mfs = new MoneyForwardScraper(id, password);
			var dbContext = GetDbContext();

			await using var transaction = await dbContext.Database.BeginTransactionAsync();
			// 取引履歴登録
			await foreach (var mt in mfs.GetTransactions(from, to)) {
				var ids = mt.Select(x => x.TransactionId).ToArray();
				var deleteTransactionList = dbContext.MfTransactions.Where(t => ids.Contains(t.TransactionId));
				dbContext.MfTransactions.RemoveRange(deleteTransactionList);
				await dbContext.MfTransactions.AddRangeAsync(mt);
				log.LogInformation($"{mt.First()?.Date:yyyy/MM}取引履歴{mt.Count()}件登録");
			}

			// 資産推移登録
			await foreach (var ma in mfs.GetAssets(from, to)) {
				var assets =
					ma.GroupBy(x => new { x.Date, x.Institution, x.Category })
						.Select(x => new MfAsset {
							Date = x.Key.Date,
							Institution = x.Key.Institution,
							Category = x.Key.Category,
							Amount = x.Sum(a => a.Amount)
						}).ToArray();
				var deleteAssetList = dbContext.MfAssets.Where(a => a.Date == assets.First().Date);
				dbContext.MfAssets.RemoveRange(deleteAssetList);
				await dbContext.MfAssets.AddRangeAsync(assets);
				log.LogInformation($"{ma.First().Date:yyyy/MM/dd}資産推移{assets.Count()}件登録");
			}

			await dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
			;
			log.LogInformation($"完了");
		}

		private static MoneyForwardViewerDbContext GetDbContext() {
			// DataBase
			var dbContext = new MoneyForwardViewerDbContext(DbType.SqlServer, Configuration.GetConnectionString("DefaultSqlConnection"));
			dbContext.Database.EnsureCreated();
			return dbContext;
		}
	}
}
