using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using StrideSaber.Diagnostics;
using System.Threading.Tasks;

namespace StrideSaber.SceneManagement
{
	public static class SceneUtils
	{
		public static async Task LoadSceneAsync(this UrlReference<Scene> sceneRef, ContentManager contentManager)
		{
			BackgroundTask task = new(
					$"LoadScene {sceneRef.Url}",
					_ => contentManager.LoadAsync(sceneRef, ContentManagerLoaderSettings.Default));
		}
	}
}