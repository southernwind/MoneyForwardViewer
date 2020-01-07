using System.Collections.Generic;
using MoneyForwardViewer.DataBase.Tables;
using MoneyForwardViewer.Models;

using Prism.Mvvm;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace MoneyForwardViewer.ViewModels {
	internal class MainWindowViewModel : BindableBase {
		public IReactiveProperty<string> Id {
			get;
		}

		public IReactiveProperty<string> Password {
			get;
		}

		public IReadOnlyReactiveProperty<IEnumerable<MfTransaction>> Transactions {
			get;
		}

		public ReactiveCommand ImportCommand {
			get;
		} = new ReactiveCommand();

		public ReactiveCommand LoadCommand {
			get;
		} = new ReactiveCommand();

		public MainWindowViewModel(MoneyForward moneyForward) {
			this.Id = moneyForward.Id.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Password = moneyForward.Password.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Transactions = moneyForward.Transactions.ToReadOnlyReactivePropertySlim();
			this.ImportCommand.Subscribe(async () => {
				await moneyForward.ImportFromMoneyForward();
			});
			this.LoadCommand.Subscribe(async () => {
				await moneyForward.LoadTransactions();
			});
		}
	}
}
