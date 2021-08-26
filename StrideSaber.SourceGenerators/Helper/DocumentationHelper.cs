using Microsoft.CodeAnalysis;

namespace StrideSaber.SourceGenerators.Helper
{
	public static class DocumentationHelper
	{
		/// <summary>
		/// Returns an XML inheritdoc for the symbol. Does not include the preceding triple slash ('///')
		/// </summary>
		/// <param name="symbol">The symbol to reference</param>
		public static string Inheritdoc(ISymbol symbol)
		{
			return $@"<inheritdoc cref=""{symbol.GetDocumentationCommentId()}""/>";
		}
	}
}