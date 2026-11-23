param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$exe = Join-Path $RepoRoot 'src\DesktopSandbox\bin\Release\net11.0-windows\DesktopSandbox.exe'
$evidence = Join-Path $RepoRoot 'evidence'
if (-not (Test-Path $exe)) { throw "Build the app first: $exe" }
New-Item -ItemType Directory -Force -Path $evidence | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class SandboxCapture {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
}
"@

$process = Start-Process $exe -PassThru
Start-Sleep -Seconds 3
$handle = $process.MainWindowHandle
[SandboxCapture]::SetWindowPos($handle, [IntPtr]::Zero, 10, 10, 1040, 760, 0x0040) | Out-Null
Start-Sleep -Milliseconds 500

$root = [System.Windows.Automation.AutomationElement]::FromHandle($handle)

function Find-ById([string]$id) {
    $root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)))
}

function Set-Value([string]$id, [string]$value) {
    $element = Find-ById $id
    ($element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($value)
    Start-Sleep -Milliseconds 120
}

function Invoke-Control([string]$id) {
    $element = Find-ById $id
    ($element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
    Start-Sleep -Milliseconds 500
}

function Save-Window([string]$name) {
    $rect = New-Object SandboxCapture+RECT
    [SandboxCapture]::GetWindowRect($handle, [ref]$rect) | Out-Null
    $bitmap = New-Object System.Drawing.Bitmap ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $dc = $graphics.GetHdc()
    [SandboxCapture]::PrintWindow($handle, $dc, 2) | Out-Null
    $graphics.ReleaseHdc($dc)
    $graphics.Dispose()
    $bitmap.Save((Join-Path $evidence $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host "saved $name"
}

Save-Window '01-registration.png'

Set-Value 'FirstNameBox' 'QA'
Set-Value 'LastNameBox' 'Tester'
Set-Value 'EmailBox' 'qa.intern@example.com'
Set-Value 'AgeBox' 'abc'
Set-Value 'PasswordBox' 'Passw0rd!'
Set-Value 'ConfirmBox' 'Passw0rd!'
Invoke-Control 'SaveRegistrationButton'
Save-Window '02-stack-trace.png'

Invoke-Control 'NavOrdersButton'
Save-Window '03-orders.png'

Invoke-Control 'DeleteButton'
Save-Window '04-delete-total-stale.png'

Invoke-Control 'NavNotesButton'
Set-Value 'NotesBox' ('x' * 300)
Invoke-Control 'SaveNoteButton'
Save-Window '05-note-truncated.png'

Invoke-Control 'NavOrdersButton'
Invoke-Control 'ResetOrdersButton'
$grid = Find-ById 'OrdersGrid'
$headerCondition = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Header)),
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, 'Amount')))
$header = $grid.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $headerCondition)
($header.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
Start-Sleep -Milliseconds 700
Save-Window '06-sort-text.png'

Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
Write-Host "note file size: $((Get-Item (Join-Path $env:TEMP 'qa-sandbox-note.txt')).Length) bytes"
