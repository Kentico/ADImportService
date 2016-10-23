using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Kentico.ADImportService
{
	/// <summary>
	/// Basic REST requests provider.
	/// </summary>
	public class RestProvider
	{
		public long RequestCounter { get; set; }

		public Encoding Encoding
		{
			get;
			private set;
		}


		public string Password
		{
			get;
			private set;
		}


		public string UserName
		{
			get;
			private set;
		}


		public X509Certificate Certificate
		{
			get;
			private set;
		}


		/// <summary>
		/// Default constructor for REST provider.
		/// </summary>
		/// <param name="encoding">Data encoding.</param>
		/// <param name="userName">User name to authorize request with.</param>
		/// <param name="password">Password to authorize request with.</param>
		/// <param name="certificatePath">Path to the ceritification file.</param>
		public RestProvider(Encoding encoding, string userName, string password, string certificatePath = null)
		{
			Encoding = encoding;
			UserName = userName;
			Password = password;
			RequestCounter = 0;

			if (!string.IsNullOrEmpty(certificatePath))
			{
				if (!File.Exists(certificatePath))
				{
					throw new ArgumentException("Provided path to certificate doesn't exist.");
				}

				// Load certificate
				Certificate = X509Certificate.CreateFromCertFile(certificatePath);
			}
		}


		/// <summary>
		/// Make REST request.
		/// </summary>
		/// <param name="target">Target URL. Contains names of objects to be requested.</param>
		/// <param name="method">HTTP method. Reflects type of operation required [add/delete/get].</param>
		/// <param name="parameters">REST request parameters.</param>
		/// <param name="data">Data to send to server. Only appropriate when making POST request.</param>
		/// <returns>Response string representation</returns>
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")] // http://stackoverflow.com/questions/3831676/ca2202-how-to-solve-this-case
		public string MakeRequest(string target, HttpVerb method = HttpVerb.Get, Dictionary<string, string> parameters = null, string data = null)
		{
			if ((parameters != null) && parameters.Any())
			{
				target += "?";

				// Append all URL parameters
				parameters.Aggregate(target, (current, parameter) => current + string.Format("{0}={1}", parameter.Key, parameter.Value));
			}

			var request = (HttpWebRequest)WebRequest.Create(target);

			// Set request header
			request.Method = method.ToString().ToUpper();
			request.ContentLength = 0;
			request.ContentType = "text\\xml";

			if (target.Contains("https://"))
			{
				request.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
				{
					return certificate.Equals(Certificate);
				};
			}

			// Set authorization header
			var credentials = Convert.ToBase64String(Encoding.GetBytes(string.Format("{0}:{1}", UserName, Password)));
			request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + credentials);

			// Append post data
			if (!string.IsNullOrEmpty(data) && ((method == HttpVerb.Post) || (method == HttpVerb.Put)))
			{
				var bytes = Encoding.GetBytes(data);
				request.ContentLength = bytes.Length;

				using (var writeStream = request.GetRequestStream())
				{
					writeStream.Write(bytes, 0, bytes.Length);
				}
			}

			// Make request
			HttpWebResponse response = null;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				response = (HttpWebResponse)ex.Response;
			}

			RequestCounter++;

			if (response != null)
			{
				switch (response.StatusCode)
				{
					case HttpStatusCode.Created:
					case HttpStatusCode.OK:
						// Read the response
						using (var responseStream = response.GetResponseStream())
						{
							if (responseStream != null)
							{
								using (var reader = new StreamReader(responseStream))
								{
									return reader.ReadToEnd();
								}
							}
						}
						break;

					case HttpStatusCode.BadRequest:
						// Build error string
						var error = new StringBuilder();
						error.AppendLine("Bad request. The data was: " + data);
						throw new InvalidOperationException(error.ToString());

					case HttpStatusCode.NotFound:
						throw new InstanceNotFoundException("Object doesn't exist.");

					default:
						var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
						throw new ApplicationException(message);
				}
			}

			return null;
		}


		/// <summary>
		/// Validate certificate.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="certificate">Certificate.</param>
		/// <param name="certificateChain">Certificate chain.</param>
		/// <param name="policyErrors">Returned certificate errors.</param>
		private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain certificateChain, SslPolicyErrors policyErrors)
		{
			// Allow certificate when no errors
			if (policyErrors == SslPolicyErrors.None)
			{
				return true;
			}

			// If there are errors in the certificate chain, look at each error to determine the cause.
			if ((certificateChain == null) || ((policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == 0))
			{
				return false;
			}

			foreach (X509ChainStatus status in certificateChain.ChainStatus)
			{
				// Accept self-signed certificates
				if ((certificate.Subject != certificate.Issuer) || (status.Status != X509ChainStatusFlags.UntrustedRoot))
				{
					// Accept certificate only if no other error
					return (status.Status == X509ChainStatusFlags.NoError);
				}
			}

			return true;
		}
	}
}
