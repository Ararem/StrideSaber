﻿using Serilog;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using StrideSaber.Diagnostics;
using System.Threading.Tasks;

namespace StrideSaber.SceneManagement
{
	/// <summary>
	/// A helper class for <see cref="Scene"/> management
	/// </summary>
	public static class SceneUtils
	{
		/// <summary>
		/// Asynchronously loads the specified <paramref name="sceneReference"/>, tracking the load progress
		/// </summary>
		/// <param name="contentManager">The <see cref="ContentManager"/> that will be used to load the </param>
		/// <param name="sceneSystem">The <see cref="SceneSystem"/> that the loaded scene will be attached to</param>
		/// <param name="sceneReference">The reference to the <see cref="Scene"/> to load</param>
		public static async Task LoadSceneAsync(ContentManager contentManager, SceneSystem sceneSystem, UrlReference<Scene> sceneReference)
		{
			Log.Information("Asynchronously loading scene {Scene} ", sceneReference);
			Task<Scene> sceneTask = null!;
			TrackedTask trackedTask = new(
					$"LoadSceneAsync {sceneReference.Url}",
					_ => sceneTask = contentManager.LoadAsync(sceneReference, ContentManagerLoaderSettings.Default)
			);
			//Wait for the task to complete
			await trackedTask;
			//And pull out the resulting scene that was loaded
			Scene newScene = await sceneTask;
			Log.Information("Asynchronously loaded Scene {SceneRef}: {Scene}", sceneReference, newScene);
			//Add it to the hierarchy so that it's enabled and shown
			Log.Information("Adding newly loaded scene {Scene} to hierarchy", newScene);
			sceneSystem.SceneInstance.RootScene.Children.Add(newScene);
			Log.Information("Added newly loaded scene {Scene} to hierarchy", newScene);
		}

		/// <summary>
		/// Asynchronously unloads the specified <paramref name="scene"/>, tracking the unload progress
		/// </summary>
		/// <param name="contentManager">The <see cref="ContentManager"/> that will be used to unload the scene</param>
		/// <param name="scene">The <see cref="Scene"/> to unload</param>
		public static async Task UnloadSceneAsync(ContentManager contentManager, Scene scene)
		{
			Log.Information("Asynchronously unloading scene {Scene} ", scene);
			TrackedTask trackedTask = new(
					$"UnloadSceneAsync {scene}",
					_ =>
					{
						contentManager.Unload(scene);
						return Task.CompletedTask;
					});
			await trackedTask;
			Log.Information("Asynchronously unloaded Scene {Scene}", scene);

			//Now get rid of it in the hierarchy
			Log.Information("Removing unloaded scene {Scene} from hierarchy", scene);
			scene.Parent.Children.Remove(scene);
			Log.Information("Removed unloaded scene {Scene} from hierarchy", scene);
		}
	}
}