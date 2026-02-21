using System.Configuration;
using System.Data.SQLite;

namespace TodoApp.DAL
{
    /// <summary>データベース接続を提供するヘルパークラス。</summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Web.config に定義された接続文字列を使用して <see cref="SQLiteConnection"/> を生成して返す。
        /// 呼び出し側で using ブロックを用いて確実にクローズすること。
        /// </summary>
        /// <returns>新しい <see cref="SQLiteConnection"/> インスタンス（未オープン）。</returns>
        public static SQLiteConnection CreateConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["TodoDbConnection"].ConnectionString;
            return new SQLiteConnection(connectionString);
        }
    }
}
