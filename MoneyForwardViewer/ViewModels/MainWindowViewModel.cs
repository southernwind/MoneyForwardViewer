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

		public ReactiveCommand ImportCommand {
			get;
		} = new ReactiveCommand();

		public MainWindowViewModel(MoneyForward moneyForward) {
			this.Id = moneyForward.Id.ToReactivePropertyAsSynchronized(x => x.Value);
			this.Password = moneyForward.Password.ToReactivePropertyAsSynchronized(x => x.Value);
			this.ImportCommand.Subscribe(async () => {
				await moneyForward.ImportFromMoneyForward();
			});
		}
	}
}
