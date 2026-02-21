using System;
using System.Web;
using TodoApp.DAL;

namespace TodoApp
{
    /// <summary>ASP.NET アプリケーションのエントリポイント。</summary>
    public class Global : HttpApplication
    {
        /// <summary>アプリケーション起動時にデータベーススキーマを初期化する。</summary>
        protected void Application_Start(object sender, EventArgs e)
        {
            DatabaseInitializer.Initialize();
        }

        /// <summary>未処理例外をキャッチしてエラーログに記録する。</summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex == null)
                return;

            // 本番環境ではロギングフレームワーク（例: NLog, log4net）に差し替えること
            System.Diagnostics.Trace.TraceError(
                "[{0}] Unhandled exception: {1}", DateTime.Now, ex);

            Server.ClearError();
            Response.Redirect("~/Error.aspx", false);
        }
    }
}
