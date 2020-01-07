using System;
using System.Collections;
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

		public MoneyForward(MoneyForwardViewerDbContext dbContext) {
			this._dbContext = dbContext;
		}

		public async Task ImportFromMoneyForward() {
			var mfs = new MoneyForwardScraper(this.Id.Value, this.Password.Value);
			var mfTransactions = (await mfs.GetTransactions()).OrderBy(x => x.Date).ToArray();
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
			var ids = mfTransactions.Select(x => x.TransactionId).ToArray();
			var deleteList = this._dbContext.MfTransactions.Where(t => ids.Contains(t.TransactionId));
			this._dbContext.MfTransactions.RemoveRange(deleteList);
			await this._dbContext.MfTransactions.AddRangeAsync(mfTransactions);
			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
			this.Transactions.Value = mfTransactions;
		}

		public async Task LoadTransactions() {
			this.Transactions.Value = await this._dbContext.MfTransactions.OrderBy(x => x.Date).ToArrayAsync();
		}
	}
}
