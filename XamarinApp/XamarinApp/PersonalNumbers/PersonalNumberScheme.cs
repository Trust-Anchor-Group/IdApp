using System;
using Waher.Script;

namespace XamarinApp.PersonalNumbers
{
	/// <summary>
	/// Checks personal numbers against a personal number scheme.
	/// </summary>
	public class PersonalNumberScheme
	{
		private readonly string variableName;
        private readonly Expression pattern;
		private readonly Expression check;

		/// <summary>
		/// Checks personal numbers against a personal number scheme.
		/// </summary>
		/// <param name="variableName">Name of variable to use in script for the personal number.</param>
		/// <param name="displayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
		/// <param name="pattern">Expression checking if the scheme applies to a personal number.</param>
		public PersonalNumberScheme(string variableName, string displayString, Expression pattern)
			: this(variableName, displayString, pattern, null)
		{
		}

		/// <summary>
		/// Checks personal numbers against a personal number scheme.
		/// </summary>
		/// <param name="variableName">Name of variable to use in script for the personal number.</param>
		/// <param name="displayString">A string that can be displayed to a user, informing the user about the approximate format expected.</param>
		/// <param name="pattern">Expression checking if the scheme applies to a personal number.</param>
		/// <param name="check">Optional expression, checking if the contents of the personal number is valid.</param>
		public PersonalNumberScheme(string variableName, string displayString, Expression pattern, Expression check)
		{
			this.variableName = variableName;
			this.DisplayString = displayString;
			this.pattern = pattern;
			this.check = check;
		}

		/// <summary>
		/// A string that can be displayed to a user, informing the user about the approximate format expected.
		/// </summary>
		public string DisplayString { get; }

        /// <summary>
		/// Checks if a personal number is valid according to the personal number scheme.
		/// </summary>
		/// <param name="personalNumber">String representation of the personal number.</param>
		/// <returns>
		/// true = valid
		/// false = invalid
		/// null = scheme not applicable
		/// </returns>
		public bool? IsValid(string personalNumber)
		{
			try
			{
				Variables variables = new Variables(new Variable(this.variableName, personalNumber));
				object result = this.pattern.Evaluate(variables);

				if (result is bool b)
				{
					if (!b)
						return null;

					if (!(this.check is null))
					{
						result = this.check.Evaluate(variables);
						return result is bool b2 && b2;
					}
					else
						return true;
				}
				else
					return null;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
