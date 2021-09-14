using Serilog.Core;
using Serilog.Events;
using Stride.Core;

namespace StrideSaber.Logging
{
	/// <inheritdoc />
	public class StrideObjectDestructurer : IDestructuringPolicy
	{
		/// <inheritdoc />
		public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
		{
			if (value is ComponentBase component)
			{
				result = propertyValueFactory.CreatePropertyValue(component.Name);
				return true;
			}

			result = null;
			return false;
		}
	}
}