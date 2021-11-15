using ConcurrentCollections;
using JetBrains.Annotations;
using Serilog;
using SmartFormat;
using SmartFormat.Core.Parsing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static StrideSaber.Diagnostics.TrackedTaskEvent;

// #define INCREMENT_ID

namespace StrideSaber.Diagnostics
{
	//TODO: IDisposable (or async)
	/// <summary>
	///  A task-like type that can be used to create tasks whose progress can be tracked and displayed to the user
	/// </summary>
	public sealed class TrackedTask : IFormattable
	{
		/// <summary>
		///  An object that can be locked upon for global (static) synchronisation
		/// </summary>
		private static readonly object GlobalLock = new();

		/// <summary>
		///  A <see cref="IReadOnlyCollection{T}">collection</see> that encompasses all the currently running tracked task instances
		/// </summary>
		/// <remarks>This value should be considered mutable and may change while being accessed, so should not be accessed directly</remarks>
		private static readonly ConcurrentHashSet<TrackedTask> Instances = new();

		private bool isDisposed = false;

		/// <summary>
		///  Constructs a new <see cref="TrackedTask"/>, with a specified <paramref name="name"/>
		/// </summary>
		/// <param name="name">The name of this <see cref="TrackedTask"/></param>
		/// <param name="taskDelegate">The <see cref="TrackedTaskDelegate"/> function to be executed</param>
		public TrackedTask(string name, TrackedTaskDelegate taskDelegate)
		{
			Name         = name;
			TaskDelegate = taskDelegate;
			Id           = GetNextId();

			RaiseEvent(Created);
			AddThis();
			Task = Task.Run(TaskRunInternal);
		}

		/// <summary>
		///  What event types messages should be logged for
		/// </summary>
		public static TrackedTaskEvent EnabledLogEvents { get; set; } = 0
																		| Error
			// | Success
			// | Created
			// | Disposed
			// | ProgressUpdated
			;

		/// <inheritdoc cref="Instances"/>
		// ReSharper disable once InconsistentlySynchronizedField
		public static IReadOnlyCollection<TrackedTask> UnsafeInstances => Instances;

		/// <summary>
		///  Gets the id of this instance
		/// </summary>
		public Guid Id { get; init; }

		/// <summary>
		///  How far the task has progressed
		/// </summary>
		/// <remarks>
		///  0 means 'just started', as no progress has been made, and 1 means 'complete', as all the operations have been executed. Values outside of this range
		///  should
		/// </remarks>
		[ValueRange(0, 1)]
		public float Progress { get; private set; }

		/// <summary>
		///  The name of this <see cref="TrackedTask"/> instance
		/// </summary>
		public string Name { get; }

		/// <summary>
		///  The <see cref="System.Threading.Tasks.Task"/> that is associated with the current instance
		/// </summary>
		public Task Task { get; private set; }

		/// <summary>
		///  The <see cref="TrackedTaskDelegate"/> that this instance is running
		/// </summary>
		public TrackedTaskDelegate TaskDelegate { get; }

		#region Instance collection methods

			/// <summary>
			/// Copies the instances list to an <see cref="ArrayPool{T}"/> array, and then returns the segment of that array that contains items
			/// </summary>
			/// <remarks>
			///	<i><b>IMPORTANT:</b></i>
			/// The <see cref="ArraySegment{T}.Array"/> contained in the <see cref="ArraySegment{T}"/> MUST BE RETURNED TO THE POOL after being processed, otherwise this method is useless and worse than a simple clone
			/// </remarks>
			public static ArraySegment<TrackedTask> ToArrayPoolArray()
			{
				lock (GlobalLock)
				{
					//Figure out how many we actually have
					int count = Instances.Count;
					//Now get an array of at least that size
					TrackedTask[] array = ArrayPool<TrackedTask>.Shared.Rent(count);
					//Start at the offset and keep moving while we have items left
					int i = 0;
					foreach (TrackedTask task in Instances)
					{
						array[i] = task;
						i++;
					}

					return new ArraySegment<TrackedTask>(array, 0, count);
				}
			}
		/// <summary>
		///  Copies the list of currently running <see cref="TrackedTask"/> instances to an <paramref name="array"/>, starting at the specified offset
		/// </summary>
		public static void CopyInstancesTo(TrackedTask[] array, int offset = 0)
		{
			lock (GlobalLock)
			{
				//Start at the offset and keep moving while we have items left
				int i = offset;
				foreach (TrackedTask task in Instances)
				{
					array[i] = task;
					i++;
				}
			}
		}

