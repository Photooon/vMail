using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace vMail
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        /* 默认构造函数 */
        public LoginWindow()
        {
            InitializeComponent();
        }

        /* 登录按钮点击 */
        private void LoginBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            /* User, Pwd, MailServer */
            mainWindow.user = LoginUserTextBox.Text;
            mainWindow.pwd = LoginPwdTextBox.Text;
            Match match = Regex.Match(LoginUserTextBox.Text, ".*@([^.]*).*");
            string server = match.Groups[1].Value;
            mainWindow.smtpServerAddr = String.Format("smtp.{0}.com", server);
            mainWindow.popServerAddr = String.Format("pop.{0}.com", server);
            //this.Hide();
            mainWindow.Show();
        }
    }
}
