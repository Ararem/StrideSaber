using System;
using System.Threading.Tasks;

namespace StrideSaber.Diagnostics
{
	/// <summary>
	/// A delegate type for a <see cref="TrackedTask"/>
	/// </summary>
	/// <param name="updateProgress">A method to be called to update progress.</param>
	/// <example>
	///<code>
	/// <![CDATA[
	/// new BackgroundTask("Example Task Name",
	///		delegate(Action<float> updateProgress)
	///			{
	///				const int start = 0;
	///				const int end = 100;
	/// 			for (int i = start; i < end; i++)
	/// 			{
	/// 				int x = (i + 69) * 420;
	/// 				float progress = (float)((i - start) + 1) / (end - start); //Complex math but it works for any start and end
	/// 				updateProgress(progress);
	/// 			}
	///			}
	///		).Run();
	/// ]]>
	/// </code>
	/// </example>
	public delegate Task TrackedTaskDelegate(Action<float> updateProgress);
}