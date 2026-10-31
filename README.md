# 使用 Microsoft Agent Framework 與 Function Calling 建立請假助理

這是一個給學員使用的 .NET Console 範例，示範如何使用 Microsoft Agent Framework 搭配 OpenAI Responses API，建立可以透過自然語言處理請假需求的 AI 助理。

助理可以：

- 蒐集請假所需的資料：姓名、起始日期、天數、事由與代理人。
- 呼叫 `LeaveRequest` 函式建立請假申請。
- 呼叫 `GetLeaveRecordAmount` 查詢員工剩餘的請假天數。
- 呼叫 `GetCurrentDate` 取得台灣時區的日期，協助處理「明天」、「下週一」等相對日期。
- 在同一個對談 session 中保留前後文，讓使用者可以逐步補充資料。

> 本專案是教學範例。請假資料與剩餘天數目前寫死在程式碼中，沒有連接實際資料庫，也不會真的送出企業假單。

## 執行環境

- .NET 10 SDK
- OpenAI API Key
- 可連線至 OpenAI API 的網路環境

確認 .NET SDK 已安裝：

```bash
dotnet --version
```

本專案的目標框架是 `net10.0`。若使用其他 .NET 版本，可能需要同步調整 `.csproj` 中的 `TargetFramework`。

## 設定 OpenAI API Key

程式會讀取名稱為 `OPENAI_API_KEY` 的環境變數：

```csharp
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
		?? throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數。");
```

請不要把 API Key 直接寫入 `Program.cs`、README、Git repository 或 commit 中。

### Linux/macOS 或 VS Code Dev Container

只在目前終端機工作階段設定：

```bash
export OPENAI_API_KEY="你的 OpenAI API Key"
```

確認是否已設定，但不要在畫面上印出完整 Key：

```bash
if [ -n "$OPENAI_API_KEY" ]; then echo "OPENAI_API_KEY 已設定"; else echo "尚未設定"; fi
```

若希望之後開啟終端機時也能使用，可以將 `export` 加到 shell 的設定檔，例如 `~/.bashrc` 或 `~/.zshrc`，再重新開啟終端機。請確認該設定檔不會被加入 Git。

### Windows PowerShell

只在目前 PowerShell 工作階段設定：

```powershell
$env:OPENAI_API_KEY = "你的 OpenAI API Key"
```

### Windows 命令提示字元（CMD）

只在目前 CMD 工作階段設定：

```cmd
set OPENAI_API_KEY=你的 OpenAI API Key
```

### 使用 VS Code `launch.json`

如果從 VS Code 的偵錯功能啟動，可以在 `.vscode/launch.json` 的 `env` 中設定：

```json
{
	"version": "0.2.0",
	"configurations": [
		{
			"name": ".NET Launch Console",
			"type": "coreclr",
			"request": "launch",
			"program": "${workspaceFolder}/bin/Debug/net10.0/MAF_LeaveRequest_using_FunctionCall.dll",
			"cwd": "${workspaceFolder}",
			"env": {
				"OPENAI_API_KEY": "你的 OpenAI API Key"
			}
		}
	]
}
```

教學或個人測試可以使用這種方式，但不建議將含有真實 API Key 的 `launch.json` 提交到公開或共用的 repository。較安全的做法是由外部終端機設定環境變數。

## 安裝與執行

在專案目錄執行：

```bash
dotnet restore
dotnet run
```

程式啟動後會看到：

```text
請假助理已啟動，輸入 exit 結束。
```

輸入空白行或 `exit` 可以結束程式。可以嘗試：

```text
我想請假
```

助理會繼續詢問缺少的資料。也可以直接測試查詢功能：

```text
請幫我查詢 David 還有幾天假？
```

範例資料中：

- David 剩餘 5 天。
- Eric 剩餘 8 天。
- 其他姓名預設剩餘 3 天。

## 使用到的套件

專案檔目前直接參考：

```xml
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.27.0" />
```

這個套件會帶入範例所需的相關相依套件。程式中各 namespace 的用途如下：

### `Microsoft.Agents.AI.OpenAI`

提供 Microsoft Agent Framework 與 OpenAI 的整合，包括：

- 將 OpenAI client 轉成 `AIAgent`。
- 使用 OpenAI Responses API 執行 agent。
- 將工具函式註冊給 agent 使用。

程式中的核心建立方式是：

```csharp
OpenAIClient client = new(apiKey);

AIAgent agent = client.GetResponsesClient().AsAIAgent(
		model: "gpt-4o-mini",
		instructions: "...",
		tools: [ ... ]
);
```

### `OpenAI`

提供 `OpenAIClient` 與 Responses client。這個範例使用的模型是 `gpt-4o-mini`，實際可用模型與費用請以 OpenAI 官方文件及帳戶設定為準。

### `Microsoft.Extensions.AI`

提供 `AIFunctionFactory.Create`，可以將 C# 方法轉成模型可呼叫的工具函式。方法上的 `Description` 與參數上的 `Description` 會協助模型理解工具用途與參數意義：

```csharp
[Description("查詢員工剩餘的請假天數。")]
static int GetLeaveRecordAmount(
		[Description("要查詢請假天數的員工姓名")] string employeeName)
```

### `System.ComponentModel`

