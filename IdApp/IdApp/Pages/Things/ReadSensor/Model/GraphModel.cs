using IdApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Waher.Events;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Things.SensorData;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a set of historical field values.
	/// </summary>
	public class GraphModel : INotifyPropertyChanged
	{
		private readonly SortedDictionary<DateTime, Field> fieldValues = new();
		private readonly ServiceReferences references;
		private readonly string fieldName;
		private Timer timer = null;
		private ImageSource image = null;

		/// <summary>
		/// Represents a set of historical field values.
		/// </summary>
		/// <param name="Field">Field added</param>
		/// <param name="References">Service references</param>
		public GraphModel(Field Field, ServiceReferences References)
		{
			this.fieldName = Field.Name;
			this.references = References;

			this.Add(Field);
		}

		/// <summary>
		/// Field Name
		/// </summary>
		public string FieldName => this.fieldName;

		/// <summary>
		/// Image Source
		/// </summary>
		public ImageSource Image => this.image;

		/// <summary>
		/// If graph has an image.
		/// </summary>
		public bool HasImage => this.image is not null;

		/// <summary>
		/// Adds a historical field value.
		/// </summary>
		/// <param name="Field">Field Value.</param>
		public void Add(Field Field)
		{
			lock (this.fieldValues)
			{
				this.fieldValues[Field.Timestamp] = Field;
			}

			this.timer?.Dispose();
			this.timer = null;

			this.timer = new Timer(this.GenerateGraph, null, 500, Timeout.Infinite);
		}

		private async void GenerateGraph(object _)
		{
			try
			{
				Variables v = new();
				Expression Exp;

				lock (this.fieldValues)
				{
					v["x"] = Expression.Encapsulate(this.fieldValues.Keys);
					v["y"] = Expression.Encapsulate(this.fieldValues.Values);

					StringBuilder sb = new();
					bool Found = false;

					sb.AppendLine("G:=plot2dcurve(x,y.Values,'Red');");
					sb.Append("G.Title:='");
					sb.Append(this.fieldName.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("'", "\\'"));
					sb.AppendLine("';");
					sb.Append("G.LabelX:='");
					sb.Append(LocalizationResourceManager.Current["Time"]);
					sb.AppendLine("';");
					sb.Append("G.LabelY:='");

					foreach (Field F in this.fieldValues.Values)
					{
						if (F is QuantityField Q)
						{
							sb.Append(Q.Unit);
							Found = true;
							break;
						}
					}

					if (!Found)
						sb.Append(LocalizationResourceManager.Current["Value"]);

					sb.AppendLine("';");
					sb.AppendLine("G");

					Exp = new Expression(sb.ToString());
				}

				Graph Graph = await Exp.EvaluateAsync(v) as Graph;

				GraphSettings Settings = new()
				{
					Width = 800,
					Height = 400
				};

				PixelInformation Pixels = Graph.CreatePixels(Settings);
				byte[] Png = Pixels.EncodeAsPng();

				this.references.UiSerializer.BeginInvokeOnMainThread(() =>
				{
					bool OldWasNull = this.image is null;
					this.image = ImageSource.FromStream(() => new MemoryStream(Png));

					this.RaisePropertyChanged(nameof(this.Image));
					if (OldWasNull)
						this.RaisePropertyChanged(nameof(this.HasImage));
				});
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private void RaisePropertyChanged(string Name)
		{
			try
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Event raised when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