		/// <summary>
		///  An array that encompasses all the currently running tracked task instances
		/// </summary>
		/// <remarks>
		///  This collection is guaranteed to not be mutated internally, as it clones the <see cref="UnsafeInstances"/> collection
		/// </remarks>
		public static TrackedTask[] CloneAllInstances()
		{
			lock (GlobalLock)
			{
				return Instances.ToArray();
			}
		}

		#endregion

		#if INCREMENT_ID
		/// <summary>
		///  The order that bytes in a <see cref="Guid"/> are read from an array
		/// </summary>
		private static readonly int[] GuidByteOrder = { 15, 14, 13, 12, 11, 10, 9, 8, 6, 7, 4, 5, 0, 1, 2, 3 };

		/// <summary>
		///  The bytes that are used for creating task Guids
		/// </summary>
		private static readonly byte[] TaskCounterBytes = new byte[16];

		private static Guid GetNextId()
		{
			lock (TaskCounterBytes) //Thread safety
			{
				//Loop over the bytes, in the order we need to increment them for the GUID to look nice
				foreach (int byteIndex in GuidByteOrder)
				{
					//The byte that we're going to increment
					ref byte b = ref TaskCounterBytes[byteIndex];
					//If the byte isn't max (255), then we can safely increase it without overflowing
					if (b != byte.MaxValue)
					{
						b++;   //Increment it
						break; //And break out of the loop (so we don't modify any more bytes)
					}

					//else //Byte is max (255)
					{
						b = 0; //Set the byte to 0
						//continue; //And move on to the next byte (try increment it next loop)
					}
				}

				//Bytes are incremented nicely, return them as a GUID
				return new Guid(TaskCounterBytes);
			}
		}
		#else
		private static Guid GetNextId()
		{
			return Guid.NewGuid();
		}
		#endif

		/// <summary>
		/// The method that is used to run the actual work item, called by <see cref="System.Threading.Tasks.Task.Run(Func{System.Threading.Tasks.Task})"/>
		/// </summary>
		private async Task TaskRunInternal()
		{
			UpdateThisInstanceProgress(0);
			try
			{
				//Call the user task delegate
				await TaskDelegate(
						//Essentially, evey time the user calls updateProgress (the parameter)
						//We update our property
						UpdateThisInstanceProgress
				);
				RaiseEvent(Success);
			}
			catch (Exception e)
			{
				//Set the exception that was thrown
				Task = Task.FromException(e);
				RaiseEvent(Error);
			}
			finally
			{
				UpdateThisInstanceProgress(1);
				//Now mark as disposed because the user work has completed
				Dispose();
			}
		}

		// ReSharper disable once InconsistentNaming
		//I purposefully want the name to be wrong so I don't get confused
		private void UpdateThisInstanceProgress(float _progress)
		{
			if (isDisposed) throw new ObjectDisposedException(ToString());
			RaiseEvent(ProgressUpdated);
			Progress = _progress;
		}

		/// <summary>
		///  Returns the awaiter for this instance
		/// </summary>
		[PublicAPI]
		public TaskAwaiter GetAwaiter()
		{
			return Task.GetAwaiter();
		}

		private void Dispose()
		{
			if (isDisposed) return;
			isDisposed = true;
			RaiseEvent(Disposed);
			RemoveThis();
			Task = null!;
		}

	#region Helper

		private void RemoveThis()
		{
			lock (GlobalLock)
			{
				Instances.TryRemove(this);
			}
		}

		private void AddThis()
		{
			lock (GlobalLock)
			{
				Instances.Add(this);
			}
		}

