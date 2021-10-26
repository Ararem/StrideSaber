using Serilog;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using StrideSaber.Diagnostics;
using System.Threading.Tasks;

namespace StrideSaber.SceneManagement
{
	public static class SceneUtils
	{
		public static async Task LoadSceneAsync(this ContentManager contentManager, UrlReference<Scene> sceneRef)
		{
			Log.Information("Asynchronously loading scene {Scene} ", sceneRef);
			BackgroundTask task = new(
					$"LoadScene {sceneRef.Url}",
					_ => contentManager.LoadAsync(sceneRef, ContentManagerLoaderSettings.Default)
					);
			await task;
			Log.Information("Asynchronously loaded Scene {Scene}", sceneRef);
		}

		public static async Task UnloadSceneAsync(this ContentManager contentManager, Scene scene)
		{
			Log.Information("Asynchronously unloading scene {Scene} ", scene);
			BackgroundTask task = new(
					$"UnloadScene {scene}",
					_ =>
					{
						contentManager.Unload(scene);
						return Task.CompletedTask;
					});
			await task;
			Log.Information("Asynchronously loaded Scene {Scene}", scene);
		}
	}
}