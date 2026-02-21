using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TodoApp.DAL;
using TodoApp.Models;

namespace TodoApp
{
    /// <summary>タスク追加・編集ページのコードビハインド。</summary>
    public partial class AddEditTaskPage : Page
    {
        private readonly TaskRepository    _taskRepo    = new TaskRepository();
        private readonly CommentRepository _commentRepo = new CommentRepository();

        /// <summary>編集対象のタスク ID（新規作成時は null）。</summary>
        private int? TaskId
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["id"], out id) ? id : (int?)null;
            }
        }

        /// <summary>ページロード時にフォームを初期化する。</summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (TaskId.HasValue)
                {
                    litPageTitle.Text = "タスク編集";
                    LoadTask(TaskId.Value);
                    LoadComments(TaskId.Value);
                    pnlComments.Visible = true;
                }
                else
                {
                    litPageTitle.Text = "新規タスク追加";
                }
            }
        }

        // -------------------------------------------------------
        // イベントハンドラ
        // -------------------------------------------------------

        /// <summary>保存ボタンクリック時にタスクを登録または更新する。</summary>
        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            var task = BuildTaskFromForm();

            if (TaskId.HasValue)
            {
                task.Id = TaskId.Value;
                _taskRepo.Update(task);
                ShowMessage("タスクを更新しました。", false);
                LoadComments(TaskId.Value);
            }
            else
            {
                int newId = _taskRepo.Insert(task);
                Response.Redirect("AddEditTask.aspx?id=" + newId, false);
            }
        }

        /// <summary>コメント追加ボタンクリック時にコメントを登録する。</summary>
        protected void btnAddComment_Click(object sender, EventArgs e)
        {
            string content = txtComment.Text.Trim();
            if (string.IsNullOrEmpty(content) || !TaskId.HasValue)
                return;

            _commentRepo.Add(new TaskComment
            {
                TaskId  = TaskId.Value,
                Content = content
            });

            txtComment.Text = string.Empty;
            LoadComments(TaskId.Value);
        }

        /// <summary>コメント一覧の行コマンド（削除）を処理する。</summary>
        protected void rptComments_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "DeleteComment" || !TaskId.HasValue)
                return;

            int commentId = Convert.ToInt32(e.CommandArgument);
            _commentRepo.Delete(commentId);
            LoadComments(TaskId.Value);
        }

        // -------------------------------------------------------
        // プライベートヘルパー
        // -------------------------------------------------------

        private void LoadTask(int taskId)
        {
            TodoTask task = _taskRepo.GetById(taskId);
            if (task == null)
            {
                Response.Redirect("Default.aspx", false);
                return;
            }

            txtTitle.Text            = task.Title;
            txtDescription.Text      = task.Description;
            ddlPriority.SelectedValue = ((int)task.Priority).ToString();
            chkIsCompleted.Checked   = task.IsCompleted;
            txtDueDate.Text          = task.DueDate.HasValue
                ? task.DueDate.Value.ToString("yyyy-MM-dd") : string.Empty;
            txtNotificationDate.Text = task.NotificationDate.HasValue
                ? task.NotificationDate.Value.ToString("yyyy-MM-ddTHH:mm") : string.Empty;
        }

        private void LoadComments(int taskId)
        {
            rptComments.DataSource = _commentRepo.GetByTaskId(taskId);
            rptComments.DataBind();
        }

        private TodoTask BuildTaskFromForm()
        {
            DateTime? dueDate = null;
            if (!string.IsNullOrEmpty(txtDueDate.Text))
            {
                DateTime parsedDue;
                if (DateTime.TryParse(txtDueDate.Text, out parsedDue))
                    dueDate = parsedDue;
            }

            DateTime? notificationDate = null;
            if (!string.IsNullOrEmpty(txtNotificationDate.Text))
            {
                DateTime parsedNotif;
                if (DateTime.TryParse(txtNotificationDate.Text, out parsedNotif))
                    notificationDate = parsedNotif;
            }

            return new TodoTask
            {
                Title            = txtTitle.Text.Trim(),
                Description      = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                Priority         = (TaskPriority)int.Parse(ddlPriority.SelectedValue),
                IsCompleted      = chkIsCompleted.Checked,
                DueDate          = dueDate,
                NotificationDate = notificationDate
            };
        }

        private void ShowMessage(string text, bool isError)
        {
            lblMessage.Text     = HttpUtility.HtmlEncode(text);
            lblMessage.CssClass = isError ? "message-error" : "message-success";
            lblMessage.Visible  = true;
        }
    }
}
