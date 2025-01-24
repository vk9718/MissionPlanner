using System;
using System.Windows.Forms;

namespace ActivationKeyManagement
{
    public class ActivationKeyUser
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public string SystemName { get; set; }
        public string SystemId { get; set; }
        public string OrganisationName { get; set; }
        public string UsedFor { get; set; }
        public string ActivationKey { get; set; }
        public long ValidDays { get; set; }
        public bool? ActiveStatus { get; set; }
    }

    public class ActivationKeyGeneratorApp : Form
    {
        public ActivationKeyGeneratorApp()
        {
            var btnGenerateKey = new Button
            {
                Text = "Request New Activation Key",
                Location = new System.Drawing.Point(50, 50),
                Size = new System.Drawing.Size(200, 40)
            };
            btnGenerateKey.Click += (s, e) =>
            {
                using (var registrationForm = new ActivationKeyRegistrationForm())
                {
                    if (registrationForm.ShowDialog() == DialogResult.OK)
                    {
                        using (var activationForm = new KeyActivationForm(registrationForm.ActivationKey.ActivationKey))
                        {
                            activationForm.ShowDialog();
                        }
                    }
                }
            };

            var btnValidateKey = new Button
            {
                Text = "Activate Software Key",
                Location = new System.Drawing.Point(50, 100),
                Size = new System.Drawing.Size(200, 40)
            };
            btnValidateKey.Click += (s, e) =>
            {
                using (var activationForm = new KeyActivationForm())
                {
                    activationForm.ShowDialog();
                }
            };

            Controls.AddRange(new Control[] { btnGenerateKey, btnValidateKey });
            Text = "Activation Key Management";
            Size = new System.Drawing.Size(300, 250);
            StartPosition = FormStartPosition.CenterScreen;

            // Add these lines to handle closing the app
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            FormClosing += (s, e) => Application.Exit();
        }
    }

    public class ActivationKeyRegistrationForm : Form
    {
        public ActivationKeyUser ActivationKey { get; private set; }

        public ActivationKeyRegistrationForm()
        {
            var lblUsername = new Label
            {
                Text = "Username",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 23)
            };
            var txtUsername = new TextBox
            {
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(200, 23)
            };

            var lblOrgName = new Label
            {
                Text = "Organisation Name",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(100, 23)
            };
            var txtOrgName = new TextBox
            {
                Location = new System.Drawing.Point(130, 60),
                Size = new System.Drawing.Size(200, 23)
            };

            var lblValidDays = new Label
            {
                Text = "Valid Days",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(100, 23)
            };
            var nudValidDays = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(200, 23),
                Minimum = 1,
                Maximum = 365
            };

            var btnSubmit = new Button
            {
                Text = "Send Request",
                Location = new System.Drawing.Point(100, 140),
                Size = new System.Drawing.Size(150, 30)
            };
            btnSubmit.Click += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                    string.IsNullOrWhiteSpace(txtOrgName.Text))
                {
                    MessageBox.Show("Username and Organisation Name are required.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                ActivationKey = new ActivationKeyUser
                {
                    SystemName = txtUsername.Text,
                    OrganisationName = txtOrgName.Text,
                    ValidDays = (long)nudValidDays.Value,
                    ActivationKey = Guid.NewGuid().ToString(),
                    ActiveStatus = true
                };

                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                lblUsername, txtUsername,
                lblOrgName, txtOrgName,
                lblValidDays, nudValidDays,
                btnSubmit
            });

            Text = "Request New Activation key";
            Size = new System.Drawing.Size(370, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
        }
    }

    public class KeyActivationForm : Form
    {
        private string _generatedKey;

        public KeyActivationForm(string prefilledKey = null)
        {
            _generatedKey = prefilledKey;

            var lblActivationKey = new Label
            {
                Text = "Enter Activation Key",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(150, 23)
            };
            var txtActivationKey = new TextBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(300, 23),
                Text = _generatedKey ?? string.Empty
            };

            var btnValidateKey = new Button
            {
                Text = "Activate",
                Location = new System.Drawing.Point(50, 100),
                Size = new System.Drawing.Size(100, 30)
            };
            btnValidateKey.Click += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtActivationKey.Text))
                {
                    MessageBox.Show("Please enter an activation key.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show("Key Validation Successful!",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            Controls.AddRange(new Control[]
            {
                lblActivationKey,
                txtActivationKey,
                btnValidateKey,
                //btnRequestNewKey
            });

            Text = "Activate software";
            Size = new System.Drawing.Size(350, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
        }
    }

    
}