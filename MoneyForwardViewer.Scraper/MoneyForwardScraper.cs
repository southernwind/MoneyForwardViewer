using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using HtmlAgilityPack.CssSelectors.NetCore;

using MoneyForwardViewer.DataBase;
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
		public async Task<MfTransaction[]> GetTransactions() {
			var result = new List<MfTransaction>();
			await this.LoginAsync();
			var now = DateTime.Now;
			for (var year = now.Year; ; year--) {
				for (var month = now.Year == year ? now.Month : 12; month >= 1; month--) {
					this._hcw.CookieContainer.Add(new Cookie("cf_last_fetch_from_date", $"{year}/{month:D2}/01", "/",
						"moneyforward.com"));
					var htmlDoc = await this._hcw.GetDocumentAsync("https://moneyforward.com/cf");
					var dateRange = htmlDoc.DocumentNode.QuerySelector(".date_range h2").InnerText;
					if (!dateRange.StartsWith($"{year}/{month:D2}")) {
						return result.ToArray();
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

					result.AddRange(list);
				}
			}
		}

		/// <summary>
		/// 資産推移の取得
		/// </summary>
		/// <returns></returns>
		public async Task<MfAsset[]> GetAssets() {
			var result = new List<MfAsset>();
			await this.LoginAsync();
			for (var date = DateTime.Now.Date; ; date = date.AddDays(-1)) {
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

				if (!list.Any()) {
					return result.ToArray();
				}

				result.AddRange(list);
			}
		}

		/// <summary>
		/// ログインする。
		/// </summary>
		private async Task LoginAsync() {
			var htmlDoc = await this._hcw.GetDocumentAsync("https://moneyforward.com/users/sign_in");

			var token = htmlDoc.DocumentNode.QuerySelector(@"input[name='authenticity_token']").GetAttributeValue("value", null);

			var content = new FormUrlEncodedContent(new Dictionary<string, string> {
				{ "utf8", "✓"},
				{ "authenticity_token", token },
				{ "sign_in_session_service[email]", this._id },
				{ "sign_in_session_service[password]", this._password },
				{ "commit",  "ログイン"}
			});
			await this._hcw.PostAsync("https://moneyforward.com/session", content);
		}
	}
}