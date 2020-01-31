# MoneyForwardViewer

MoneyForwardのデータをPower BIで読み込める形式にするAzure Functions。  

## Power BI版
Azure Functionsの環境変数に以下の値を設定する。  

| 変数名 | 値 |
|:---|:---|
| MAIL | MoneyForwardログインメールアドレス |
| PASSWORD | MoneyForwardログインパスワード |
| ConnectionStrings/DefaultSqlConnection | SQLサーバー接続文字列 |

ローカルで実行する場合は、local.settings.jsonを以下のように設定する。  
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  },
  "ConnectionStrings": {
    "DefaultSqlConnection": "Server=tcp:sqlserver.database.windows.net,1433;Initial Catalog=MoneyForwardViewer;Persist Security Info=False;User ID=moneyuser;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "MAIL": "testakina@gmail.com",
  "PASSWORD": "password"
}
```

10分に一度MoneyForwardから資産推移・支出詳細データが取得され、データベースに登録される。  
Power BIで適当なレポートを作ると以下のように表示することができる。  
 - 支出
 ![支出](https://raw.github.com/wiki/southernwind/MoneyForwardViewer/images/powerbi1.gif)

 - 資産推移
 ![資産推移](https://raw.github.com/wiki/southernwind/MoneyForwardViewer/images/powerbi2.png)

## WPF版
家計の右上にMoneyForwardのIDとパスワードを入力して取得実行ボタンを押すとデータの取得が始まる。  
  
Power BI版のほうが便利な気がするので開発中止  
 - 家計
  ![家計](https://raw.github.com/wiki/southernwind/MoneyForwardViewer/images/page1.png)
- 資産推移
  ![資産推移](https://raw.github.com/wiki/southernwind/MoneyForwardViewer/images/page2.png)
- グラフ
  ![グラフ](https://raw.github.com/wiki/southernwind/MoneyForwardViewer/images/page3.png)
