<%@ Page Title="タスク追加・編集" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AddEditTask.aspx.cs" Inherits="TodoApp.AddEditTaskPage" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">タスク追加・編集</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-header">
        <h2><asp:Literal ID="litPageTitle" runat="server" /></h2>
        <a href="Default.aspx" class="btn btn-secondary">&#8592; 一覧に戻る</a>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false" />

    <%-- タスク入力フォーム --%>
    <div class="form-card">
        <div class="form-row">
            <label for="<%: txtTitle.ClientID %>">タイトル <span style="color:red">*</span></label>
            <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" MaxLength="200"
                placeholder="タスクのタイトルを入力" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtTitle"
                ErrorMessage="タイトルは必須です。" Display="Dynamic" ForeColor="Red" />
        </div>
        <div class="form-row">
            <label for="<%: txtDescription.ClientID %>">説明</label>
            <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control"
                TextMode="MultiLine" Rows="4" placeholder="タスクの説明を入力（任意）" />
        </div>
        <div class="form-row">
            <label for="<%: ddlPriority.ClientID %>">優先度</label>
            <asp:DropDownList ID="ddlPriority" runat="server" CssClass="form-control">
                <asp:ListItem Text="低" Value="0" />
                <asp:ListItem Text="中" Value="1" Selected="True" />
                <asp:ListItem Text="高" Value="2" />
            </asp:DropDownList>
        </div>
        <div class="form-row">
            <label for="<%: txtDueDate.ClientID %>">期限</label>
            <asp:TextBox ID="txtDueDate" runat="server" CssClass="form-control"
                TextMode="Date" />
        </div>
        <div class="form-row">
            <label for="<%: txtNotificationDate.ClientID %>">通知日時</label>
            <asp:TextBox ID="txtNotificationDate" runat="server" CssClass="form-control"
                TextMode="DateTimeLocal" />
            <small style="color:#666">期限前に通知を受け取る日時を設定します（任意）。</small>
        </div>
        <div class="form-row">
            <label>
                <asp:CheckBox ID="chkIsCompleted" runat="server" />
                完了済みとしてマーク
            </label>
        </div>
        <div class="form-actions">
            <asp:Button ID="btnSave" runat="server" Text="保存" OnClick="btnSave_Click" CssClass="btn" />
            <a href="Default.aspx" class="btn btn-secondary">キャンセル</a>
        </div>
    </div>

    <%-- コメントセクション（編集時のみ表示） --%>
    <asp:Panel ID="pnlComments" runat="server" Visible="false" CssClass="comment-section">
        <h3>&#128172; コメント</h3>

        <asp:Repeater ID="rptComments" runat="server" OnItemCommand="rptComments_ItemCommand">
            <HeaderTemplate><ul class="comment-list"></HeaderTemplate>
            <ItemTemplate>
                <li class="comment-item">
                    <div><%# HttpUtility.HtmlEncode(Eval("Content").ToString()) %></div>
                    <div class="comment-meta">
                        <%# Eval("CreatedAt") is DateTime dt ? dt.ToString("yyyy/MM/dd HH:mm") : "" %>
                        <asp:LinkButton runat="server"
                            CommandName="DeleteComment"
                            CommandArgument='<%# Eval("Id") %>'
                            CssClass="btn btn-sm btn-danger"
                            Text="削除"
                            OnClientClick="return confirm('このコメントを削除してもよいですか？');" />
                    </div>
                </li>
            </ItemTemplate>
            <FooterTemplate></ul></FooterTemplate>
        </asp:Repeater>

        <div class="comment-add">
            <asp:TextBox ID="txtComment" runat="server" CssClass="form-control"
                TextMode="MultiLine" Rows="2" placeholder="コメントを入力してください" MaxLength="2000" />
            <asp:Button ID="btnAddComment" runat="server" Text="追加" OnClick="btnAddComment_Click" CssClass="btn btn-success" />
        </div>
    </asp:Panel>

</asp:Content>
