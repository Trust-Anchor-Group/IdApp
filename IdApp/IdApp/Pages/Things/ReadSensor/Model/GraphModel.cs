using IdApp.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using Waher.Events;
using Waher.Script;
using Waher.Script.Graphs;
using Waher.Script.Objects.VectorSpaces;
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
					int c = this.fieldValues.Count;
					DateTime[] Timepoints = new DateTime[c];
					Field[] Values = new Field[c];

					this.fieldValues.Keys.CopyTo(Timepoints, 0);
					this.fieldValues.Values.CopyTo(Values, 0);

					v["x"] = new DateTimeVector(Timepoints);
					v["y"] = new ObjectVector(Values);
				}

				StringBuilder sb = new();
				bool Found = false;

				sb.AppendLine("y:=y.Value;");
				sb.AppendLine("G:=scatter2d(x,y,'Blue',7)+plot2dline(x,y,'Red',5);");
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
					sb.Append(this.fieldName.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("'", "\\'"));

				sb.AppendLine("';");
				sb.AppendLine("G");

				Exp = new Expression(sb.ToString());

				Graph Graph = await Exp.EvaluateAsync(v) as Graph;

				if (Graph is not null)
				{
					GraphSettings Settings = new()
					{
						Width = 1280,
						Height = 720,
						AxisColor = SKColors.Black,			// TODO: Light & Dark Theme
						BackgroundColor = SKColors.White,   // TODO: Light & Dark Theme
						GridColor = SKColors.LightGray,     // TODO: Light & Dark Theme
						AxisWidth = 5,
						FontName = "Arial",                 // TODO: App font
						LabelFontSize = 40,
						GridWidth = 3
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
