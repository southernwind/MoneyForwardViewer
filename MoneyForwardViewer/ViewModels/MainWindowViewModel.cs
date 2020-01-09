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

		public IReactiveProperty<DateTime> FromDate {
			get;
		}

		public IReactiveProperty<DateTime> ToDate {
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
				return x => $"{new DateTime((long)x):MM/dd}";
			}
		}

		public Func<double, string> CurrencyLabelFormatter {
			get {
				return x => $"{x:\\\\#,0}";
			}
		}

		public MainWindowViewModel(MoneyForward moneyForward) {
			var lastmonth = DateTime.Today.AddMonths(-1);
			this.FromDate = new ReactivePropertySlim<DateTime>(new DateTime(lastmonth.Year,lastmonth.Month,1));
			this.ToDate = new ReactivePropertySlim<DateTime>(new DateTime(lastmonth.Year,lastmonth.Month,DateTime.DaysInMonth(lastmonth.Year,lastmonth.Month)));
			this.Id = moneyForward.Id.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Password = moneyForward.Password.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Transactions =
				moneyForward.Transactions
					.CombineLatest(this.FromDate,(transactions, from)=> new { transactions, from })
					.CombineLatest(this.ToDate,(x,to)=>new {x.transactions,x.from,to})
					.Select(x => x.transactions.Where(t => t.Date > x.from && t.Date < x.to))
					.ToReadOnlyReactivePropertySlim();
			this.Assets =
				moneyForward.Assets
					.CombineLatest(this.FromDate, (assets, from) => new { assets, from })
					.CombineLatest(this.ToDate, (x, to) => new { x.assets, x.from, to })
					.Select(x => x.assets.Where(t => t.Date > x.from && t.Date < x.to))
					.ToReadOnlyReactivePropertySlim();
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
							LabelPoint = p => $"{p.SeriesView.Title}\n{p.Y:\\\\#,0}"
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
