using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Waher.Script;
using Xamarin.Forms;

namespace IdApp.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// The view model to bind to for when displaying information about the current state of a state-machine.
	/// </summary>
	public class MachineVariablesViewModel : BaseViewModel
	{
		/// <summary>
		/// The view model to bind to for when displaying information about the current state of a state-machine.
		/// </summary>
		public MachineVariablesViewModel()
			: base()
		{
			this.Variables = new();
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out MachineVariablesNavigationArgs args))
			{
				this.Running = args.Running;
				this.Ended = args.Ended;
				this.CurrentState = args.CurrentState;

				if (args.Variables is not null)
				{
					foreach (Variable Variable in args.Variables)
						this.Variables.Add(new VariableModel(Variable.Name, Variable.ValueObject, Expression.ToString(Variable.ValueObject)));
				}
			}
		}

		#region Properties

		/// <summary>
		/// Current variables
		/// </summary>
		public ObservableCollection<VariableModel> Variables { get; }

		/// <summary>
		/// See <see cref="Running"/>
		/// </summary>
		public static readonly BindableProperty RunningProperty =
			BindableProperty.Create(nameof(Running), typeof(bool), typeof(MachineVariablesViewModel), default);

		/// <summary>
		/// If the state-machine is running
		/// </summary>
		public bool Running
		{
			get => (bool)this.GetValue(RunningProperty);
			set => this.SetValue(RunningProperty, value);
		}

		/// <summary>
		/// See <see cref="Ended"/>
		/// </summary>
		public static readonly BindableProperty EndedProperty =
			BindableProperty.Create(nameof(Ended), typeof(bool), typeof(MachineVariablesViewModel), default);

		/// <summary>
		/// If the state-machine has ended
		/// </summary>
		public bool Ended
		{
			get => (bool)this.GetValue(EndedProperty);
			set => this.SetValue(EndedProperty, value);
		}

		/// <summary>
		/// See <see cref="CurrentState"/>
		/// </summary>
		public static readonly BindableProperty CurrentStateProperty =
			BindableProperty.Create(nameof(CurrentState), typeof(string), typeof(MachineVariablesViewModel), default);

		/// <summary>
		/// Current state of state-machine
		/// </summary>
		public string CurrentState
		{
			get => (string)this.GetValue(CurrentStateProperty);
			set => this.SetValue(CurrentStateProperty, value);
		}

		#endregion

	}
}
