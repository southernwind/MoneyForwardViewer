using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace MoneyForwardViewer.Scraper {
	internal class HttpClientWrapper {
		/// <summary>
		/// HttpClient
		/// </summary>
		private readonly HttpClient _hc;

		/// <summary>
		/// Cookie
		/// </summary>
		public CookieContainer CookieContainer {
			get;
		}

		public HttpClientWrapper() {
			this.CookieContainer = new CookieContainer();
			this._hc = new HttpClient(new HttpClientHandler {
				CookieContainer = this.CookieContainer
			});
		}

		/// <summary>
		/// 引数で渡されたURLのHTMLDocumentを取得する(GET)
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>取得したHTMLDocument</returns>
		public async Task<HtmlDocument> GetDocumentAsync(string url) {
			var uri = new Uri(url);
			var request = new HttpRequestMessage {
				Method = HttpMethod.Get,
				RequestUri = uri
			};
			this.SetHeaders(request);

			var response = await this._hc.SendAsync(request);
			var html = await response.Content.ReadAsStringAsync();

			var hd = new HtmlDocument();
			hd.LoadHtml(html);
			return hd;
		}

		/// <summary>
		/// Postする。
		/// </summary>
		/// <param name="url">URL</param>
		/// <param name="content">要求本文</param>
		public async Task<HtmlDocument> PostAsync(string url, HttpContent content) {
			var uri = new Uri(url);
			var request = new HttpRequestMessage {
				Method = HttpMethod.Post,
				RequestUri = uri,
				Content = content
			};
			this.SetHeaders(request);

			var response = await this._hc.SendAsync(request);
			var html = await response.Content.ReadAsStringAsync();

			var hd = new HtmlDocument();
			hd.LoadHtml(html);
			return hd;
		}

		private void SetHeaders(HttpRequestMessage request) {
			request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
			request.Headers.Add("Accept-Language", "ja");
			request.Headers.Add("Connection", "Keep-Alive");
			request.Headers.Add("Accept", "text/html, application/xhtml+xml, application/xml; q=0.9, */*; q=0.8");
		}
	}
}
