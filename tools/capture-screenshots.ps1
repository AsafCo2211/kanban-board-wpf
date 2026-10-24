# Drives the real WPF app through Windows UI Automation and captures README
# screenshots: login → board list → board detail.
#
#   tools/capture-screenshots.ps1 -AppDir <folder with the .exe and kanban.db> -OutDir docs
#
# Run in an interactive Windows session (GitHub's windows-latest runners
# provide one). Every step is logged to <OutDir>/automation.log.

param(
    [Parameter(Mandatory)] [string] $AppDir,
    [Parameter(Mandatory)] [string] $OutDir,
    [string] $Email = "dana@kanban.dev",
    [string] $Password = "Demo1234",
    [string] $Board = "Mobile app v2"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    // DWMWA_EXTENDED_FRAME_BOUNDS = 9: the visible frame, without the
    // invisible resize border that GetWindowRect includes.
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);
}
"@
[Win32]::SetProcessDPIAware() | Out-Null

$UIA  = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
New-Item -ItemType Directory -Force $OutDir | Out-Null
$LogFile = Join-Path $OutDir "automation.log"

function Log([string] $msg) {
    $line = "{0:HH:mm:ss.fff} {1}" -f (Get-Date), $msg
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

function Prop($property, $value) {
    New-Object System.Windows.Automation.PropertyCondition($property, $value)
}

# All windows of the process. Owned windows (e.g. a modal dialog with
# Owner = this) sit under their owner in the UI Automation tree rather than
# at the top level, so look one level down as well.
function Get-AppWindows([int] $procId) {
    $isWindow = Prop $UIA::ControlTypeProperty ([System.Windows.Automation.ControlType]::Window)
    $all = @()
    foreach ($top in $UIA::RootElement.FindAll($Scope::Children, (Prop $UIA::ProcessIdProperty $procId))) {
        $all += $top
        foreach ($owned in $top.FindAll($Scope::Children, $isWindow)) { $all += $owned }
    }
    return ,$all
}

# Wait for a window of the process that we haven't seen before.
function Wait-NewWindow([int] $procId, [int[]] $known, [int] $timeoutSec = 30) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        foreach ($w in (Get-AppWindows $procId)) {
            if ($known -notcontains $w.Current.NativeWindowHandle) { return $w }
        }
        Start-Sleep -Milliseconds 300
    }
    throw "No new window appeared within $timeoutSec s"
}

# WPF hides TextBlocks inside data templates from the default control view,
# so searches by visible text walk the raw view instead.
function Find-Raw($root, [scriptblock] $match, [int] $limit = 4000) {
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $queue = New-Object System.Collections.Queue
    $queue.Enqueue($root)
    $results = @(); $seen = 0
    while ($queue.Count -gt 0 -and $seen -lt $limit) {
        $el = $queue.Dequeue(); $seen++
        if (& $match $el) { $results += $el }
        $child = $walker.GetFirstChild($el)
        while ($child) { $queue.Enqueue($child); $child = $walker.GetNextSibling($child) }
    }
    return ,$results
}

function Wait-Text($parent, [string] $text, [int] $timeoutSec = 15) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $hits = Find-Raw $parent { param($e) $e.Current.Name -eq $text }
        if ($hits.Count) { return $hits[0] }
        Start-Sleep -Milliseconds 400
    }
    $names = (Find-Raw $parent { param($e) $e.Current.Name } | ForEach-Object { $_.Current.Name } | Select-Object -Unique) -join " | "
    throw "Text '$text' not found. Visible names: $names"
}

function Wait-Element($parent, $condition, [string] $what, [int] $timeoutSec = 15) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $el = $parent.FindFirst($Scope::Descendants, $condition)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 300
    }
    throw "Element not found: $what"
}

# Buttons here hold a TextBlock (and an icon) rather than plain content, so
# find the text, then walk up to the enclosing button.
function Find-ButtonByText($parent, [string] $text, [int] $index = 0) {
    $texts = Find-Raw $parent { param($e) $e.Current.Name -eq $text }
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $found = @()
    foreach ($t in $texts) {
        $el = $t
        while ($el -and $el.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button) {
            $el = $walker.GetParent($el)
        }
        if ($el) { $found += $el }
    }
    if ($found.Count -le $index) { throw "Button '$text' #$index not found ($($found.Count) present)" }
    return $found[$index]
}

function Invoke-Button($button) {
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

function Set-Text($element, [string] $value) {
    try {
        $element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($value)
    } catch {
        # PasswordBox may refuse ValuePattern.SetValue — type it instead.
        $element.SetFocus()
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait($value)
    }
}

function Save-Window($window, [string] $name, [int] $width = 0, [int] $height = 0) {
    $handle = [IntPtr] $window.Current.NativeWindowHandle
    if ($width -gt 0) { [Win32]::MoveWindow($handle, 20, 20, $width, $height, $true) | Out-Null }
    [Win32]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 1200   # let WPF finish layout and animations
    $r = New-Object Win32+RECT
    [Win32]::DwmGetWindowAttribute($handle, 9, [ref] $r, 16) | Out-Null
    $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
    $path = Join-Path $OutDir "$name.png"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Log "saved $name.png (${w}x${h})"
}

$exe = Get-ChildItem -Path $AppDir -Filter "IntroSE.Kanban.Frontend.exe" | Select-Object -First 1
if (-not $exe) { throw "App executable not found in $AppDir" }
Log "screen: $([System.Windows.Forms.Screen]::PrimaryScreen.Bounds)"
Log "starting $($exe.FullName)"
$proc = Start-Process -FilePath $exe.FullName -WorkingDirectory $AppDir -PassThru

try {
    # 1. Login window
    $login = Wait-NewWindow $proc.Id @()
    Log "login window: '$($login.Current.Name)'"
    $emailBox = Wait-Element $login (Prop $UIA::AutomationIdProperty "EmailBox") "EmailBox"
    $passBox  = Wait-Element $login (Prop $UIA::AutomationIdProperty "PasswordBox") "PasswordBox"
    Set-Text $emailBox $Email
    Set-Text $passBox $Password
    Save-Window $login "login"
    Invoke-Button (Wait-Element $login (Prop $UIA::AutomationIdProperty "Login_Button") "Login_Button")
    Log "login clicked"

    # 2. Board list
    $boards = Wait-NewWindow $proc.Id @($login.Current.NativeWindowHandle)
    Log "boards window: '$($boards.Current.Name)'"
    Wait-Text $boards $Board | Out-Null
    Save-Window $boards "boards" 760 380

    # 3. Board detail — the "View" button on the first card
    Invoke-Button (Find-ButtonByText $boards "View" 0)
    Log "view clicked"
    $detail = Wait-NewWindow $proc.Id @($login.Current.NativeWindowHandle, $boards.Current.NativeWindowHandle)
    Log "detail window: '$($detail.Current.Name)'"
    Wait-Text $detail "Push notifications" | Out-Null
    Save-Window $detail "board" 860 1000
}
catch {
    Log "ERROR: $($_.Exception.Message)"
    foreach ($w in (Get-AppWindows $proc.Id)) { Log "  open window: '$($w.Current.Name)' handle=$($w.Current.NativeWindowHandle)" }
    throw
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
}
Log "done"
