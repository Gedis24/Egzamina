using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void buttonRegister_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text;
            string password = textBoxPassword.Text;
            string hashedPassword = PasswordHelper.HashPassword(password);
            UserStore.AddUser(username, hashedPassword);
            MessageBox.Show("User registered successfully!");
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text;
            string password = textBoxLoginPassword.Text;
            bool isAuthenticated = Authentication.ValidateLogin(username, password);

            MessageBox.Show(isAuthenticated ? "Login successful!" : "Login failed!");
        } 
        public static class Authentication
        {
            public static bool ValidateLogin(string username, string password)
            {
                string storedHashedPassword = UserStore.GetHashedPassword(username);

                if (storedHashedPassword == null)
                {
                    return false; // User not found
                }

                string hashedInputPassword = PasswordHelper.HashPassword(password);

                return storedHashedPassword == hashedInputPassword;
            }
        }
        public static class PasswordHelper
        {
            private static readonly string FixedSalt = "Ykdcr";

            public static string HashPassword(string password)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] saltedPassword = Encoding.UTF8.GetBytes(FixedSalt + password);
                    byte[] hashBytes = sha256.ComputeHash(saltedPassword);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }
        public static class UserStore
        {
            private static Dictionary<string, string> users = new Dictionary<string, string>();

            public static void AddUser(string username, string hashedPassword)
            {
                users[username] = hashedPassword;
            }

            public static string GetHashedPassword(string username)
            {
                return users.ContainsKey(username) ? users[username] : null;
            }
        }

        
        private async void buttonBruteForce_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text;
            string storedHashedPassword = UserStore.GetHashedPassword(username);
            if (storedHashedPassword != null)
            {
                string crackedPassword = await Task.Run(() => BruteForceAttack.PerformBruteForce(storedHashedPassword, 4, 4)); // maxLength and numThreads can be adjusted
                MessageBox.Show(crackedPassword != null ? $"Password cracked: {crackedPassword}" : "Password not found.");
            }
            else
            {
                MessageBox.Show("User not found.");
            }
        }
        public static class BruteForceAttack
        {
            public static string PerformBruteForce(string hashedPassword, int maxLength, int numThreads)
            {
                string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

                List<Task<string>> tasks = new List<Task<string>>();

                for (int i = 0; i < numThreads; i++)
                {
                    tasks.Add(Task.Run(() => BruteForceWorker(hashedPassword, chars, queue, maxLength)));
                }

                // Seed the queue with initial values
                foreach (char c in chars)
                {
                    queue.Enqueue(c.ToString());
                }

                Task.WaitAll(tasks.ToArray());

                foreach (var task in tasks)
                {
                    if (task.Result != null)
                    {
                        return task.Result;
                    }
                }

                return null;
            }

            private static string BruteForceWorker(string hashedPassword, string chars, ConcurrentQueue<string> queue, int maxLength)
            {
                while (queue.TryDequeue(out string current))
                {
                    if (current.Length >= maxLength)
                    {
                        continue;
                    }

                    string currentHashedPassword = PasswordHelper.HashPassword(current);

                    if (currentHashedPassword == hashedPassword)
                    {
                        return current;
                    }

                    foreach (char c in chars)
                    {
                        queue.Enqueue(current + c);
                    }
                }
                return null;
            }
        }

        
    }
}