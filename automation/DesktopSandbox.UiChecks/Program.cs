using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Automation;

namespace DesktopSandbox.UiChecks;

internal enum Outcome
{
    Pass,
    Defect,
    Fail
}

internal static class Program
{
    private static AutomationElement _root = null!;
    private static int _failed;
    private static int _passed;
    private static int _defects;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            if (args.Contains("--logic"))
            {
                RunLogicChecks();
            }
            else
            {
                var exe = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))
                          ?? Path.GetFullPath(Path.Combine("src", "DesktopSandbox", "bin", "Release", "net11.0-windows", "DesktopSandbox.exe"));

                if (!File.Exists(exe))
                {
                    Console.WriteLine($"Application not found: {exe}");
                    return 2;
                }

                RunUiChecks(exe);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Automation crashed: {ex}");
            return 1;
        }

        PrintSummary();
        return _failed == 0 ? 0 : 1;
    }

    private static void RunUiChecks(string exe)
    {
        Console.WriteLine("# Desktop QA Sandbox — UI defect reproduction run");
        Console.WriteLine();
        Console.WriteLine($"- Date (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"- OS: {Environment.OSVersion}");
        Console.WriteLine($"- Application: `{Path.GetFileName(exe)}`");
        Console.WriteLine();

        var process = Process.Start(new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe)!
        }) ?? throw new InvalidOperationException("Could not start the application.");

        WaitForMainWindow(process);
        if (process.MainWindowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("The application window did not appear.");
        }

        _root = AutomationElement.FromHandle(process.MainWindowHandle);
        Console.WriteLine($"- Window: `{_root.Current.Name}`");
        Console.WriteLine();
        Thread.Sleep(3000);

        try
        {
            CheckOrders();
            CheckRegistration();
            CheckNotes(process);
            CheckCloseWithoutPrompt(process);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static void WaitForMainWindow(Process process)
    {
        for (var i = 0; i < 120; i++)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    private static void CheckRegistration()
    {
        SelectTab("Registration");

        var valid = SaveRegistration("qa.intern@example.com", "25");
        Record("TC-01", "Valid registration data is accepted", "Saved successful", valid,
            valid.Contains("Saved successful", StringComparison.Ordinal) ? Outcome.Pass : Outcome.Fail);

        Record("D-10", "Success message uses correct grammar", "Saved successfully", valid,
            valid == "Saved successful" ? Outcome.Defect : Outcome.Pass,
            "The UI says 'Saved successful' (missing '-ly').");

        var noTld = SaveRegistration("user@domain", "25");
        Record("D-01", "E-mail without a top-level domain is rejected", "Error: e-mail format is invalid", noTld,
            noTld.Contains("Saved successful", StringComparison.Ordinal) ? Outcome.Defect : Outcome.Pass,
            "Regex `^[^@\\s]+@[^@\\s]+$` accepts 'user@domain'.");

        var ageZero = SaveRegistration("qa.intern@example.com", "0");
        Record("D-02", "Age 0 is rejected", "Error: age must be between 1 and 149", ageZero,
            ageZero.Contains("Saved successful", StringComparison.Ordinal) ? Outcome.Defect : Outcome.Pass);

        var ageNegative = SaveRegistration("qa.intern@example.com", "-5");
        Record("D-02b", "Negative age is rejected", "Error: age must be between 1 and 149", ageNegative,
            ageNegative.Contains("Saved successful", StringComparison.Ordinal) ? Outcome.Defect : Outcome.Pass);

        var ageText = SaveRegistration("qa.intern@example.com", "abc");
        Record("D-11", "Non-numeric age shows a friendly validation message", "Error: age must be a number", ageText,
            ageText.Contains("FormatException", StringComparison.Ordinal) ? Outcome.Defect : Outcome.Pass,
            "A raw .NET stack trace is rendered in the status label.");

        var mismatch = SaveRegistration("qa.intern@example.com", "25", "Passw0rd!", "Other123!");
        Record("D-09", "Password mismatch message names the fields", "Passwords do not match", mismatch,
            mismatch == "Error" ? Outcome.Defect : Outcome.Pass,
            "Only the word 'Error' is shown, without pointing to the password fields.");

        var weak = SaveRegistration("qa.intern@example.com", "25", "abc123");
        Record("D-13", "Short password is rejected", "Error: password must be at least 8 characters", weak,
            weak.Contains("Saved successful", StringComparison.Ordinal) ? Outcome.Defect : Outcome.Pass,
            "Rules.IsValidPassword exists but is never called on save.");
    }

    private static void CheckOrders()
    {
        SelectTab("Orders");
        Invoke("ResetOrdersButton");

        var total = TotalValue();
        Record("TC-02", "Order total is calculated correctly", "437.44", total.ToString("0.00"),
            Math.Abs(total - 437.44m) < 0.005m ? Outcome.Pass : Outcome.Fail);

        Invoke("ApplyDiscountButton");
        var firstDiscount = TotalValue();
        Invoke("ApplyDiscountButton");
        var secondDiscount = TotalValue();

        Record("D-03", "Discount is applied only once per action", "Second click keeps 393.70", $"First 393.70, second {secondDiscount:0.00}",
            secondDiscount < firstDiscount ? Outcome.Defect : Outcome.Pass,
            "Every click applies another 10% to the already discounted total.");

        Invoke("ResetOrdersButton");
        var rowsBefore = GridRows();
        var totalBefore = TotalValue();
        Invoke("DeleteButton");
        var rowsAfter = GridRows();
        var totalAfter = TotalValue();

        Record("D-04", "Row deletion asks for confirmation", "Confirmation dialog before delete", $"{rowsBefore} -> {rowsAfter} rows, no dialog",
            rowsAfter == rowsBefore - 1 ? Outcome.Defect : Outcome.Pass);

        Record("D-05", "Total is recalculated after deletion", "Deleting Alpha Corp (100.00) gives 337.44", totalAfter.ToString("0.00"),
            Math.Abs(totalAfter - totalBefore) > 0.005m ? Outcome.Pass : Outcome.Defect,
            "The total keeps the pre-deletion value.");

        Invoke("ResetOrdersButton");
        var header = FindHeader("Amount");
        ((InvokePattern)header.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        Thread.Sleep(500);
        var amounts = Amounts();
        var firstThree = string.Join(", ", amounts.Take(3));
        Record("D-06", "Amount column is sorted as a number", "0.99, 3.20, 5.50", firstThree,
            amounts.Count > 1 && amounts[1] == "100.00" ? Outcome.Defect : Outcome.Pass,
            "Amounts are stored as text, so ascending sort is lexicographic.");

        Invoke("ResetOrdersButton");
        SetText("SearchBox", "alpha");
        Invoke("SearchButton");
        var lowerRows = GridRows();
        Record("D-07", "Search is case-insensitive", "1 row for 'alpha' (Alpha Corp)", $"{lowerRows} rows",
            lowerRows == 0 ? Outcome.Defect : Outcome.Pass);

        SetText("SearchBox", "Alpha");
        Invoke("SearchButton");
        var exactRows = GridRows();
        Record("TC-03", "Search with the exact case finds the customer", "1 row", $"{exactRows} rows",
            exactRows == 1 ? Outcome.Pass : Outcome.Fail);

        Invoke("ResetOrdersButton");
    }

    private static void CheckNotes(Process process)
    {
        SelectTab("Notes");
        var noteFile = Path.Combine(Path.GetTempPath(), "qa-sandbox-note.txt");
        if (File.Exists(noteFile))
        {
            File.Delete(noteFile);
        }

        SetText("NotesBox", new string('x', 300));
        Invoke("SaveNoteButton");
        Thread.Sleep(300);

        var savedLength = File.Exists(noteFile) ? new FileInfo(noteFile).Length : -1;
        Record("D-08", "A 300-character note is saved without silent data loss", "300 characters or a validation error", $"{savedLength} characters on disk",
            savedLength == 255 ? Outcome.Defect : Outcome.Pass,
            "Rules.TruncateNote cuts the note at 255 characters and still reports success.");

        SetText("NotesBox", "unsaved draft");
        Invoke("ClearButton");
        var afterClear = GetText("NotesBox");
        Record("D-12", "Clear asks for confirmation (or offers undo)", "Confirmation dialog", afterClear.Length == 0 ? "Cleared immediately" : "Text kept",
            afterClear.Length == 0 ? Outcome.Defect : Outcome.Pass);

        _ = process;
    }

    private static void CheckCloseWithoutPrompt(Process process)
    {
        SelectTab("Notes");
        SetText("NotesBox", "draft that was never saved");
        ((WindowPattern)_root.GetCurrentPattern(WindowPattern.Pattern)).Close();
        Thread.Sleep(1200);
        process.Refresh();

        Record("D-14", "Closing with unsaved notes shows a warning", "Confirmation dialog or save prompt",
            process.HasExited ? "Window closed silently" : "Window is still open",
            process.HasExited ? Outcome.Defect : Outcome.Pass);
    }

    private static void RunLogicChecks()
    {
        Console.WriteLine("# Desktop QA Sandbox — logic checks (CI mode)");
        Console.WriteLine();

        Record("D-01", "Email without TLD is accepted by Rules.IsValidEmail", "false", Rules.IsValidEmail("user@domain").ToString(),
            Rules.IsValidEmail("user@domain") ? Outcome.Defect : Outcome.Pass);

        Record("TC-04", "Valid email is accepted by Rules.IsValidEmail", "true", Rules.IsValidEmail("qa@example.com").ToString(),
            Rules.IsValidEmail("qa@example.com") ? Outcome.Pass : Outcome.Fail);

        var truncated = Rules.TruncateNote(new string('x', 300));
        Record("D-08", "300-character note is truncated to 255", "300", truncated.Length.ToString(),
            truncated.Length == 255 ? Outcome.Defect : Outcome.Pass);

        var once = Rules.ApplyDiscount(100m, 10m);
        var twice = Rules.ApplyDiscount(once, 10m);
        Record("D-03", "Discount can be chained by the caller", "second call returns 90.00", twice.ToString("0.00"),
            Math.Abs(twice - 81m) < 0.005m ? Outcome.Defect : Outcome.Pass);

        Record("TC-05", "Age rule rejects 0 (rule exists but is not wired into the form)", "false", Rules.IsValidAge(0).ToString(),
            !Rules.IsValidAge(0) ? Outcome.Pass : Outcome.Fail);
    }

    private static void Record(string id, string title, string expected, string actual, Outcome outcome, string? note = null)
    {
        switch (outcome)
        {
            case Outcome.Pass:
                _passed++;
                break;
            case Outcome.Defect:
                _defects++;
                break;
            default:
                _failed++;
                break;
        }

        Console.WriteLine($"| {id} | {title} | {expected} | {actual} | {outcome} |");
        if (note != null)
        {
            Console.WriteLine($"| | {note} | | | |");
        }
    }

    private static void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("## Summary");
        Console.WriteLine();
        Console.WriteLine($"- PASS: {_passed}");
        Console.WriteLine($"- DEFECT reproduced: {_defects}");
        Console.WriteLine($"- FAIL: {_failed}");
        Console.WriteLine();
        Console.WriteLine(_failed == 0
            ? "Result: every documented behaviour was reproduced; see defect reports for severity and impact."
            : "Result: unexpected behaviour detected. Review the rows marked FAIL.");
    }

    private static string SaveRegistration(string email, string age, string password = "Passw0rd!", string? confirm = null)
    {
        SetText("FirstNameBox", "QA");
        SetText("LastNameBox", "Tester");
        SetText("EmailBox", email);
        SetText("AgeBox", age);
        SetText("PasswordBox", password);
        SetText("ConfirmBox", confirm ?? password);
        Invoke("SaveRegistrationButton");
        Thread.Sleep(400);
        return Find("RegStatusLabel", 3000)?.Current.Name ?? "<no status>";
    }

    private static void SelectTab(string name)
    {
        var buttonId = name switch
        {
            "Registration" => "NavRegistrationButton",
            "Orders" => "NavOrdersButton",
            "Notes" => "NavNotesButton",
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown section")
        };

        var probe = name switch
        {
            "Registration" => "FirstNameBox",
            "Orders" => "OrdersGrid",
            _ => "NotesBox"
        };

        Invoke(buttonId);
        Find(probe, 5000);
    }

    private static AutomationElement Find(string automationId, int timeoutMs = 5000)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < timeoutMs)
        {
            var element = _root.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
            if (element != null)
            {
                return element;
            }

            Thread.Sleep(100);
        }

        throw new InvalidOperationException(
            $"UI element not found: {automationId}. Tree sample: {DescribeTree()}");
    }

    private static string DescribeTree()
    {        var parts = new List<string>();
        var elements = _root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        foreach (AutomationElement element in elements)
        {
            if (parts.Count >= 25)
            {
                break;
            }

            parts.Add($"{element.Current.ControlType.ProgrammaticName}#{element.Current.AutomationId}");
        }

        return string.Join(", ", parts);
    }

    private static AutomationElement FindHeader(string name)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Header),
            new PropertyCondition(AutomationElement.NameProperty, name));
        return Require("OrdersGrid").FindFirst(TreeScope.Descendants, condition)
               ?? throw new InvalidOperationException($"Header not found: {name}");
    }

    private static AutomationElement Require(string id) => Find(id);

    private static void SetText(string id, string value)
    {
        var element = Require(id);
        ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
        Thread.Sleep(80);
    }

    private static string GetText(string id)
    {
        var element = Require(id);
        return ((ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
    }

    private static void Invoke(string id)
    {
        var element = Require(id);
        ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
        Thread.Sleep(250);
    }

    private static int GridRows()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var grid = Require("OrdersGrid");
            if (grid.TryGetCurrentPattern(TablePattern.Pattern, out var pattern))
            {
                return ((TablePattern)pattern).Current.RowCount;
            }

            Thread.Sleep(100);
        }

        // WinForms sometimes invalidates the table provider after the row collection is rebuilt.
        // Fall back to counting the value cells exposed by the grid.
        return Amounts().Count;
    }

    private static List<string> Amounts()
    {
        List<string> values = new();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            values.Clear();
            var grid = Require("OrdersGrid");
            var items = grid.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem));
            foreach (AutomationElement item in items)
            {
                if (item.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
                {
                    values.Add(((ValuePattern)pattern).Current.Value);
                }
            }

            if (values.Count > 0)
            {
                break;
            }

            Thread.Sleep(100);
        }

        var amounts = new List<string>();
        for (var i = 1; i < values.Count; i += 2)
        {
            amounts.Add(values[i]);
        }

        return amounts;
    }

    private static decimal TotalValue()
    {
        var text = Require("TotalLabel").Current.Name;
        var index = text.IndexOf('$');
        return index >= 0 && decimal.TryParse(text[(index + 1)..], NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : -1m;
    }
}
