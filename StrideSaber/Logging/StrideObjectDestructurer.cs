using Serilog.Core;
using Serilog.Events;
using Stride.Core;
using Stride.UI;
using System.Diagnostics;
using System.Reflection;

namespace StrideSaber.Logging
{
	/// <inheritdoc />
	public sealed class StrideObjectDestructurer : IDestructuringPolicy
	{
		/// <inheritdoc />
		public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
		{
			switch (value)
			{
				case ComponentBase component:
					result = propertyValueFactory.CreatePropertyValue($"[Component] {component.Name}");
					return true;
				case UIElement uiElement:
					result = propertyValueFactory.CreatePropertyValue($"[UiElement] {uiElement.Name}");
					return true;
				default:
					result = null;
					return false;
			}
		}
	}
}