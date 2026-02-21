<%@ Page Title="エラー" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="TodoApp.ErrorPage" %>

<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">エラー</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="error-container">
        <h2>&#9888; エラーが発生しました</h2>
        <p>予期しないエラーが発生しました。しばらくしてから再度お試しください。</p>
        <a href="<%: ResolveUrl("~/") %>" class="btn">&#8592; タスク一覧に戻る</a>
    </div>
</asp:Content>
