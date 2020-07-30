using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using System.Text.RegularExpressions;

namespace vMail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const String saveFilePath = "C:\\Users\\15278\\Desktop\\";

        public string user;                     // 用户邮箱
        public string pwd;                      // 用户密钥
        public string smtpServerAddr;           // SMTP服务器地址
        public string popServerAddr;            // POP3服务器地址

        private SMTP smtp;                      // SMTP类实例
        private POP3 pop;                       // POP3类实例
        private List<Heading> headings;         // 收信箱摘要列表
        private Email selectedEmail;            // 所选中的邮件
        private List<Attachment> attachments;   // 待发送的附件

        /* 默认构造函数 */
        public MainWindow()
        {
            InitializeComponent();
            /* SMTP*/
            smtp = new SMTP();
            smtp.StatusUpdateEvent += UpdateStatusBar;

            /* POP3 */
            pop = new POP3();

            /* Heading */
            headings = new List<Heading>();

            HeadingsListBox.ItemsSource = headings;

            /* DetailTabItem */
            detailTabItem.IsEnabled = false;                // 初始不显示
            detailTabItem.Visibility = Visibility.Hidden;

            /* Other */
            attachments = new List<Attachment>();
        }

        /* 将邮件填入详情页 */
        private void FillDetailPage(Email email)
        {
            selectedEmail = email;

            /* 信头 */
            detailFromTextBox.Text = email.Head.From;
            detailToTextBox.Text = email.Head.To;
            detailSubjectTextBox.Text = email.Head.Subject;
            detailDateLabel.Content = email.Head.DateStr;

            /* 信体 */
            detailDataTextBox.Text = email.Content.Text;        // 文本部分
            List<Attachment> attachments = email.Content.Attachments;

            this.ReceiveAttachmentListBox.ItemsSource = null;          // 更新附件列表
            this.ReceiveAttachmentListBox.ItemsSource = attachments;
            
        }

        /* 发送邮件 */
        private void SendBtnClick(object sender, RoutedEventArgs e)
        {
            if (ToTextBox.Text != null & DataTextBox.Text != null)
            {
                smtp.Init(smtpServerAddr, 25);                       // 初始化并登录邮箱
                smtp.Login(user, pwd);

                Email email = new Email();          // 创建一封邮件

                /* 信头 */
                Heading head = new Heading()
                {
                    From = user,
                    To = ToTextBox.Text,
                    Subject = SubjectTextBox.Text == null ? "Unknown" : SubjectTextBox.Text,     // TODO: 提取部分Data作为subject
                };

                /* 信体 */
                Body body = new Body();

                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] b = utf8.GetBytes(MyEncoder.EncodeWithUTF8(DataTextBox.Text));
                Body textBody = new Body()
                {
                    ContentType = Content_Type.Text_Plain,
                    Data = b
                };

                if (attachments.Count != 0)
                {
                    body.ContentType = Content_Type.Multi_Mixed;
                    body.SubBodies.Add(textBody);
                    foreach (Attachment attachment in attachments)
                    {
                        Body attachBody = new Body()
                        {
                            ContentType = attachment.ContentType,
                            Name = attachment.Name,
                            Data = attachment.Data,
                        };
                        body.SubBodies.Add(attachBody);
                    }
                    attachments.Clear();
                    SendAttachmentListBox.ItemsSource = null;
                    SendAttachmentListBox.ItemsSource = attachments;
                }
                else
                {
                    body = textBody;
                }

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

        /* 获取所有摘要 */
        private void FetchBtnClick(object sender, RoutedEventArgs e)
        {
            pop.Init(popServerAddr, 110);
            pop.Login(user, pwd);

            int count = pop.Stat().Item1;               // 邮件数量
            /*if (count > 25)                           // 做个简单的限制
            {
                count = 25;
            }*/

            headings.Clear();
            for (int i = 1; i <= count; i++)
            {
                Heading heading = pop.GetHeading(i);
                if (heading == null)              // 返回null说明该邮件已被删除
                {
                    continue;
                }
                headings.Add(heading.Trim());
            }
            headings.Reverse();                        // 将时间近的放在前面
            HeadingsListBox.ItemsSource = null;                 // 刷新数据
            HeadingsListBox.ItemsSource = headings;

            pop.Close();        // 关闭连接
        }

        /* 批量删除邮件 */
        private void DeleteBtnClick(object sender, RoutedEventArgs e)
        {
            List<int> indexes = new List<int>();

            for (int i = 0; i < headings.Count; i++)
            {
                if (headings[i].IsSelected)
                {
                    indexes.Add(headings[i].Id);
                }
            }

            if (!pop.Connected)
            {
                pop.Init(popServerAddr, 110);
                pop.Login(user, pwd);
            }

            pop.Delete(indexes);                // 删除服务器中的邮件

            foreach (int index in indexes)      // 删除显示的邮件
            {
                for(int i = 0; i < headings.Count; i++)
                {
                    if (headings[i].Id == index)
                    {
                        headings.RemoveAt(i);
                        i--;
                    }
                }
            }
            HeadingsListBox.ItemsSource = null; // 更新数据
            HeadingsListBox.ItemsSource = headings;

            pop.Close();
        }

        /* 查看邮件详情 */
        private void ListBoxSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Heading heading = HeadingsListBox.SelectedItem as Heading;

                /* 获取邮件 */
                pop.Init(popServerAddr, 110);
                pop.Login(user, pwd);

                Email email = pop.GetEmail(Convert.ToInt32(heading.Id));       // 获取选中邮件的id
                if (email == null)          // 返回null说明该邮件已被删除
                {
                    return;
                }

                pop.Close();                // 关闭连接

                /* 加载详情页 */
                if (!this.detailTabItem.IsEnabled)          // 第一次选中邮件开启显示
                {
                    this.detailTabItem.IsEnabled = true;
                    this.detailTabItem.Visibility = Visibility.Visible;
                }

                FillDetailPage(email);                      // 填充详情页

                this.mainTabControl.SelectedIndex = 2;      // 跳转到详情页
            }
            catch (Exception)
            {
                
            }
        }

        /* 状态栏更新 */
        public void UpdateStatusBar(int status)
        {
            if (StatusTextBlock.Text == string.Empty)
            {
                StatusTextBlock.Text = "";
            }

            StatusTextBlock.Text += " ";
            StatusTextBlock.Text += status.ToString();
        }

        /* 下载附件 */
        private void DownloadMenuItemClick(object sender, RoutedEventArgs e)
        {
            Attachment attachment = ReceiveAttachmentListBox.SelectedItem as Attachment;

            FileStream fileStream = new FileStream(saveFilePath + attachment.Name, FileMode.Create);
            fileStream.Write(attachment.Data, 0, attachment.Data.Length);
            fileStream.Close();
        }

        /* 添加附件 */
        private void AddAttachmentBtnClick(object sender, RoutedEventArgs e)
        {
            /* 获取文件 */
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择文件";
            openFileDialog.Filter = "所有文件|*.*";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "*";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            string fileName = openFileDialog.FileName;

            /* 处理文件 */
            byte[] b = File.ReadAllBytes(fileName);
            string[] paths = Regex.Split(fileName, @"\\");
            string name = paths[paths.Length - 1];

            Attachment attachment = new Attachment();
            attachment.Name = name;
            attachment.Data = b;

            attachments.Add(attachment);

            SendAttachmentListBox.ItemsSource = null;
            SendAttachmentListBox.ItemsSource = attachments;
        }

        /* 删除待发送的附件 */
        private void SendAttachmentDeleteMenuItemClick(object sender, RoutedEventArgs e)
        {
            Attachment attachment = SendAttachmentListBox.SelectedItem as Attachment;

            attachments.Remove(attachment);
            SendAttachmentListBox.ItemsSource = null;
            SendAttachmentListBox.ItemsSource = attachments;
        }
    }
}