提供 `DescriptionAttribute`，也就是上面標註工具與參數說明所使用的 `Description`。

## 預覽 API 與警告說明

Microsoft Agent Framework 及其 OpenAI 整合在不同版本中仍可能調整 API。這個範例使用 `Microsoft.Agents.AI.OpenAI` 1.27.0，學員在更新套件版本前，應先閱讀版本變更內容並重新確認 API。

另外，`AsAIAgent` 所使用的 OpenAI Responses 整合 API 會產生 `OPENAI001` 實驗性 API 警告。因此範例只在建立 agent 的區段暫時停用該警告：

```csharp
#pragma warning disable OPENAI001
AIAgent agent = client.GetResponsesClient().AsAIAgent(...);
#pragma warning restore OPENAI001
```

這不是把所有警告都關閉，也不代表 API 已經完全穩定。實際應用程式應該：

1. 固定並記錄套件版本。
2. 更新套件前先在測試環境驗證。
3. 不要把預覽或實驗性 API 當成永遠不變的公開契約。
4. 若未來 API 穩定或改名，移除不再需要的 `#pragma warning`。

## Function Calling 的流程

本範例不是讓模型直接修改資料，而是將三個 C# 方法提供給模型選擇：

1. 使用者輸入自然語言。
2. Agent 根據 `instructions` 判斷需求與缺少的資訊。
3. 如果資料尚未完整，Agent 先以對話方式提問。
4. 如果資料完整，模型產生對工具的呼叫。
5. Framework 執行對應的 C# 函式。
6. 函式結果回到對談中，由 Agent 產生人類可讀的回覆。

三個工具的責任如下：

| 函式 | 用途 | 回傳值 |
| --- | --- | --- |
| `LeaveRequest` | 建立請假申請 | `bool` |
| `GetLeaveRecordAmount` | 查詢剩餘請假天數 | `int` |
| `GetCurrentDate` | 取得台灣日期 | `DateTime` |

目前函式只將動作印在 Console 上。若要接上真實系統，可以在函式內加入資料庫、企業內部 API 或簽核流程，但仍應加入權限驗證、輸入檢查、重複送單防護與錯誤處理。

## 對談 Session 的建立方式

### 建立一個新的 session

程式啟動時建立一個 `AIAgent`，再建立一個對談 session：

```csharp
var session = await agent.CreateSessionAsync();
```

`AIAgent` 描述「助理具備哪些能力」，而 `session` 描述「某一次對談的狀態」。同一個 agent 可以建立很多個 session，例如每位員工各自一個 session，或每個瀏覽器對談各自一個 session。

### 在同一個 session 中持續對話

每次呼叫 `RunAsync` 都傳入相同的 session：

```csharp
var result = await agent.RunAsync(userInput, session);
```

因此模型可以記得前面已提供的姓名、日期或請假事由，並在下一輪只詢問尚未取得的資料。如果每次都建立新的 session，模型就不會擁有前一輪的對談上下文。

### Session 的生命週期

這個 Console 範例的 session 只存在於目前程式執行期間：

- 程式啟動時建立。
- 使用者持續輸入時重複使用。
- 程式結束後 session 不會由這個範例自動保存。

實際網站或服務通常會在每位使用者登入、每個工單或每個對談建立 session，並使用自己的儲存機制保存識別資訊與必要的對談狀態。若需要跨程式重啟恢復對談，請依使用中的 Agent Framework 版本查閱 session serialization、session store 或 conversation state 的 API；不要直接假設不同版本的 session 可以互換。

## 程式結構重點

### Agent instructions

`instructions` 是給 agent 的行為規則，包含使用語言、何時詢問資料，以及何時呼叫哪一個工具。它不是 C# 的驗證邏輯，因此正式系統仍應在工具函式內再次檢查必要欄位與資料格式。

### Function description

工具與參數的 `Description` 是給模型看的 metadata。描述越清楚，模型越容易選對工具與填入正確參數，但它不能取代後端權限控管或輸入驗證。

### 模型與工具執行權限

本範例允許 agent 自動呼叫已註冊的函式。請注意，正式環境不應只依賴模型決定是否可以送出假單；送出、取消或修改等具副作用的操作，通常需要員工身分驗證、確認步驟與伺服器端授權。

## 常見問題

### 啟動時顯示「請設定 OPENAI_API_KEY 環境變數」

代表目前執行程式的終端機沒有讀到環境變數。請依照上面的作業系統方式設定，並在同一個終端機重新執行 `dotnet run`。

### API Key 已設定但仍無法呼叫

請確認：

- Key 沒有多餘的空白或引號內容。
- OpenAI 帳戶仍可使用 API 且有足夠額度。
- 網路可以連到 OpenAI API。
- 使用的模型名稱與帳戶權限相容。

### 更新套件後無法編譯

先確認 `MAF_LeaveRequest_using_FunctionCall.csproj` 中的套件版本，再閱讀 Microsoft Agent Framework 與 OpenAI 整合套件的版本說明。預覽或實驗性 API 可能變更方法名稱、參數或 session 型別。

## 參考檔案

- `Program.cs`：Agent、session、對談迴圈與三個工具函式。
- `MAF_LeaveRequest_using_FunctionCall.csproj`：目標框架與 NuGet 套件版本。