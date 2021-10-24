using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;

//ReSharper disable all
namespace StrideSaber.Hacks
{
	/// <summary>
	///  A test class
	/// </summary>
	public sealed class TestClass
	{
		[EventMethod(typeof(TestEvent))]
		#pragma warning disable CA1822
		private void Fail_NonStatic()
		{
		}
		#pragma warning restore CA1822

		[EventMethod(typeof(TestEvent))]
		private static void Fail_TooManyParams(object o1, object o2, object o3)
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static void Fail_NotInheritedFromEvent(object o1)
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static void Success_VoidNoParams()
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static object? Success_ObjectReturnNoParams()
		{
			return null;
		}

		[EventMethod(typeof(TestEvent))]
		private static void Fail_CannotCastAttributeToParam(GameLoadEvent e)
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static void Success_Void_BaseEventParam(Event e)
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static void Success_Void_InheritedEventParam(TestEvent e)
		{
		}

		[EventMethod(typeof(TestEvent))]
		private static object? Success_ReturnObject_BaseEventParam(Event e)
		{
			return null;
		}

		[EventMethod(typeof(TestEvent))]
		private static object? Success_ReturnObject_InheritedEventParam(TestEvent e)
		{
			return null;
		}
	}
}