		private void RaiseEvent(TrackedTaskEvent evt)
		{
			//If the flag is not enabled, do nothing
			if ((EnabledLogEvents & evt) == 0) return;
			switch (evt)
			{
				case Created:
					Log.Verbose("{Task:NoProgress} created", this);
					break;
				case Disposed:
					Log.Verbose("{Task:NoProgress} disposed", this);
					break;
				case Error:
					Log.Warning(Task.Exception, "{Task:NoProgress} threw exception", this);
					break;
				case Success:
					Log.Verbose("{Task:NoProgress} completed successfully", this);
					break;
				case ProgressUpdated:
					Log.Verbose("{Task:NoProgress} progress update ({Progress,6:00.0%})", this, Progress);
					break;
				case None:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(evt), evt, null);
			}
		}

	#endregion

	#region ToString()

		/// <summary>
		///  A cached <see cref="Format"/> for default <see cref="ToString()"/> behaviour
		/// </summary>
		private static readonly Format DefaultToStringFormat = Smart.Default.Parser.ParseFormat("TrackedTask {Name,-20} Id {Id} ({Progress,6:00.0%})");

		/// <summary>
		///  A cached <see cref="Format"/> for <see cref="ToString()"/> behaviour without specifying the progress of the task
		/// </summary>
		private static readonly Format NoProgressToStringFormat = Smart.Default.Parser.ParseFormat("TrackedTask {Name,-20} Id {Id}");

		/// <inheritdoc/>
		public override string ToString()
		{
			return ToString(DefaultToStringFormat);
		}

		/// <inheritdoc/>
		/// <remarks>
		///  Uses <see cref="SmartFormat"/> format strings for formatting. If the specified <paramref name="format"/> is equal to "NoProgress", the progress will
		///  not be included when formatting
		/// </remarks>
		public string ToString(string? format, IFormatProvider? formatProvider = null)
		{
			return format switch
			{
					null         => ToString(DefaultToStringFormat,                    formatProvider),
					"NoProgress" => ToString(NoProgressToStringFormat,                 formatProvider),
					_            => ToString(Smart.Default.Parser.ParseFormat(format), formatProvider)
			};
		}

		/// <inheritdoc cref="ToString()"/>
		public string ToString(Format format, IFormatProvider? formatProvider = null)
		{
			return Smart.Default.Format(formatProvider, format, this);
		}

	#endregion
	}

	// /// <summary>
	// ReSharper disable CommentTypo
	// /// An awaiter for the <see cref="TrackedTask"/> type, allowing use of the <see langword="await"/> keyword
	// /// </summary>
	// public readonly struct TrackedTaskAwaiter : INotifyCompletion
	// {
	// 	private readonly TrackedTask instance;
	//
	// 	/// <summary>
	// 	/// Constructs a new <see cref="TrackedTaskAwaiter"/> for the <see cref="TrackedTask"/> <paramref name="instance"/>
	// 	/// </summary>
	// 	/// <param name="instance">The <see cref="TrackedTask"/> to create the awaiter for</param>
	// 	public TrackedTaskAwaiter(TrackedTask instance)
	// 	{
	// 		this.instance = instance;
	// 	}
	//
	// 	/// <inheritdoc />
	// 	public void OnCompleted(Action continuation)
	// 	{
	// 		Task.Run(continuation);
	// 	}
	//
	// 	/// <summary>
	// 	/// Gets the result for this awaitable instance
	// 	/// </summary>
	// 	/// <remarks>Does nothing under the hood (empty method body)</remarks>
	// 	[PublicAPI]
	// 	#pragma warning disable CA1822
	// 	public void GetResult()
	// 	{
	// 	}
	// 	#pragma warning restore CA1822
	//
	// 	// ReSharper disable once CompareOfFloatsByEqualityOperator
	// 	/// <summary>
	// 	/// Gets whether the <see cref="TrackedTask"/> has completed
	// 	/// </summary>
	// 	[PublicAPI]
	// 	public bool IsCompleted => instance.Progress == 1f;
	// }
	// ReSharper restore CommentTypo
}