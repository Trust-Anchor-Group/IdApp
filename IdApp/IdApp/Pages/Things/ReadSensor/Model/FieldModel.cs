using System;
using Waher.Things.SensorData;

namespace IdApp.Pages.Things.ReadSensor.Model
{
	/// <summary>
	/// Represents a sensor-data field.
	/// </summary>
	public class FieldModel
	{
		private readonly Field field;

		/// <summary>
		/// Represents a sensor-data field.
		/// </summary>
		/// <param name="Field">Sensor-data field.</param>
		public FieldModel(Field Field)
		{
			this.field = Field;
		}

		/// <summary>
		/// Field Name
		/// </summary>
		public string Name => this.field.Name;

		/// <summary>
		/// If the field is writable (through the actuator interface)
		/// </summary>
		public bool Writable => this.field.Writable;

		/// <summary>
		/// Quality of service of data
		/// </summary>
		public FieldQoS QoS => this.field.QoS;

		/// <summary>
		/// Field Type
		/// </summary>
		public FieldType Type => this.field.Type;

		/// <summary>
		/// Timestamp of field.
		/// </summary>
		public DateTime Timestamp => this.field.Timestamp;
	}
}
