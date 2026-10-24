# Kanban Board – .NET 6 WPF (MVVM, SQLite)

A desktop Kanban board application implementing a full, layered Software Engineering stack:
a WPF (MVVM) client, a C# backend with clear separation of concerns (Business, Service, and Data‑Access layers),
SQLite persistence, logging, and automated tests (NUnit).

> **Context:** Built as part of the **Introduction to Software Engineering** course at **Ben‑Gurion University**.

---
## ✨ Features
- **User management:** register, login, logout.
- **Boards & membership:** create/delete boards, owner transfer, join/leave board, list a user's boards.
- **Columns:** three standard stages (Backlog → In Progress → Done) with **WIP limits** per column.
- **Tasks:** create, edit (title/description/due date), assign/unassign, move (update state), delete.
- **Persistence:** SQLite database created on first run (or uses `kanban.db` if present).
- **Logging:** `log4net` to console and rolling file (`main.log`).
- **Error handling:** service methods return JSON `Response<T>` objects with `ErrorMessage` and `ReturnValue`.
- **Tests:** NUnit test project covering user/board/task flows.

---
## 🧱 Architecture
- **Frontend:** WPF client following **MVVM** (Views, ViewModels, Models).
- **Backend:** C# class library with three layers:
  - **Business Layer** — core entities and domain logic (`UserBL`, `BoardBL`, `TaskBL`, `ColumnBL`, `AuthenticationFacade`).
  - **Service Layer** — thin JSON façade (`UserService`, `BoardService`, `TaskService`, `Response<T>`, `ServiceFactory`).
  - **Data Access Layer** — SQLite controllers and DTOs (`User/Board/Task/Column/Collaborators`).
- **Persistence:** SQLite (file `kanban.db`, created on first run if missing).
- **Logging:** `log4net` to console and rolling file (`main.log`).

**Service Layer (selected methods):**
- `UserService`: `Register`, `Login`, `Logout`, `LoadAllUserData`, `DeleteAllUsers`
- `BoardService`: `CreateBoard`, `DeleteBoard`, `ChangeBoardOwner`, `JoinBoard`, `LeaveBoard`, `GetUserBoards`, `GetBoard`, `GetColumn`, `LimitNumOfTasks`, `ListInProgTasks`
- `TaskService`: `CreateTask`, `EditTitle`, `EditDesc`, `EditDue_Date`, `AssignTask`, `UpdateState`, `DeleteTask`

All service methods return JSON of the form:
```json
{ "returnValue": <T or null>, "errorMessage": "…" | null }
```

---
## 🛠 Tech Stack
- **Language:** C# 10 / .NET 6
- **Frontend:** WPF (`net6.0-windows`) with MVVM (View, ViewModel, Model)
- **Backend:** .NET class library with Business Layer, Service Layer (`UserService`, `BoardService`, `TaskService`)
- **Data:** SQLite via `System.Data.SQLite` with DTOs and controllers
- **Logging:** `log4net`
- **Testing:** `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`

---
## 🚀 Getting Started

### Prerequisites
- **.NET 6 SDK** (6.x)
- **Windows 10/11** to run the WPF client (the backend and tests are cross‑platform)

### Build & Run (CLI)
```bash
git clone <repo-url>
cd <repo-folder>
dotnet --version    # should be 6.x

# 1) Restore & build the whole solution
dotnet restore
dotnet build Kanban.sln -c Release

# 2) Run the WPF client (Windows 10/11 only)
dotnet run --project Frontend --configuration Release
```

### Build & Run (Visual Studio)
1. Open **Kanban.sln** in Visual Studio 2022 (Windows).
2. Right‑click **Frontend** → *Set as Startup Project*.
3. Build (**Ctrl+Shift+B**) and run (**F5**).

### Run Tests
```bash
# From the repository root
dotnet test BackendTests -c Release
```

### Database
- The app looks for `kanban.db` in the **current working directory**.
- If the file does not exist, it will be created automatically and all tables will be created (`CREATE TABLE IF NOT EXISTS`).
- To start fresh, **close the app** and delete the `kanban.db` next to the executable (e.g., `Frontend/bin/Release/net6.0-windows/kanban.db`).

---
## 📁 Project Structure
```
Kanban.sln
├─ Backend/                          # Class library (business, service, data access)
│  ├─ BuisnessLayer/                 # Domain entities & facades (UserBL, BoardBL, TaskBL, ColumnBL, AuthenticationFacade, …)
│  ├─ ServiceLayer/                  # JSON-facing services (UserService, BoardService, TaskService, Response<T>, ServiceFactory)
│  └─ DataAccessLayer/               # SQLite DTOs & controllers (User/Board/Task/Column/Collaborators)
│     ├─ Controllers/
│     └─ DTOs/
├─ Frontend/                         # WPF client (MVVM)
│  ├─ View/                          # XAML windows (Login, Board list, Board details, New board/task dialogs, …)
│  ├─ ViewModel/                     # VM classes (MainWindowVM, BoardViewModel, BoardDetailVM, …)
│  └─ Model/                         # Client-side models + ServiceController bridge to backend
├─ BackendTests/                     # NUnit tests; references Backend
│  └─ BackendTests.csproj
├─ documents/
│  └─ design.pdf                     # Design document (course context)
├─ kanban.db                         # SQLite database (optional – created on first run if missing)
├─ Backend/log4net.config            # Logging configuration (console + rolling file)
└─ README.md
```

---
## 📌 What this project demonstrates
- **Full-stack architecture:** layered C# solution with clear separation of concerns.
- **Desktop UI engineering:** WPF + MVVM with data binding and commands.
- **Relational persistence:** schema design & CRUD via SQLite and DTO controllers.
- **API design:** JSON service layer that decouples UI from domain logic.
- **Testing culture:** NUnit tests for core flows.
- **Operational maturity:** structured logging (log4net), defensive validations, and error propagation.

---
## ⚠️ Notes & Limitations
- The WPF client targets Windows (`net6.0-windows`). On macOS/Linux you can build the backend and run the tests, but the GUI requires Windows.

## ▶️ Quick Start (UI)
- 1. **Launch the app** (see Build & Run).
- 2. **Register** with an email & password.
- 3. **Login** and you will see your boards list.
- 4. **Create a board**, then open it to see the three columns.
- 5. **Add tasks** (title, description, due date).
- 6. **Drag/move** tasks between columns using the provided actions; assign tasks to members.
- 7. **Adjust WIP limits** per column if needed.

---
