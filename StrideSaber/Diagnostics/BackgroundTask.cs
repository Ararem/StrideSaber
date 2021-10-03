using ConcurrentCollections;
using JetBrains.Annotations;
using Stride.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
	public delegate void BackgroundTaskDelegate(Action<float> updateProgress);

	//TODO: IDisposable (or async)
	public class BackgroundTask : IIdentifiable, IAsyncDisposable
	{
		/// <summary>
		/// An object that can be locked upon for global (static) synchronisation
		/// </summary>
		private static readonly object GlobalLock = new();

		/// <summary>
		/// A <see cref="IReadOnlyCollection{T}">collection</see> that encompasses all the currently running background task instances
		/// </summary>
		/// <remarks>This value should be considered mutable and may change while being accessed, so should not be accessed directly</remarks>
		private static readonly ConcurrentHashSet<BackgroundTask> Instances = new();

		private static bool instancesDirty = false;

		/// <summary>
		/// A field that caches the array version of our instances so that we don't allocate a new one each time
		/// </summary>
		private static BackgroundTask[] cachedInstancesArray = Array.Empty<BackgroundTask>();

		/// <summary>
		/// A <see cref="IReadOnlyCollection{T}">collection</see> that encompasses all the currently running background task instances
		/// </summary>
		public static IReadOnlyCollection<BackgroundTask> GetAllInstances()
		{
			lock (GlobalLock)
			{
				//Update our cache 
				if (instancesDirty)
				{
					cachedInstancesArray = Instances.ToArray();
					instancesDirty = false;
				}

				return cachedInstancesArray;
			}
		}

		/// <inheritdoc />
		public Guid Id { get; set; } = Guid.NewGuid();

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
			protected set
			{
				if (value is < 0 or >1)
					throw new ArgumentOutOfRangeException(nameof(value), value, "The given value must be in the range of [0..1] (inclusive)");
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
			lock (GlobalLock)
			{
				Instances.Add(this);
			}

			Task = null;
			Name = name;
			awaiter = new BackgroundTaskAwaiter(this);
			this.taskDelegate = taskDelegate;
		}

		public void Run()
		{
			if (isDisposed) throw new ObjectDisposedException(Name);
			Task = Task.Run(async delegate
			{
				//Call the user task delegate
				taskDelegate(
						//Essentially, evey time the user calls updateProgress (the parameter)
						//We update our property
						UpdateThisInstanceProgress
				);
				//Now mark as disposed because the user work has completed
				await DisposeAsync();
			});
		}

		private void UpdateThisInstanceProgress(float progress)
		{
			if (isDisposed) throw new ObjectDisposedException(ToString());
			Progress = progress;
			lock (GlobalLock)
			{
				instancesDirty = true;
			}
		}

		/// <summary>
		/// The <see cref="System.Threading.Tasks.Task"/> that is associated with the current instance
		/// </summary>
		public Task? Task { get; private set; }

		/// <summary>
		/// The delegate that will be run in the background thread
		/// </summary>
		private readonly BackgroundTaskDelegate taskDelegate;

		/// <summary>
		/// Returns the awaiter for this instance
		/// </summary>
		[PublicAPI]
		public BackgroundTaskAwaiter GetAwaiter()
		{
			return awaiter;
		}

		/// <summary>
		/// The cached awaiter for this instance
		/// </summary>
		private readonly BackgroundTaskAwaiter awaiter;

		private bool isDisposed = false;

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			if (isDisposed) return;
			isDisposed = true;
			RemoveThis();
			Task = null!;
		}

	#region Helper

		private void RemoveThis()
		{
			lock (GlobalLock)
			{
				Instances.TryRemove(this);
				instancesDirty = true;
			}
		}

		private void AddThis()
		{
			lock (GlobalLock)
			{
				Instances.Add(this);
				instancesDirty = true;
			}
		}

	#endregion
	}

	/// <summary>
	/// An awaiter for the <see cref="BackgroundTask"/> type, allowing use of the <see langword="await"/> keyword
	/// </summary>
	public readonly struct BackgroundTaskAwaiter : INotifyCompletion
	{
		private readonly BackgroundTask instance;

		/// <summary>
		/// Constructs a new <see cref="BackgroundTaskAwaiter"/> for the <see cref="BackgroundTask"/> <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The <see cref="BackgroundTask"/> to create the awaiter for</param>
		public BackgroundTaskAwaiter(BackgroundTask instance)
		{
			this.instance = instance;
		}

		/// <inheritdoc />
		public void OnCompleted(Action continuation)
		{
			Task.Run(continuation);
		}

		/// <summary>
		/// Gets the result for this awaitable instance
		/// </summary>
		/// <remarks>Does nothing under the hood (empty method body)</remarks>
		[PublicAPI]
		#pragma warning disable CA1822
		public void GetResult()
		{
		}
		#pragma warning restore CA1822

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		/// <summary>
		/// Gets whether the <see cref="BackgroundTask"/> has completed
		/// </summary>
		[PublicAPI]
		public bool IsCompleted => instance.Progress == 1f;
	}
}