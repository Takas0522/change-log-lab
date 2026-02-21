<%@ Page Title="タスク一覧" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TodoApp.DefaultPage" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">タスク一覧</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="page-header">
        <h2>タスク一覧</h2>
        <a href="AddEditTask.aspx" class="btn">&#65291; 新規タスク追加</a>
    </div>

    <%-- フィルター・検索・ソートバー --%>
    <div class="filter-bar">
        <div class="filter-group">
            <label for="<%: txtSearch.ClientID %>">検索:</label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" placeholder="タイトル・説明で検索" MaxLength="100" />
            <asp:Button ID="btnSearch" runat="server" Text="検索" OnClick="btnSearch_Click" CssClass="btn" />
            <asp:Button ID="btnReset"  runat="server" Text="リセット" OnClick="btnReset_Click" CssClass="btn btn-secondary" />
        </div>
        <div class="filter-group">
            <label for="<%: ddlStatus.ClientID %>">ステータス:</label>
            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-control"
                AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                <asp:ListItem Text="すべて"   Value="-1" />
                <asp:ListItem Text="未完了"   Value="0"  />
                <asp:ListItem Text="完了済み" Value="1"  />
            </asp:DropDownList>
        </div>
        <div class="filter-group">
            <label for="<%: ddlPriority.ClientID %>">優先度:</label>
            <asp:DropDownList ID="ddlPriority" runat="server" CssClass="form-control"
                AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                <asp:ListItem Text="すべて" Value="-1" />
                <asp:ListItem Text="低"     Value="0"  />
                <asp:ListItem Text="中"     Value="1"  />
                <asp:ListItem Text="高"     Value="2"  />
            </asp:DropDownList>
        </div>
        <div class="filter-group">
            <label for="<%: ddlSort.ClientID %>">並び替え:</label>
            <asp:DropDownList ID="ddlSort" runat="server" CssClass="form-control"
                AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                <asp:ListItem Text="作成日時" Value="CreatedAt" />
                <asp:ListItem Text="期限"     Value="DueDate"   />
                <asp:ListItem Text="優先度"   Value="Priority"  />
                <asp:ListItem Text="タイトル" Value="Title"     />
            </asp:DropDownList>
        </div>
        <div class="filter-group">
            <asp:CheckBox ID="chkSortDesc" runat="server" Text="降順"
                AutoPostBack="true" OnCheckedChanged="ddlFilter_Changed" />
        </div>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false" />

    <%-- タスク一覧グリッド --%>
    <asp:GridView ID="gvTasks" runat="server"
        AutoGenerateColumns="false"
        CssClass="task-grid"
        DataKeyNames="Id"
        EmptyDataText="タスクが見つかりません。"
        OnRowCommand="gvTasks_RowCommand">
        <Columns>
            <asp:TemplateField HeaderText="タイトル">
                <ItemTemplate>
                    <a href='AddEditTask.aspx?id=<%# Eval("Id") %>'><%# HtmlEncode(Eval("Title")) %></a>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="優先度">
                <ItemTemplate>
                    <span class='<%# GetPriorityClass(Eval("Priority")) %>'><%# GetPriorityLabel(Eval("Priority")) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="期限">
                <ItemTemplate>
                    <%# FormatDate(Eval("DueDate")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="通知日時">
                <ItemTemplate>
                    <%# FormatDate(Eval("NotificationDate")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="ステータス">
                <ItemTemplate>
                    <span class='<%# (bool)Eval("IsCompleted") ? "status-completed" : "status-pending" %>'>
                        <%# (bool)Eval("IsCompleted") ? "完了" : "未完了" %>
                    </span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="操作">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Toggle"
                        CommandArgument='<%# Eval("Id") %>'
                        CssClass="btn btn-sm"
                        Text='<%# (bool)Eval("IsCompleted") ? "未完了に戻す" : "完了にする" %>' />
                    <a href='AddEditTask.aspx?id=<%# Eval("Id") %>' class="btn btn-sm">編集</a>
                    <asp:LinkButton runat="server"
                        CommandName="Delete"
                        CommandArgument='<%# Eval("Id") %>'
                        CssClass="btn btn-sm btn-danger"
                        Text="削除"
                        OnClientClick="return confirm('このタスクを削除してもよいですか？');" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

</asp:Content>
