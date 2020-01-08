using System.Collections.Generic;
using MoneyForwardViewer.DataBase.Tables;
using MoneyForwardViewer.Models;

using Prism.Mvvm;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

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

		public IReadOnlyReactiveProperty<IEnumerable<MfAsset>> Assets {
			get;
		}
		public IReadOnlyReactiveProperty<SeriesCollection> LargeCategorySeriesCollection {
			get;
		}

		public IReadOnlyReactiveProperty<SeriesCollection> AssetTransitionSeriesCollection {
			get;
		}

		public ReactiveCommand ImportCommand {
			get;
		} = new ReactiveCommand();

		public ReactiveCommand LoadCommand {
			get;
		} = new ReactiveCommand();

		public Func<double, string> DateLabelFormatter {
			get {
				return x => $"{new DateTime((long)x):yyyy/MM/dd}";
			}
		}

		public MainWindowViewModel(MoneyForward moneyForward) {
			this.Id = moneyForward.Id.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Password = moneyForward.Password.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Transactions = moneyForward.Transactions.ToReadOnlyReactivePropertySlim();
			this.Assets = moneyForward.Assets.ToReadOnlyReactivePropertySlim();
			this.LargeCategorySeriesCollection = this.Transactions.Select(x => {
				var sc = new SeriesCollection();
				var sl =
					x.Where(t => t.IsCalculateTarget)
						.GroupBy(t => t.LargeCategory)
						.Select(g => new {Title=g.Key,Value= g.Sum(x => -x.Amount) })
						.Where(x => x.Value > 0)
						.OrderBy(x => x.Value)
						.Select(x => new PieSeries() {
							Title = x.Title,
							Values = new ChartValues<int>(new []{ x.Value }),
							DataLabels = true,
							LabelPoint = p => $"{p.SeriesView.Title}\n{p.Y:\\\\#,0}\n{p.Participation:P}"
						});

				sc.AddRange(sl);
				return sc;
			}).ToReadOnlyReactivePropertySlim();
			this.AssetTransitionSeriesCollection = this.Assets.Select(x => {
				var sc = new SeriesCollection();
				var sl = x
					.GroupBy(x => new { x.Institution, x.Category })
					.Select(x => {
						return new StackedAreaSeries {
							Title = $"{x.Key.Institution}({x.Key.Category})",
							Values = new ChartValues<DateTimePoint>(
								x.Select(
									x => new DateTimePoint(x.Date, Math.Max(x.Amount,0))
								).ToArray()),
							LineSmoothness = 0
						};
					}).ToArray();
				sc.AddRange(sl);
				return sc;
			}).ToReadOnlyReactivePropertySlim();

			this.ImportCommand.Subscribe(async () => {
				await moneyForward.ImportFromMoneyForward();
			});
			this.LoadCommand.Subscribe(async () => {
				await moneyForward.LoadTransactions();
				await moneyForward.LoadAssets();
			});
		}
	}
}
