using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

		public MoneyForward(MoneyForwardViewerDbContext dbContext) {
			this._dbContext = dbContext;
		}

		public Func<double,string> LabelFormatter {
			get {
				return x => x.ToString();
			}
		}
		public async Task ImportFromMoneyForward() {
			var mfs = new MoneyForwardScraper(this.Id.Value, this.Password.Value);
			// 取引履歴取得
			var mfTransactions = (await mfs.GetTransactions()).OrderBy(x => x.Date).ToArray();
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
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			// 取引履歴登録
			var ids = mfTransactions.Select(x => x.TransactionId).ToArray();
			var deleteTransactionList = this._dbContext.MfTransactions.Where(t => ids.Contains(t.TransactionId));
			this._dbContext.MfTransactions.RemoveRange(deleteTransactionList);
			await this._dbContext.MfTransactions.AddRangeAsync(mfTransactions);
			// 資産推移登録
			var dates = mfAssets.Select(x => x.Date).ToArray();
			var deleteAssetList = this._dbContext.MfAssets.Where(a => dates.Contains(a.Date));
			this._dbContext.MfAssets.RemoveRange(deleteAssetList);
			await this._dbContext.MfAssets.AddRangeAsync(mfAssets);

			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
			this.Transactions.Value = mfTransactions;
		}

		public async Task LoadTransactions() {
			this.Transactions.Value = await this._dbContext.MfTransactions.OrderBy(x => x.Date).ToArrayAsync();
		}

		public async Task LoadAssets() {
			this.Assets.Value = await this._dbContext.MfAssets.OrderBy(x => x.Date).ToArrayAsync();
		}
	}
}
