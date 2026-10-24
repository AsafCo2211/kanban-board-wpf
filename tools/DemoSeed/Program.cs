// Seeds kanban.db (in the current directory) with a realistic demo board,
// using only the backend's public services — so every record obeys the same
// rules as data created through the app. Any error response aborts the run.
//
//   dotnet run --project tools/DemoSeed            → ./kanban.db
//
// Demo login: dana@kanban.dev / Demo1234

using System.Text.Json;
using Backend.ServiceLayer;

const string Email = "dana@kanban.dev";
const string Password = "Demo1234";

var services = new ServiceFactory();
Check("reset", services.DeleteAllData());
Check("register", services.Us.Register(Email, Password));
TryLogin();

var now = DateTime.Now;

SeedBoard("Mobile app v2", new[]
{
    ("Set up CI pipeline",        "Build and test on every pull request.", 2, 2),
    ("Design onboarding screens", "Three screens, skippable.", 5, 2),
    ("API rate limiting",         "Per-token limits on public endpoints.", 6, 2),
    ("Push notifications",        "Opt-in, per-category settings.", 4, 1),
    ("Offline mode",              "Cache the last synced state.", 6, 1),
    ("Dark theme",                "Follow the system setting.", 12, 0),
    ("App Store listing",         "Screenshots and description.", 15, 0),
    ("Accessibility audit",       "Screen reader and contrast pass.", 20, 0),
});

SeedBoard("Thesis", new[]
{
    ("Literature review",  "Summarise the ten core papers.", 4, 2),
    ("Experiment design",  "Pick datasets and baselines.", 7, 1),
    ("Write chapter 3",    "Method and implementation.", 30, 0),
});

SeedBoard("Apartment move", new[]
{
    ("Book movers",        "Get three quotes.", 3, 1),
    ("Change address",     "Bank, post office, employer.", 10, 0),
});

Console.WriteLine($"Seeded kanban.db — log in as {Email} / {Password}");
return 0;

void SeedBoard(string board, (string Title, string Desc, int DueInDays, int Column)[] tasks)
{
    Check($"create board '{board}'", services.Bs.CreateBoard(board, Email));
    foreach (var t in tasks)
        Check($"create task '{t.Title}'",
              services.Bs.CreateTask(board, Email, t.Title, t.Desc, now.AddDays(t.DueInDays)));

    // Look up the IDs the backend assigned, then assign and advance each task
    // to its target column (0 = backlog, 1 = in progress, 2 = done).
    var ids = TaskIds(board);
    foreach (var t in tasks)
    {
        long id = ids[t.Title];
        Check($"assign '{t.Title}'", services.Ts.AssignTask(Email, board, 0, (int)id, Email));
        for (int col = 0; col < t.Column; col++)
            Check($"advance '{t.Title}'", services.Ts.UpdateState(Email, board, col, (int)id));
    }
}

Dictionary<string, long> TaskIds(string board)
{
    using var doc = JsonDocument.Parse(Check($"read backlog of '{board}'", services.Bs.GetColumn(Email, board, 0)));
    return doc.RootElement.GetProperty("ReturnValue").EnumerateArray()
              .ToDictionary(t => t.GetProperty("Title").GetString()!, t => t.GetProperty("Id").GetInt64());
}

void TryLogin()
{
    // Register may or may not log the user in; logging in twice is harmless here.
    services.Us.Login(Email, Password);
}

static string Check(string step, string json)
{
    using var doc = JsonDocument.Parse(json);
    if (doc.RootElement.TryGetProperty("ErrorMessage", out var err) && err.ValueKind == JsonValueKind.String)
    {
        Console.Error.WriteLine($"Seeding failed at {step}: {err.GetString()}");
        Environment.Exit(1);
    }
    return json;
}
