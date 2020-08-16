using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using HtmlAgilityPack.CssSelectors.NetCore;

using MoneyForwardViewer.DataBase.Tables;

namespace MoneyForwardViewer.Scraper {
	public class MoneyForwardScraper {

		private readonly string _id;
		private readonly string _password;
		private readonly HttpClientWrapper _hcw;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="id">MoneyForwardのID(メールアドレス)</param>
		/// <param name="password">MoneyForwardのパスワード</param>
		public MoneyForwardScraper(string id, string password) {
			this._id = id;
			this._password = password;
			this._hcw = new HttpClientWrapper();
		}

		/// <summary>
		/// 取引履歴の取得
		/// </summary>
		/// <returns></returns>
		public async IAsyncEnumerable<MfTransaction[]> GetTransactions(DateTime from, DateTime to) {
			await this.LoginAsync();
			for (var date = from; date <= to; date = new DateTime(date.AddMonths(1).Year, date.AddMonths(1).Month, 1)) {
				var year = date.Year;
				var month = date.Month;
				this._hcw.CookieContainer.Add(new Cookie("cf_last_fetch_from_date", $"{year}/{month:D2}/01", "/",
					"moneyforward.com"));
				var htmlDoc = await this._hcw.GetDocumentAsync("https://moneyforward.com/cf");
				var dateRange = htmlDoc.DocumentNode.QuerySelector(".date_range h2").InnerText;
				if (!dateRange.StartsWith($"{year}/{month:D2}")) {
					continue;
				}

				var list = htmlDoc
					.DocumentNode
					.QuerySelectorAll(@".list_body .transaction_list")
					.Select(tr => tr.QuerySelectorAll("td"))
					.Select(tdList => new MfTransaction {
						TransactionId =
							tdList[0].QuerySelector("input#user_asset_act_id").GetAttributeValue("value", null),
						IsCalculateTarget = tdList[0].QuerySelector("i.icon-check") != null,
						Date = new DateTime(year, month, int.Parse(tdList[1].InnerText.Trim().Substring(3, 2))),
						Content = tdList[2].InnerText.Trim(),
						Amount = int.Parse(tdList[3].QuerySelector("span").InnerText.Trim().Replace(",", "")),
						Institution = tdList[4].GetAttributeValue("title", null),
						LargeCategory = tdList[5].InnerText.Trim(),
						MiddleCategory = tdList[6].InnerText.Trim(),
						Memo = tdList[7].InnerText.Trim()
					});

				if (list.Count() > 0) {
					yield return list.ToArray();
				}
			}
		}

		/// <summary>
		/// 資産推移の取得
		/// </summary>
		/// <returns></returns>
		public async IAsyncEnumerable<MfAsset[]> GetAssets(DateTime from, DateTime to) {
			await this.LoginAsync();
			for (var date = from; date <= to.AddDays(1); date = date.AddDays(1)) {
				var htmlDoc = await this._hcw.GetDocumentAsync($"https://moneyforward.com/bs/history/list/{date:yyyy-MM-dd}");
				var list = htmlDoc
					.DocumentNode
					.QuerySelectorAll(@"#history-list tbody tr")
					.Select(tr => tr.QuerySelectorAll("td"))
					.Select(tdList => new MfAsset {
						Date = date,
						Institution = tdList[0].InnerText.Trim(),
						Category = tdList[1].InnerText.Trim(),
						Amount = int.Parse(tdList[2].InnerText.Trim().Replace("円", "").Replace(",", ""))
					});
				if (list.Count() > 0) {
					yield return list.ToArray();
				}
			}
		}

		/// <summary>
		/// ログインする。
		/// </summary>
		private async Task LoginAsync() {
			var htmlDoc = await this._hcw.GetDocumentAsync("https://moneyforward.com/cf");
			if (!Regex.IsMatch(htmlDoc.Text, @"^.*gon\.authorizationParams=({.*?}).*$", RegexOptions.Singleline)) {
				// ログイン済みとみなす
				return;
			}
			var json = Regex.Replace(htmlDoc.Text, @"^.*gon\.authorizationParams=({.*?}).*$", "$1", RegexOptions.Singleline);
			var urlParams = JsonSerializer.Deserialize<UrlParams>(json);

			// メールアドレス入力画面
			htmlDoc = await this._hcw.GetDocumentAsync("https://id.moneyforward.com/sign_in/email?" + $"client_id={Encode(urlParams.ClientId)}&nonce={Encode(urlParams.Nonce)}&redirect_uri={Encode(urlParams.RedirectUri)}&response_type={Encode(urlParams.ResponseType)}&scope={Encode(urlParams.Scope)}&state={Encode(urlParams.State)}");

			json = Regex.Replace(htmlDoc.Text, @"^.*gon\.authorizationParams=({.*?}).*$", "$1", RegexOptions.Singleline);
			urlParams = JsonSerializer.Deserialize<UrlParams>(json);
			var token = htmlDoc.DocumentNode.QuerySelector(@"meta[name='csrf-token']").GetAttributeValue("content", null);

			// パスワード入力画面
			var content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "authenticity_token", token},
				{ "_method","post" },
				{ "client_id", urlParams.ClientId},
				{ "redirect_uri", urlParams.RedirectUri},
				{ "response_type", urlParams.ResponseType},
				{ "scope", urlParams.Scope},
				{ "state", urlParams.State},
				{ "nonce", urlParams.Nonce},
				{ "mfid_user[email]", this._id},
				{ "hiddenPassword","" }
			});
			htmlDoc = await this._hcw.PostAsync("https://id.moneyforward.com/sign_in/email", content);

			json = Regex.Replace(htmlDoc.Text, @"^.*gon\.authorizationParams=({.*?}).*$", "$1", RegexOptions.Singleline);
			urlParams = JsonSerializer.Deserialize<UrlParams>(json);
			token = htmlDoc.DocumentNode.QuerySelector(@"meta[name='csrf-token']").GetAttributeValue("content", null);

			content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "authenticity_token", token},
				{ "_method","post" },
				{ "client_id", urlParams.ClientId},
				{ "redirect_uri", urlParams.RedirectUri},
				{ "response_type", urlParams.ResponseType},
				{ "scope", urlParams.Scope},
				{ "state", urlParams.State},
				{ "nonce", urlParams.Nonce},
				{ "mfid_user[email]", this._id},
				{ "mfid_user[password]",this._password }
			});
			await this._hcw.PostAsync("https://id.moneyforward.com/sign_in", content);
		}

		private static string Encode(string text) {
			return HttpUtility.UrlEncode(text);
		}
		private class UrlParams {
			[JsonPropertyName("clientId")]
			public string ClientId {
				get;
				set;
			}
			[JsonPropertyName("redirectUri")]
			public string RedirectUri {
				get;
				set;
			}
			[JsonPropertyName("responseType")]
			public string ResponseType {
				get;
				set;
			}
			[JsonPropertyName("scope")]
			public string Scope {
				get;
				set;
			}
			[JsonPropertyName("state")]
			public string State {
				get;
				set;
			}
			[JsonPropertyName("nonce")]
			public string Nonce {
				get;
				set;
			}
		}
	}
}