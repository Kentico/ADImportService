using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kentico.ADImportService
{
	/// <summary>
	/// Provides basic operation for manipulation with REST reposnes.
	/// </summary>
	public class RestHelper
	{
		/// <summary>
		/// Get value of given attribute from REST response.
		/// </summary>
		/// <param name="response">Complete REST response.</param>
		/// <param name="attributeName">Object attribute to retrieve.</param>
		public static string GetAttributeFromReponse(string response, string attributeName)
		{
			return GetAttributesFromReponse(response, attributeName).FirstOrDefault();
		}


		/// <summary>
		/// Get values of given attribute from REST response.
		/// </summary>
		/// <param name="response">Complete REST response.</param>
		/// <param name="attributeName">Object attribute to retrieve.</param>
		public static IEnumerable<string> GetAttributesFromReponse(string response, string attributeName)
		{
			if (!string.IsNullOrEmpty(response) && !string.IsNullOrEmpty(attributeName))
			{
				Regex idRegex = new Regex(string.Format(@"\<{0}\>(?<id>[^\<\>]*)\</{0}\>", attributeName), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				return idRegex.Matches(response).Cast<Match>().Select(m => m.Groups["id"].Value).ToList();
			}

			return new List<string>();
		}
	}
}
