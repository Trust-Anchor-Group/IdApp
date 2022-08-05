using System.Threading.Tasks;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Calculator
{
	/// <summary>
	/// The view model to bind to for when displaying the calculator.
	/// </summary>
	public class CalculatorViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="CalculatorViewModel"/> class.
		/// </summary>
		public CalculatorViewModel()
			: base()
		{
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out CalculatorNavigationArgs args))
			{
				this.Entry = args.Entry;
				this.ViewModel = args.ViewModel;
				this.Property = args.Property;
			}
		}

		#region Properties

		/// <summary>
		/// See <see cref="Entry"/>
		/// </summary>
		public static readonly BindableProperty EntryProperty =
			BindableProperty.Create(nameof(Entry), typeof(Entry), typeof(CalculatorViewModel), default(Entry));

		/// <summary>
		/// Entry of eDaler
		/// </summary>
		public Entry Entry
		{
			get => (Entry)this.GetValue(EntryProperty);
			set => this.SetValue(EntryProperty, value);
		}

		/// <summary>
		/// See <see cref="ViewModel"/>
		/// </summary>
		public static readonly BindableProperty ViewModelProperty =
			BindableProperty.Create(nameof(ViewModel), typeof(BaseViewModel), typeof(CalculatorViewModel), default(BaseViewModel));

		/// <summary>
		/// ViewModel of eDaler
		/// </summary>
		public BaseViewModel ViewModel
		{
			get => (BaseViewModel)this.GetValue(ViewModelProperty);
			set => this.SetValue(ViewModelProperty, value);
		}

		/// <summary>
		/// See <see cref="Property"/>
		/// </summary>
		public static readonly BindableProperty PropertyProperty =
			BindableProperty.Create(nameof(Property), typeof(BindableProperty), typeof(CalculatorViewModel), default(BindableProperty));

		/// <summary>
		/// Property of eDaler
		/// </summary>
		public BindableProperty Property
		{
			get => (BindableProperty)this.GetValue(PropertyProperty);
			set => this.SetValue(PropertyProperty, value);
		}

		#endregion

		#region Commands

		#endregion


	}
}
