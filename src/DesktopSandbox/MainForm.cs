using System.Globalization;

namespace DesktopSandbox;

public sealed class MainForm : Form
{
    private readonly TextBox firstNameBox = new() { Name = "FirstNameBox", Width = 300 };
    private readonly TextBox lastNameBox = new() { Name = "LastNameBox", Width = 300 };
    private readonly TextBox emailBox = new() { Name = "EmailBox", Width = 300 };
    private readonly TextBox ageBox = new() { Name = "AgeBox", Width = 300 };
    private readonly TextBox passwordBox = new() { Name = "PasswordBox", Width = 300, UseSystemPasswordChar = true };
    private readonly TextBox confirmBox = new() { Name = "ConfirmBox", Width = 300, UseSystemPasswordChar = true };
    private readonly Label regStatus = new()
    {
        Name = "RegStatusLabel",
        AutoSize = false,
        Width = 700,
        Height = 120,
        MaximumSize = new Size(700, 120),
        Margin = new Padding(0, 10, 0, 0)
    };

    private readonly TextBox searchBox = new() { Name = "SearchBox", Width = 220 };
    private readonly DataGridView ordersGrid = new()
    {
        Name = "OrdersGrid",
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        GridColor = Color.FromArgb(229, 231, 235),
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 34,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(243, 244, 246),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            SelectionBackColor = Color.FromArgb(243, 244, 246),
            SelectionForeColor = Color.Black
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            SelectionBackColor = Color.FromArgb(209, 250, 229),
            SelectionForeColor = Color.Black
        }
    };
    private readonly Label totalLabel = new()
    {
        Name = "TotalLabel",
        AutoSize = true,
        Font = new Font("Segoe UI", 12, FontStyle.Bold)
    };

    private readonly TextBox notesBox = new()
    {
        Name = "NotesBox",
        Multiline = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical,
        AcceptsReturn = true
    };
    private readonly Label noteStatus = new() { Name = "NoteStatusLabel", AutoSize = true };
    private readonly Label charCount = new() { Name = "CharCountLabel", AutoSize = true };

    private readonly Panel contentHost = new() { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16) };
    private readonly Dictionary<string, (Panel Panel, Button Button)> sections = new(StringComparer.OrdinalIgnoreCase);

    private decimal _displayedTotal;

    private static readonly (string Customer, decimal Amount)[] SeedOrders =
    {
        ("Alpha Corp", 100.00m),
        ("beta llc", 20.00m),
        ("Gamma Inc", 5.50m),
        ("Delta Trading", 45.75m),
        ("Epsilon", 3.20m),
        ("Zeta Holdings", 250.00m),
        ("Eta Services", 12.00m),
        ("Theta Labs", 0.99m)
    };

    public MainForm()
    {
        Text = "QA Desktop Sandbox \u2014 Employee & Orders Console";
        Name = "MainForm";
        Width = 1040;
        Height = 760;
        MinimumSize = new Size(860, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.White;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(contentHost, 1, 0);
        Controls.Add(root);

        var registration = BuildRegistrationPanel();
        var orders = BuildOrdersPanel();
        var notes = BuildNotesPanel();

        sections["Registration"] = (registration, FindNavButton("NavRegistrationButton"));
        sections["Orders"] = (orders, FindNavButton("NavOrdersButton"));
        sections["Notes"] = (notes, FindNavButton("NavNotesButton"));

        foreach (var (panel, _) in sections.Values)
        {
            panel.Visible = false;
            contentHost.Controls.Add(panel);
        }

        ShowSection("Registration");
        LoadOrders();
        UpdateCharCount();
    }

    private Button FindNavButton(string name) =>
        (Button)Controls.Find(name, searchAllChildren: true).Single();

    private Control BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(31, 41, 55), Padding = new Padding(18, 24, 18, 18) };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        stack.Controls.Add(new Label
        {
            Text = "QA Sandbox",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        });
        stack.Controls.Add(new Label
        {
            Text = "Desktop test object",
            ForeColor = Color.FromArgb(156, 163, 175),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 26)
        });

        stack.Controls.Add(CreateNavButton("NavRegistrationButton", "Registration", "Register a new employee"));
        stack.Controls.Add(CreateNavButton("NavOrdersButton", "Orders", "Discounts, search and deletion"));
        stack.Controls.Add(CreateNavButton("NavNotesButton", "Notes", "Save a 255-character note"));

        sidebar.Controls.Add(stack);
        return sidebar;
    }

    private Button CreateNavButton(string name, string title, string subtitle)
    {
        var button = new Button
        {
            Name = name,
            Text = title,
            Width = 160,
            Height = 46,
            Margin = new Padding(0, 0, 0, 8),
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            ForeColor = Color.Gainsboro,
            BackColor = Color.FromArgb(31, 41, 55),
            AccessibleDescription = subtitle
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => ShowSection(title);
        return button;
    }

    private void ShowSection(string name)
    {
        foreach (var (key, (panel, button)) in sections)
        {
            var isActive = string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
            panel.Visible = isActive;
            button.BackColor = isActive ? Color.FromArgb(16, 185, 129) : Color.FromArgb(31, 41, 55);
            button.ForeColor = isActive ? Color.White : Color.Gainsboro;
        }
    }

    private Panel BuildRegistrationPanel()
    {
        var panel = new Panel { Name = "RegistrationPanel", Dock = DockStyle.Fill, AutoScroll = true };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        var heading = new Label
        {
            Text = "Employee registration",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        };
        flow.Controls.Add(heading);

        AddField(flow, "First name *", firstNameBox);
        AddField(flow, "Last name *", lastNameBox);
        AddField(flow, "Email *", emailBox);
        AddField(flow, "Age", ageBox);
        AddField(flow, "Password", passwordBox);
        AddField(flow, "Confirm password", confirmBox);

        var save = new Button { Name = "SaveRegistrationButton", Text = "Save", Width = 140, Height = 32, Margin = new Padding(0, 10, 0, 8) };
        save.Click += OnSaveRegistration;
        flow.Controls.Add(save);

        var hint = new Label
        {
            Text = "Fields marked * are required. Password must match the confirmation.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 8, 0, 12)
        };
        flow.Controls.Add(hint);

        flow.Controls.Add(regStatus);

        panel.Controls.Add(flow);
        return panel;
    }

    private static void AddField(FlowLayoutPanel flow, string labelText, Control control)
    {
        var wrapper = new Panel { Width = 420, Height = 66, Margin = new Padding(0, 0, 0, 4) };
        var label = new Label { Text = labelText, AutoSize = true, Location = new Point(0, 0) };
        control.Location = new Point(0, 23);
        control.Width = 320;
        wrapper.Controls.Add(label);
        wrapper.Controls.Add(control);
        flow.Controls.Add(wrapper);
    }

    private Panel BuildOrdersPanel()
    {
        var panel = new Panel { Name = "OrdersPanel", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));

        layout.Controls.Add(new Label
        {
            Text = "Orders",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = true
        }, 0, 0);

        var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        searchPanel.Controls.Add(new Label { Text = "Search customer:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
        searchPanel.Controls.Add(searchBox);
        var search = new Button { Name = "SearchButton", Text = "Search", Width = 90 };
        search.Click += OnSearch;
        searchPanel.Controls.Add(search);
        var reset = new Button { Name = "ResetOrdersButton", Text = "Reset", Width = 90 };
        reset.Click += (_, _) => LoadOrders();
        searchPanel.Controls.Add(reset);
        layout.Controls.Add(searchPanel, 0, 1);

        ordersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer" });
        ordersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Amount",
            HeaderText = "Amount",
            ValueType = typeof(string)
        });
        ordersGrid.Columns[0].FillWeight = 65;
        ordersGrid.Columns[1].FillWeight = 35;
        layout.Controls.Add(ordersGrid, 0, 2);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var discount = new Button { Name = "ApplyDiscountButton", Text = "Apply 10% discount", Width = 180, Height = 32 };
        discount.Click += OnApplyDiscount;
        var delete = new Button { Name = "DeleteButton", Text = "Delete selected", Width = 140, Height = 32 };
        delete.Click += OnDelete;
        actionPanel.Controls.Add(discount);
        actionPanel.Controls.Add(delete);
        bottom.Controls.Add(actionPanel, 0, 0);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill };
        footer.Controls.Add(totalLabel);
        footer.Controls.Add(new Label
        {
            Text = "Amounts are stored as text in this training build (see defect D-06).",
            ForeColor = Color.DimGray,
            AutoSize = true,
            Margin = new Padding(24, 6, 0, 0)
        });
        bottom.Controls.Add(footer, 0, 1);

        layout.Controls.Add(bottom, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private Panel BuildNotesPanel()
    {
        var panel = new Panel { Name = "NotesPanel", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        layout.Controls.Add(new Label
        {
            Text = "Notes",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            AutoSize = true
        }, 0, 0);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var save = new Button { Name = "SaveNoteButton", Text = "Save note", Width = 120 };
        save.Click += OnSaveNote;
        var clear = new Button { Name = "ClearButton", Text = "Clear", Width = 90 };
        clear.Click += OnClearNotes;
        bottom.Controls.Add(save);
        bottom.Controls.Add(clear);
        bottom.Controls.Add(noteStatus);
        bottom.Controls.Add(charCount);

        notesBox.TextChanged += (_, _) => UpdateCharCount();
        layout.Controls.Add(notesBox, 0, 1);
        layout.Controls.Add(bottom, 0, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private void OnSaveRegistration(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firstNameBox.Text) || string.IsNullOrWhiteSpace(lastNameBox.Text))
            {
                SetRegStatus("Error: required fields are missing", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(emailBox.Text))
            {
                SetRegStatus("Error: e-mail is required", false);
                return;
            }

            if (!Rules.IsValidEmail(emailBox.Text))
            {
                SetRegStatus("Error: e-mail format is invalid", false);
                return;
            }

            // Seeded defect D-11: a raw exception is shown to the user when age is not numeric.
            var age = int.Parse(ageBox.Text);

            // Seeded defect D-02: age range is never validated here.
            if (passwordBox.Text != confirmBox.Text)
            {
                // Seeded defect D-09: generic message instead of a field-level hint.
                SetRegStatus("Error", false);
                return;
            }

            // Seeded defect D-13: password strength is not checked on save.
            _ = age;
            SetRegStatus("Saved successful", true);
        }
        catch (Exception ex)
        {
            SetRegStatus(ex.ToString(), false);
        }
    }

    private void SetRegStatus(string message, bool ok)
    {
        regStatus.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
        regStatus.Text = message;
    }

    private void LoadOrders()
    {
        ordersGrid.Rows.Clear();
        foreach (var order in SeedOrders)
        {
            ordersGrid.Rows.Add(order.Customer, order.Amount.ToString("0.00", CultureInfo.InvariantCulture));
        }

        searchBox.Text = string.Empty;
        if (ordersGrid.Rows.Count > 0)
        {
            ordersGrid.Rows[0].Selected = true;
        }

        UpdateTotal();
    }

    private void UpdateTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in ordersGrid.Rows)
        {
            if (decimal.TryParse(Convert.ToString(row.Cells[1].Value, CultureInfo.InvariantCulture),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                total += amount;
            }
        }

        _displayedTotal = total;
        totalLabel.Text = "Total: $" + total.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void OnApplyDiscount(object? sender, EventArgs e)
    {
        // Seeded defect D-03: every click applies the discount again.
        _displayedTotal = Rules.ApplyDiscount(_displayedTotal, 10);
        totalLabel.Text = "Total: $" + _displayedTotal.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        // Seeded defect D-04: no confirmation before deleting a row.
        // Seeded defect D-05: the total is not recalculated after deletion.
        if (ordersGrid.SelectedRows.Count == 0)
        {
            return;
        }

        ordersGrid.Rows.RemoveAt(ordersGrid.SelectedRows[0].Index);
    }

    private void OnSearch(object? sender, EventArgs e)
    {
        var query = searchBox.Text.Trim();
        ordersGrid.Rows.Clear();

        foreach (var order in SeedOrders)
        {
            // Seeded defect D-07: search is case-sensitive.
            if (query.Length == 0 || order.Customer.Contains(query, StringComparison.Ordinal))
            {
                ordersGrid.Rows.Add(order.Customer, order.Amount.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }

        // Seeded defect D-05b: the total keeps the previous value after filtering.
    }

    private void OnSaveNote(object? sender, EventArgs e)
    {
        var path = Path.Combine(Path.GetTempPath(), "qa-sandbox-note.txt");
        File.WriteAllText(path, Rules.TruncateNote(notesBox.Text));
        noteStatus.ForeColor = Color.SeaGreen;
        // Seeded defect D-10: grammar/typo in the user-facing message.
        noteStatus.Text = "Saved successful";
    }

    private void OnClearNotes(object? sender, EventArgs e)
    {
        // Seeded defect D-12: clear is immediate, without confirmation or undo.
        notesBox.Clear();
        noteStatus.ForeColor = Color.DimGray;
        noteStatus.Text = "Cleared";
    }

    private void UpdateCharCount()
    {
        charCount.ForeColor = notesBox.TextLength > Rules.NoteLimit ? Color.Firebrick : Color.DimGray;
        charCount.Text = $"{notesBox.TextLength} characters";
    }
}
