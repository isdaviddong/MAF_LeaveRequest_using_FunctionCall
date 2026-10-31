using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("請設定 OPENAI_API_KEY 環境變數。");

OpenAIClient client = new(apiKey);

#pragma warning disable OPENAI001
AIAgent agent = client.GetResponsesClient().AsAIAgent(
    model: "gpt-4o-mini",
    instructions: """
        你是企業的請假助理，所有回答都使用正體中文，語氣自然口語化。
        你可以協助員工送出請假申請，或查詢員工剩餘的請假天數。
        員工要請假時，必須先蒐集請假者姓名、請假起始日期、請假天數、請假事由與代理人；資料不完整時要逐項詢問，資料完整後才呼叫 LeaveRequest。
        員工要查詢餘額時，必須先取得員工姓名，再呼叫 GetLeaveRecordAmount。
        若需要判斷相對日期（例如明天），請先呼叫 GetCurrentDate。
        工具執行完成後，請清楚告知員工結果。
        """,
    tools:
    [
        AIFunctionFactory.Create(LeaveRequest),
        AIFunctionFactory.Create(GetLeaveRecordAmount),
        AIFunctionFactory.Create(GetCurrentDate)
    ]
);
#pragma warning restore OPENAI001

var session = await agent.CreateSessionAsync();
Console.WriteLine("請假助理已啟動，輸入 exit 結束。\n");

while (true)
{
    Console.Write("用戶 > ");
    string? userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var result = await agent.RunAsync(userInput, session);
    Console.WriteLine($"AI 助理 > {result}\n");
}

[Description("送出請假申請。只有在已取得所有必要資料後才呼叫此函式。")]
static bool LeaveRequest(
    [Description("請假者姓名")] string employeeName,
    [Description("請假起始日期")] DateTime startDate,
    [Description("請假天數，例如 1 或 0.5")] double days,
    [Description("請假事由")] string reason,
    [Description("工作代理人姓名")] string 代理人)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[Function Call] 建立假單：{employeeName} 請假 {days} 天，從 {startDate:yyyy-MM-dd} 開始，事由：{reason}，代理人：{代理人}");
    Console.ResetColor();

    return true;
}

[Description("查詢員工剩餘的請假天數。")]
static int GetLeaveRecordAmount(
    [Description("要查詢請假天數的員工姓名")] string employeeName)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[Function Call] 查詢 {employeeName} 的剩餘請假天數");
    Console.ResetColor();

    return employeeName.Trim().ToLowerInvariant() switch
    {
        "david" => 5,
        "eric" => 8,
        _ => 3
    };
}

[Description("取得今天的日期，時區為台灣（UTC+8）。")]
static DateTime GetCurrentDate()
{
    return DateTime.UtcNow.AddHours(8).Date;
}

