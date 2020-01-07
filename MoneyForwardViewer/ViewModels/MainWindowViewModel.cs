using System.Collections.Generic;
using MoneyForwardViewer.DataBase.Tables;
using MoneyForwardViewer.Models;

using Prism.Mvvm;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using OxyPlot;
using System.Reactive.Linq;
using OxyPlot.Series;
using System.Linq;

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

		public IReadOnlyReactiveProperty<PlotModel> LargeCategoryModel {
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
			this.LargeCategoryModel = this.Transactions.Select(x => {
				var pm = new PlotModel {
					Title = "支出"
				};
				var pie = new PieSeries();
				pie.TrackerFormatString = "{1}: \\{2:#,0} ({3:P1})";
				var slices =
					x.Where(t => t.IsCalculateTarget)
						.GroupBy(t => t.LargeCategory)
						.Select(g => new PieSlice(g.Key, g.Sum(x => -x.Amount)))
						.Where(x => x.Value > 0)
						.OrderBy(x => x.Value);
				foreach (var slice in slices) {
					pie.Slices.Add(slice);
				}
				pm.Series.Add(pie);
				return pm;
			}).ToReadOnlyReactivePropertySlim();
			this.ImportCommand.Subscribe(async () => {
				await moneyForward.ImportFromMoneyForward();
			});
			this.LoadCommand.Subscribe(async () => {
				await moneyForward.LoadTransactions();
			});
		}
	}
}
