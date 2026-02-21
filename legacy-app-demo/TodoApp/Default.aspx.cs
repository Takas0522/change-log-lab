using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TodoApp.DAL;

namespace TodoApp
{
    /// <summary>タスク一覧ページのコードビハインド。</summary>
    public partial class DefaultPage : Page
    {
        private readonly TaskRepository _taskRepo = new TaskRepository();

        /// <summary>ページロード時にタスク一覧を読み込む。</summary>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindTasks();
        }

        // -------------------------------------------------------
        // イベントハンドラ
        // -------------------------------------------------------

        /// <summary>検索ボタンクリック時にタスク一覧を再読み込みする。</summary>
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindTasks();
        }

        /// <summary>リセットボタンクリック時に検索条件をクリアしてタスク一覧を再読み込みする。</summary>
        protected void btnReset_Click(object sender, EventArgs e)
        {
            txtSearch.Text       = string.Empty;
            ddlStatus.SelectedValue   = "-1";
            ddlPriority.SelectedValue = "-1";
            ddlSort.SelectedValue     = "CreatedAt";
            chkSortDesc.Checked       = false;
            BindTasks();
        }

        /// <summary>フィルター・ソートのドロップダウン変更時にタスク一覧を再読み込みする。</summary>
        protected void ddlFilter_Changed(object sender, EventArgs e)
        {
            BindTasks();
        }

        /// <summary>グリッドの行コマンド（完了切替・削除）を処理する。</summary>
        protected void gvTasks_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int taskId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "Toggle")
            {
                _taskRepo.ToggleComplete(taskId);
                ShowMessage("タスクのステータスを更新しました。", false);
            }
            else if (e.CommandName == "Delete")
            {
                _taskRepo.Delete(taskId);
                ShowMessage("タスクを削除しました。", false);
            }

            BindTasks();
        }

        // -------------------------------------------------------
        // テンプレートバインディングヘルパー（protected で aspx から呼び出し可）
        // -------------------------------------------------------

        /// <summary>優先度の表示ラベルを返す。</summary>
        protected string GetPriorityLabel(object priority)
        {
            switch (Convert.ToInt32(priority))
            {
                case 0:  return "低";
                case 1:  return "中";
                case 2:  return "高";
                default: return "-";
            }
        }

        /// <summary>優先度に対応する CSS クラス名を返す。</summary>
        protected string GetPriorityClass(object priority)
        {
            switch (Convert.ToInt32(priority))
            {
                case 0:  return "priority-low";
                case 1:  return "priority-medium";
                case 2:  return "priority-high";
                default: return "";
            }
        }

        /// <summary>日付を "yyyy/MM/dd" 形式の文字列に変換する。null の場合は "-" を返す。</summary>
        protected string FormatDate(object date)
        {
            if (date == null || date == DBNull.Value)
                return "-";
            if (date is DateTime dt)
                return dt.ToString("yyyy/MM/dd");
            return "-";
        }

        /// <summary>XSS 対策のため文字列を HTML エンコードして返す。</summary>
        protected string HtmlEncode(object value)
        {
            return HttpUtility.HtmlEncode(value?.ToString() ?? string.Empty);
        }

        // -------------------------------------------------------
        // プライベートヘルパー
        // -------------------------------------------------------

        private void BindTasks()
        {
            int? statusFilter   = ddlStatus.SelectedValue   == "-1" ? (int?)null : int.Parse(ddlStatus.SelectedValue);
            int? priorityFilter = ddlPriority.SelectedValue == "-1" ? (int?)null : int.Parse(ddlPriority.SelectedValue);

            gvTasks.DataSource = _taskRepo.GetAll(
                searchTerm:     txtSearch.Text.Trim(),
                statusFilter:   statusFilter,
                priorityFilter: priorityFilter,
                sortBy:         ddlSort.SelectedValue,
                sortDescending: chkSortDesc.Checked);
            gvTasks.DataBind();
        }

        private void ShowMessage(string text, bool isError)
        {
            lblMessage.Text    = HttpUtility.HtmlEncode(text);
            lblMessage.CssClass = isError ? "message-error" : "message-success";
            lblMessage.Visible  = true;
        }
    }
}
