using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace vMail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SMTP smtp;
        private POP3 pop;
        private List<Heading> abstractEmails;

        public MainWindow()
        {
            InitializeComponent();
            smtp = new SMTP();
            smtp.StatusUpdateEvent += UpdateStatusBar;

            pop = new POP3();
            abstractEmails = new List<Heading>
            {
                new Heading(),
            };

            AbstractListBox.ItemsSource = abstractEmails;
        }

        private string DecodeBase64String(byte[] buff)
        {
            string base64String = Encoding.Default.GetString(buff);
            byte[] temp = Convert.FromBase64String(base64String);
            string utf8String = Encoding.UTF8.GetString(temp) + "\n";
            return utf8String;
        }

        private void SendBtnClick(object sender, RoutedEventArgs e)
        {
            if (FromTextBox.Text != null & ToTextBox.Text != null & KeyTextBox.Text != null & DataTextBox.Text != null)
            {
                // TODO: 检查mail合法性

                smtp.Init("smtp.qq.com", 25);                       // 初始化并登录邮箱
                smtp.Login(FromTextBox.Text, KeyTextBox.Text);

                Email email = new Email();          // 创建一封邮件
                Heading head = new Heading()        // 创建信头
                {
                    From = FromTextBox.Text,
                    To = ToTextBox.Text,
                    Subject = SubjectTextBox.Text == null ? "Unknown" : SubjectTextBox.Text,     // TODO: 提取部分Data作为subject
                };
                Body body = new Body()              // 创建信体
                {
                    ContentType = Content_Type.Text_Plain,
                    Data = Encoding.Default.GetBytes(DataTextBox.Text)
                };
                email.Head = head;
                email.Content = body;

                smtp.SendEmail(email);          // 发送邮件
                smtp.Close();
            }
            else
            {
                // 显示错误
            }
            
        }

        private void FetchBtnClick(object sender, RoutedEventArgs e)
        {
            // TODO 检查邮箱合法性
            // TODO 根据邮箱地址获取服务器名

            pop.Init("pop.qq.com", 110);
            pop.Login(POPUserTextBox.Text, POPKeyTextBox.Text);

            int count = pop.Stat().Item1;          // 邮件数量
            /*if (count > 25)                         // 做个简单的限制，TODO
            {
                count = 25;
            }*/

            abstractEmails.Clear();

            for (int i = 1; i <= count; i++)
            {
                Heading abstractEmail = pop.GetAbstracts(i);
                if (abstractEmail == null)          // 返回null说明该邮件已被删除
                {
                    continue;
                }
                abstractEmails.Add(abstractEmail.Trim());
                AbstractListBox.ItemsSource = null;             // 刷新数据
                AbstractListBox.ItemsSource = abstractEmails;
            }

            pop.Close();        // 关闭连接
        }

        public void UpdateStatusBar(int status)
        {
            if (StatusTextBlock.Text == string.Empty)
            {
                StatusTextBlock.Text = "";
            }

            StatusTextBlock.Text += " ";
            StatusTextBlock.Text += status.ToString();
        }

        private void DeleteBtnClick(object sender, RoutedEventArgs e)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < abstractEmails.Count; i++)
            {
                if (abstractEmails[i].IsSelected)
                {
                    indexes.Add(abstractEmails[i].Id);
                }
            }

            if (!pop.Connected)
            {
                // TODO 检查邮箱合法性
                // TODO 根据邮箱地址获取服务器名

                pop.Init("pop.qq.com", 110);
                pop.Login(POPUserTextBox.Text, POPKeyTextBox.Text);
            }

            pop.Delete(indexes);                // 删除服务器中的邮件

            foreach (int index in indexes)      // 删除显示的邮件
            {
                for(int i = 0; i < abstractEmails.Count; i++)
                {
                    if (abstractEmails[i].Id == index)
                    {
                        abstractEmails.RemoveAt(i);
                        i--;
                    }
                }
            }
            AbstractListBox.ItemsSource = null; // 更新数据
            AbstractListBox.ItemsSource = abstractEmails;

            pop.Close();
        }

        private void TestBtnClick(object sender, RoutedEventArgs e)
        {
            /* 获取邮件 */
            pop.Init("pop.qq.com", 110);
            pop.Login(POPUserTextBox.Text, POPKeyTextBox.Text);

            Email email = pop.GetEmail(Convert.ToInt32(TestTextBox.Text));
            if (email == null)          // 返回null说明该邮件已被删除
            {
                return;
            }

            pop.Close();        // 关闭连接

            /* 填充界面 */
            mFromTextBox.Text = email.Head.From;
            mToTextBox.Text = email.Head.To;
            mSubjectTextBox.Text = email.Head.Subject;

            mDateLabel.Content = email.Content.DataStr;

            /*if (email. == Content_Type.Multi_Alter || email.MajorType == Content_Type.Multi_Mixed)
            {
                mDataTextBox.Text = "";
                foreach (Body part in email.SubParts)
                {
                    mDataTextBox.Text += (part.Data + "\n");
                }
            }
            else
            {
                mDataTextBox.Text = email.Data;
            }*/
        }
    }
}
