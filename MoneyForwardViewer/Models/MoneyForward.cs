using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using MoneyForwardViewer.DataBase;
using MoneyForwardViewer.DataBase.Tables;
using MoneyForwardViewer.Scraper;

using Prism.Mvvm;

using Reactive.Bindings;

namespace MoneyForwardViewer.Models {
	internal class MoneyForward : BindableBase {
		private readonly MoneyForwardViewerDbContext _dbContext;

		public IReactiveProperty<string> Id {
			get;
		} = new ReactiveProperty<string>();

		public IReactiveProperty<string> Password {
			get;
		} = new ReactiveProperty<string>();

		public IReactiveProperty<IEnumerable<MfTransaction>> Transactions {
			get;
		} = new ReactivePropertySlim<IEnumerable<MfTransaction>>(Array.Empty<MfTransaction>());

		public IReactiveProperty<IEnumerable<MfAsset>> Assets {
			get;
		} = new ReactivePropertySlim<IEnumerable<MfAsset>>(Array.Empty<MfAsset>());

		public IReactiveProperty<string> ProcessingText {
			get;
		} = new ReactivePropertySlim<string>();

		public MoneyForward(MoneyForwardViewerDbContext dbContext) {
			this._dbContext = dbContext;
		}

		public Func<double, string> LabelFormatter {
			get {
				return x => x.ToString();
			}
		}
		public async Task ImportFromMoneyForward() {
			var mfs = new MoneyForwardScraper(this.Id.Value, this.Password.Value);
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			// 取引履歴登録
			await foreach (var mt in mfs.GetTransactions()) {
				var ids = mt.Select(x => x.TransactionId).ToArray();
				var deleteTransactionList = this._dbContext.MfTransactions.Where(t => ids.Contains(t.TransactionId));
				this._dbContext.MfTransactions.RemoveRange(deleteTransactionList);
				await this._dbContext.MfTransactions.AddRangeAsync(mt);
				this.ProcessingText.Value = $"{mt.First()?.Date:yyyy/MM}取引履歴{mt.Length}件登録";
			}

			// 資産推移登録
			await foreach (var ma in mfs.GetAssets()) {
				var assets =
					ma.GroupBy(x => new { x.Date, x.Institution, x.Category })
						.Select(x => new MfAsset {
							Date = x.Key.Date,
							Institution = x.Key.Institution,
							Category = x.Key.Category,
							Amount = x.Sum(a => a.Amount)
						}).ToArray();
				var dates = assets.Select(x => x.Date).ToArray();
				var deleteAssetList = this._dbContext.MfAssets.Where(a => dates.Contains(a.Date));
				this._dbContext.MfAssets.RemoveRange(deleteAssetList);
				await this._dbContext.MfAssets.AddRangeAsync(assets);
				this.ProcessingText.Value = $"{ma.First().Date:yyyy/MM/dd}資産推移{assets.Length}件登録";
			}
			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
			await this.LoadTransactions();
			await this.LoadAssets();
			this.ProcessingText.Value = "";
		}

		public async Task LoadTransactions() {
			this.Transactions.Value = await this._dbContext.MfTransactions.OrderBy(x => x.Date).ToArrayAsync();
		}

		public async Task LoadAssets() {
			this.Assets.Value = await this._dbContext.MfAssets.OrderBy(x => x.Date).ToArrayAsync();
		}
	}
}
