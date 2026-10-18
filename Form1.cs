using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Calculator
{
    public partial class Form1 : Form
    {
        private Label lblMode;
        private TextBox txtExpression;
        private TextBox txtDisplay;
        private ComboBox cmbMode;
        private Panel memoryPanel;
        private TableLayoutPanel buttonsPanel;

        // Pending calculation, stored as operands and operators waiting to be applied.
        // Operators are applied by precedence: power, then x / mod, then + -.
        private readonly List<double> pendingValues = new List<double>();
        private readonly List<string> pendingOperators = new List<string>();

        private double memoryValue = 0;
        private bool newEntry = true;
        private bool operatorJustPressed = false;


        public Form1()
        {
            InitializeComponent();
            BuildDynamicGui();
            BuildStandardButtons();
            UpdateDisplay("0");
        }
        private void BuildDynamicGui()
        {
            Controls.Clear();

            this.Text = "Calculator";
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Font = new Font("Segoe UI", 10F);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.Padding = new Padding(10);
            root.BackColor = this.BackColor;

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;

            lblMode = new Label();
            lblMode.Text = "Standard";
            lblMode.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblMode.AutoSize = true;
            lblMode.Location = new Point(5, 8);

            cmbMode = new ComboBox();
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Items.Add("Standard");
            cmbMode.Items.Add("Scientific");
            cmbMode.SelectedIndex = 0;
            cmbMode.Width = 140;
            cmbMode.Location = new Point(240, 10);
            cmbMode.SelectedIndexChanged += CmbMode_SelectedIndexChanged;

            topPanel.Controls.Add(lblMode);
            topPanel.Controls.Add(cmbMode);

            txtExpression = new TextBox();
            txtExpression.Dock = DockStyle.Fill;
            txtExpression.ReadOnly = true;
            txtExpression.BorderStyle = BorderStyle.None;
            txtExpression.BackColor = this.BackColor;
            txtExpression.TextAlign = HorizontalAlignment.Right;
            txtExpression.Font = new Font("Segoe UI", 11F);

            txtDisplay = new TextBox();
            txtDisplay.Dock = DockStyle.Fill;
            txtDisplay.ReadOnly = true;
            txtDisplay.Multiline = true;
            txtDisplay.TextAlign = HorizontalAlignment.Right;
            txtDisplay.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            txtDisplay.BackColor = Color.White;
            txtDisplay.BorderStyle = BorderStyle.FixedSingle;

            memoryPanel = new Panel();
            memoryPanel.Dock = DockStyle.Fill;
            BuildMemoryButtons();

            buttonsPanel = new TableLayoutPanel();
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.BackColor = this.BackColor;

            root.Controls.Add(topPanel, 0, 0);
            root.Controls.Add(txtExpression, 0, 1);
            root.Controls.Add(txtDisplay, 0, 2);
            root.Controls.Add(memoryPanel, 0, 3);
            root.Controls.Add(buttonsPanel, 0, 4);

            Controls.Add(root);
        }

        private void BuildMemoryButtons()
        {
            memoryPanel.Controls.Clear();

            string[] memoryButtons = { "MC", "MR", "M+", "M-", "MS" };
            int x = 0;

            foreach (string text in memoryButtons)
            {
                Button btn = CreateButton(text);
                btn.Size = new Size(70, 32);
                btn.Location = new Point(x, 5);
                btn.Click += MemoryButton_Click;
                memoryPanel.Controls.Add(btn);
                x += 75;
            }
        }

        private void CmbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbMode.SelectedIndex == 0)
            {
                lblMode.Text = "Standard";
                BuildStandardButtons();
            }
            else
            {
                lblMode.Text = "Scientific";
                BuildScientificButtons();
            }

            ClearAll();
        }

        private void BuildStandardButtons()
        {
            buttonsPanel.Controls.Clear();
            buttonsPanel.ColumnStyles.Clear();
            buttonsPanel.RowStyles.Clear();

            buttonsPanel.ColumnCount = 4;
            buttonsPanel.RowCount = 6;

            for (int i = 0; i < 4; i++)
                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            for (int i = 0; i < 6; i++)
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66F));

            string[,] labels =
            {
                { "%", "CE", "C", "⌫" },
                { "1/x", "x²", "√x", "÷" },
                { "7", "8", "9", "×" },
                { "4", "5", "6", "-" },
                { "1", "2", "3", "+" },
                { "+/-", "0", ".", "=" }
            };

            AddButtons(labels);
        }

        private void BuildScientificButtons()
        {
            buttonsPanel.Controls.Clear();
            buttonsPanel.ColumnStyles.Clear();
            buttonsPanel.RowStyles.Clear();

            buttonsPanel.ColumnCount = 5;
            buttonsPanel.RowCount = 7;

            for (int i = 0; i < 5; i++)
                buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            for (int i = 0; i < 7; i++)
                buttonsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28F));

            string[,] labels =
            {
                { "π", "e", "C", "⌫", "mod" },
                { "x²", "1/x", "|x|", "exp", "n!" },
                { "√x", "sin", "cos", "tan", "÷" },
                { "xʸ", "7", "8", "9", "×" },
                { "10ˣ", "4", "5", "6", "-" },
                { "log", "1", "2", "3", "+" },
                { "ln", "+/-", "0", ".", "=" }
            };

            AddButtons(labels);
        }

        private void AddButtons(string[,] labels)
        {
            for (int r = 0; r < labels.GetLength(0); r++)
            {
                for (int c = 0; c < labels.GetLength(1); c++)
                {
                    Button btn = CreateButton(labels[r, c]);
                    btn.Dock = DockStyle.Fill;
                    btn.Margin = new Padding(3);
                    btn.Click += CalculatorButton_Click;
                    buttonsPanel.Controls.Add(btn, c, r);
                }
            }
        }

        private Button CreateButton(string text)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.White;
            btn.Font = new Font("Segoe UI", 12F);
            return btn;
        }

        private void CalculatorButton_Click(object sender, EventArgs e)
        {
            string key = ((Button)sender).Text;

            bool previousWasOperator = operatorJustPressed;
            operatorJustPressed = false;

            if (key.Length == 1 && char.IsDigit(key[0]))
            {
                AppendDigit(key);
                return;
            }

            switch (key)
            {
                case ".":
                    AppendDecimal();
                    break;
                case "+/-":
                    ToggleSign();
                    break;
                case "C":
                    ClearAll();
                    break;
                case "CE":
                    ClearEntry();
                    break;
                case "⌫":
                    Backspace();
                    break;
                case "+":
                case "-":
                case "×":
                case "÷":
                case "mod":
                case "xʸ":
                    SetOperator(key, previousWasOperator);
                    break;
                case "=":
                    CalculateResult();
                    break;
                case "%":
                    Percent();
                    break;
                case "1/x":
                    Reciprocal();
                    break;
                case "x²":
                    Square();
                    break;
                case "√x":
                    Sqrt();
                    break;
                case "|x|":
                    Abs();
                    break;
                case "exp":
                    Exp();
                    break;
                case "10ˣ":
                    TenPowerX();
                    break;
                case "log":
                    Log10();
                    break;
                case "ln":
                    Ln();
                    break;
                case "n!":
                    Factorial();
                    break;
                case "π":
                    SetConstant(Math.PI);
                    break;
                case "e":
                    SetConstant(Math.E);
                    break;
                case "sin":
                    Sin();
                    break;
                case "cos":
                    Cos();
                    break;
                case "tan":
                    Tan();
                    break;
            }
        }

        private void MemoryButton_Click(object sender, EventArgs e)
        {
            string key = ((Button)sender).Text;
            double value = ParseDisplay();
            operatorJustPressed = false;

            switch (key)
            {
                case "MC":
                    memoryValue = 0;
                    break;
                case "MR":
                    UpdateDisplay(FormatNumber(memoryValue));
                    newEntry = true;
                    break;
                case "MS":
                    memoryValue = value;
                    break;
                case "M+":
                    memoryValue += value;
                    break;
                case "M-":
                    memoryValue -= value;
                    break;
            }
        }

        private void AppendDigit(string digit)
        {
            if (txtDisplay.Text == "0" || newEntry)
            {
                txtDisplay.Text = digit;
                newEntry = false;
            }
            else
            {
                txtDisplay.Text += digit;
            }
        }

        private void AppendDecimal()
        {
            if (newEntry)
            {
                txtDisplay.Text = "0.";
                newEntry = false;
                return;
            }

            if (!txtDisplay.Text.Contains("."))
                txtDisplay.Text += ".";
        }

        private void ToggleSign()
        {
            double value = ParseDisplay();
            UpdateDisplay(FormatNumber(value == 0 ? 0 : -value));
        }

        private void ClearAll()
        {
            pendingValues.Clear();
            pendingOperators.Clear();
            operatorJustPressed = false;
            txtExpression.Text = "";
            UpdateDisplay("0");
            newEntry = true;
        }

        private void ClearEntry()
        {
            UpdateDisplay("0");
            newEntry = true;
        }

        private void Backspace()
        {
            if (newEntry)
                return;

            if (txtDisplay.Text.Length > 1)
                txtDisplay.Text = txtDisplay.Text.Substring(0, txtDisplay.Text.Length - 1);
            else
                txtDisplay.Text = "0";
        }

        private void SetOperator(string op, bool replaceLast)
        {
            // Pressing a second operator right after the first one replaces it.
            if (replaceLast && pendingOperators.Count > 0)
            {
                pendingOperators[pendingOperators.Count - 1] = op;
                UpdateExpressionText();
                operatorJustPressed = true;
                return;
            }

            pendingValues.Add(ParseDisplay());

            // Apply earlier operators that bind at least as tightly as the new one.
            while (pendingOperators.Count > 0 &&
                   AppliesBefore(pendingOperators[pendingOperators.Count - 1], op))
            {
                if (!ApplyTopOperator())
                    return;
            }

            pendingOperators.Add(op);
            UpdateExpressionText();
            newEntry = true;
            operatorJustPressed = true;
        }

        private void CalculateResult()
        {
            if (pendingOperators.Count == 0)
                return;

            pendingValues.Add(ParseDisplay());

            while (pendingOperators.Count > 0)
            {
                if (!ApplyTopOperator())
                    return;
            }

            double result = pendingValues[0];
            pendingValues.Clear();

            UpdateDisplay(FormatNumber(result));
            txtExpression.Text = "";
            newEntry = true;
        }

        private static int Precedence(string op)
        {
            switch (op)
            {
                case "xʸ":
                    return 3;
                case "×":
                case "÷":
                case "mod":
                    return 2;
                default:
                    return 1;
            }
        }

        // True if the operator already waiting must be applied before the new one.
        // Power is right-associative (2^3^2 = 2^9), all other operators are left-associative.
        private static bool AppliesBefore(string waiting, string incoming)
        {
            int waitingLevel = Precedence(waiting);
            int incomingLevel = Precedence(incoming);

            if (waitingLevel != incomingLevel)
                return waitingLevel > incomingLevel;

            return incoming != "xʸ";
        }

        // Applies the most recent pending operator to the two most recent operands.
        // Returns false (after showing "Error") if the operation is not possible.
        private bool ApplyTopOperator()
        {
            string op = pendingOperators[pendingOperators.Count - 1];
            double right = pendingValues[pendingValues.Count - 1];
            double left = pendingValues[pendingValues.Count - 2];

            pendingOperators.RemoveAt(pendingOperators.Count - 1);
            pendingValues.RemoveRange(pendingValues.Count - 2, 2);

            double result;

            switch (op)
            {
                case "+":
                    result = left + right;
                    break;
                case "-":
                    result = left - right;
                    break;
                case "×":
                    result = left * right;
                    break;
                case "÷":
                    if (right == 0)
                    {
                        ShowError();
                        return false;
                    }
                    result = left / right;
                    break;
                case "mod":
                    if (right == 0)
                    {
                        ShowError();
                        return false;
                    }
                    result = left % right;
                    break;
                case "xʸ":
                    result = Math.Pow(left, right);
                    break;
                default:
                    result = right;
                    break;
            }

            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                ShowError();
                return false;
            }

            pendingValues.Add(result);
            UpdateDisplay(FormatNumber(result));
            return true;
        }

        private void UpdateExpressionText()
        {
            string text = "";

            for (int i = 0; i < pendingOperators.Count; i++)
                text += FormatNumber(pendingValues[i]) + " " + pendingOperators[i] + " ";

            txtExpression.Text = text.Trim();
        }

        private void Percent()
        {
            double value = ParseDisplay();
            double baseValue = pendingValues.Count > 0 ? pendingValues[pendingValues.Count - 1] : 0;
            double result = baseValue * value / 100.0;
            UpdateDisplay(FormatNumber(result));
        }

        private void Reciprocal()
        {
            double value = ParseDisplay();
            if (value == 0)
            {
                ShowError();
                return;
            }

            UpdateDisplay(FormatNumber(1 / value));
            newEntry = true;
        }

        private void Square()
        {
            double value = ParseDisplay();
            UpdateDisplay(FormatNumber(value * value));
            newEntry = true;
        }

        private void Sqrt()
        {
            double value = ParseDisplay();
            if (value < 0)
            {
                ShowError();
                return;
            }

            UpdateDisplay(FormatNumber(Math.Sqrt(value)));
            newEntry = true;
        }

        private void Abs()
        {
            double value = ParseDisplay();
            UpdateDisplay(FormatNumber(Math.Abs(value)));
            newEntry = true;
        }

        private void Exp()
        {
            double value = ParseDisplay();
            UpdateDisplay(FormatNumber(Math.Exp(value)));
            newEntry = true;
        }

        private void TenPowerX()
        {
            double value = ParseDisplay();
            UpdateDisplay(FormatNumber(Math.Pow(10, value)));
            newEntry = true;
        }

        private void Log10()
        {
            double value = ParseDisplay();
            if (value <= 0)
            {
                ShowError();
                return;
            }

            UpdateDisplay(FormatNumber(Math.Log10(value)));
            newEntry = true;
        }

        private void Ln()
        {
            double value = ParseDisplay();
            if (value <= 0)
            {
                ShowError();
                return;
            }

            UpdateDisplay(FormatNumber(Math.Log(value)));
            newEntry = true;
        }

        private void Factorial()
        {
            double value = ParseDisplay();

            if (value < 0 || value != Math.Floor(value) || value > 170)
            {
                ShowError();
                return;
            }

            double result = 1;
            for (int i = 2; i <= (int)value; i++)
                result *= i;

            UpdateDisplay(FormatNumber(result));
            newEntry = true;
        }

        private void Sin()
        {
            double radians = ParseDisplay() * Math.PI / 180.0;
            UpdateDisplay(FormatNumber(CleanResult(Math.Sin(radians))));
            newEntry = true;
        }

        private void Cos()
        {
            double radians = ParseDisplay() * Math.PI / 180.0;
            UpdateDisplay(FormatNumber(CleanResult(Math.Cos(radians))));
            newEntry = true;
        }

        private void Tan()
        {
            double radians = ParseDisplay() * Math.PI / 180.0;

            // tan is undefined where cos is (numerically) zero, e.g. 90 degrees.
            if (Math.Abs(Math.Cos(radians)) < 1e-12)
            {
                ShowError();
                return;
            }

            UpdateDisplay(FormatNumber(CleanResult(Math.Tan(radians))));
            newEntry = true;
        }

        // Removes floating-point noise such as sin(180 deg) = 1.2E-16 and avoids "-0".
        private static double CleanResult(double value)
        {
            value = Math.Round(value, 12);
            return value == 0 ? 0 : value;
        }

        private void SetConstant(double value)
        {
            UpdateDisplay(FormatNumber(value));
            newEntry = true;
        }

        private void ShowError()
        {
            txtDisplay.Text = "Error";
            pendingValues.Clear();
            pendingOperators.Clear();
            txtExpression.Text = "";
            operatorJustPressed = false;
            newEntry = true;
        }

        private double ParseDisplay()
        {
            double value;
            if (double.TryParse(txtDisplay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return value;

            return 0;
        }

        private void UpdateDisplay(string text)
        {
            txtDisplay.Text = text;
        }

        private string FormatNumber(double value)
        {
            return value.ToString("G15", CultureInfo.InvariantCulture);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}