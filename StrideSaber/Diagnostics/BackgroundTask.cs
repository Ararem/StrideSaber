using ConcurrentCollections;
using JetBrains.Annotations;
using Serilog;
using ServiceWire.ZeroKnowledge;
using Stride.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BigInteger = System.Numerics.BigInteger;

namespace StrideSaber.Diagnostics
{
	/// <summary>
	/// A delegate type for a <see cref="BackgroundTask"/>
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
	public delegate Task BackgroundTaskDelegate(Action<float> updateProgress);

	//TODO: IDisposable (or async)
	/// <summary>
	/// A task-like type that can be used to create tasks whose progress can be tracked and displayed to the user
	/// </summary>
	public sealed class BackgroundTask
	{
		/// <summary>
		/// If messages should be logged when instances are created and destroyed (completed), or an error occurs
		/// </summary>
		public static bool LogInternalEvents { get; set; } = false;

		/// <summary>
		/// An object that can be locked upon for global (static) synchronisation
		/// </summary>
		private static readonly object GlobalLock = new();

		/// <summary>
		/// A <see cref="IReadOnlyCollection{T}">collection</see> that encompasses all the currently running background task instances
		/// </summary>
		/// <remarks>This value should be considered mutable and may change while being accessed, so should not be accessed directly</remarks>
		private static readonly ConcurrentHashSet<BackgroundTask> Instances = new();

		/// <summary>
		/// A <see cref="IReadOnlyCollection{T}">collection</see> that encompasses all the currently running background task instances
		/// </summary>
		/// <remarks>
		/// This collection is guaranteed to not be mutated internally
		/// </remarks>
		public static IReadOnlyCollection<BackgroundTask> CloneAllInstances()
		{
			lock (GlobalLock)
			{
				return Instances.ToArray();
			}
		}

		/// <inheritdoc cref="Instances"/>
		public static IReadOnlyCollection<BackgroundTask> UnsafeInstances
		{
			get
			{
				lock (GlobalLock)
				{
					return Instances;
				}
			}
		}

		/// <summary>
		/// Gets the id of this instance
		/// </summary>
		public Guid Id { get; init; }

		private float progress;

		/// <summary>
		/// How far the task has progressed
		/// </summary>
		/// <remarks> 0 means 'just started', as no progress has been made, and 1 means 'complete', as all the operations have been executed. Values outside of this range should </remarks>
		[ValueRange(0, 1)]
		public float Progress
		{
			get
			{
				//Nifty way to return the progress while clamping it at the same time
				return progress = progress switch
				{
						< 0 => 0,
						> 1 => 1,
						_   => progress
				};
			}
			private set
			{
				//Due to some issues with floating point approximation, I've decided to ignore throwing and just internally clamp
				value = Math.Clamp(value, 0, 1);
				// if (value is < 0 or >1)
				// throw new ArgumentOutOfRangeException(nameof(value), value, "The given value must be in the range of [0..1] (inclusive)");
				progress = value;
			}
		}

		/// <summary>
		/// The name of this <see cref="BackgroundTask"/> instance
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructs a new <see cref="BackgroundTask"/>, with a specified <paramref name="name"/>
		/// </summary>
		/// <param name="name">The name of this <see cref="BackgroundTask"/></param>
		/// <param name="taskDelegate">The <see cref="BackgroundTaskDelegate"/> function to be executed</param>
		public BackgroundTask(string name, BackgroundTaskDelegate taskDelegate)
		{
			Name = name;
			//awaiter = new BackgroundTaskAwaiter(this);
			if (LogInternalEvents)
				Log.Verbose("Created new BackgroundTask {Task}", this);
			AddThis();
			TaskDelegate = taskDelegate;
			Task = Task.Run(TaskRunInternal);
			Id = GetNextId();
		}

		public static Guid GetNextId()
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
					else //Byte is max (255)
					{
						b = 0; //Set the byte to 0
						continue; //And move on to the next byte (try increment it next loop)
					}
				}

				//Bytes are incremented nicely, return them as a GUID
				return new Guid(TaskCounterBytes);
			}
		}

		/// <summary>
		/// The order that bytes in a <see cref="Guid"/> are read from an array
		/// </summary>
		private static readonly int[] GuidByteOrder = { 15, 14, 13, 12, 11, 10, 9, 8, 6, 7, 4, 5, 0, 1, 2, 3 };

		/// <summary>
		/// 
		/// </summary>
		private static readonly byte[] TaskCounterBytes = new byte[16];

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
			}
			catch (Exception e)
			{
				if (LogInternalEvents)
					Log.Warning(e, "Caught exception when running task {BackgroundTask}", this);
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
			Progress = _progress;
		}

		/// <summary>
		/// The <see cref="System.Threading.Tasks.Task"/> that is associated with the current instance
		/// </summary>
		public Task Task { get; private set; }

		/// <summary>
		/// The <see cref="BackgroundTaskDelegate"/> that this instance is running
		/// </summary>
		public BackgroundTaskDelegate TaskDelegate { get; private set; }

		/// <summary>
		/// Returns the awaiter for this instance
		/// </summary>
		[PublicAPI]
		public TaskAwaiter GetAwaiter()
		{
			return Task.GetAwaiter();
		}

		// /// <summary>
		// /// The cached awaiter for this instance
		// /// </summary>
		// private readonly BackgroundTaskAwaiter awaiter;

		private bool isDisposed = false;

		private void Dispose()
		{
			if (isDisposed) return;
			if (LogInternalEvents)
				Log.Verbose("Disposing BackgroundTask {Task}", this);
			isDisposed = true;
			RemoveThis();
			Task = null!;
		}

	#region Helper

		private void RemoveThis()
		{
			if (LogInternalEvents)
				Log.Verbose("Removing BackgroundTask {Task}", this);
			lock (GlobalLock)
			{
				Instances.TryRemove(this);
			}
		}

		private void AddThis()
		{
			if (LogInternalEvents)
				Log.Verbose("Adding BackgroundTask {Task}", this);
			lock (GlobalLock)
			{
				Instances.Add(this);
			}
		}

	#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			return $"BackgroundTask \"{Name}\" Id \"{Id}\": {Progress:p0} complete";
		}

		/// <summary>
		/// The same as <see cref="ToString"/> but includes a progress bar
		/// </summary>
		public string ToStringBar()
		{
			const int progressSegments = 25;
			const char fullChar = '=';
			const char emptyChar = '-';
			Span<char> progressBar = stackalloc char[progressSegments];
			int lastFullChar = (int)MathF.Floor((Progress) * progressSegments);
			for (int i = 0; i < progressSegments; i++)
					//Fill depending on if we've reached the last full segment yet
				progressBar[i] = i <= lastFullChar ? fullChar : emptyChar;
			return $"BackgroundTask \"{Name}\"\tId \"{Id}\":\t{Progress:p0} complete\t[{new string(progressBar)}]";
		}
	}

	// /// <summary>
	// ReSharper disable CommentTypo
	// /// An awaiter for the <see cref="BackgroundTask"/> type, allowing use of the <see langword="await"/> keyword
	// /// </summary>
	// public readonly struct BackgroundTaskAwaiter : INotifyCompletion
	// {
	// 	private readonly BackgroundTask instance;
	//
	// 	/// <summary>
	// 	/// Constructs a new <see cref="BackgroundTaskAwaiter"/> for the <see cref="BackgroundTask"/> <paramref name="instance"/>
	// 	/// </summary>
	// 	/// <param name="instance">The <see cref="BackgroundTask"/> to create the awaiter for</param>
	// 	public BackgroundTaskAwaiter(BackgroundTask instance)
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
	// 	/// Gets whether the <see cref="BackgroundTask"/> has completed
	// 	/// </summary>
	// 	[PublicAPI]
	// 	public bool IsCompleted => instance.Progress == 1f;
	// }
	// ReSharper restore CommentTypo
}