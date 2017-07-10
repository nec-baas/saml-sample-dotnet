using System;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Nec.Nebula;

namespace ADFSAuthNativeAppTP
{
    /// <summary>
    /// Top.xaml の相互作用ロジック
    /// </summary>
    public partial class Top : Page
    {
        const string EndpointUrl = "";
        const string TenantId = "";
        const string AppId = "";
        const string AppKey = "";
        const string MasterKey = "";
        HttpListener listener;
        string onetimeToken = "";
        bool isProcessing = false;

        public Top()
        {
            InitializeComponent();
        }

        // 認証開始
        private void btn_startAuth(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("--- Start Authentication ---");

            if (isProcessing){
                return ;
            }

            string redirectUri = "http://localhost:8080/receiveToken/";
            string encodeUri = System.Web.HttpUtility.UrlEncode(redirectUri);

            // ローカルサーバ起動
            // HTTPリスナー生成
            if (listener == null)
            {
                listener = new HttpListener();
                // リスナー各種設定
                listener.Prefixes.Clear();
                listener.Prefixes.Add(redirectUri);
            }
            else if (listener.IsListening)
            {
                Console.WriteLine("HttpListener is Listening yet");
                return;
            }

            // リスナー開始
            listener.Start();
            Console.WriteLine("HttpListener Open for " + redirectUri);
            isProcessing = true;

            // ブラウザ起動でREST API実行
            System.Diagnostics.Process.Start(EndpointUrl + "/1/" + TenantId + "/auth/saml/init" +
                "?redirect=" + encodeUri);

            // リクエスト待ち
            HttpListenerContext context = listener.GetContext();
            // リクエスト取得
            HttpListenerRequest req = context.Request;

            // レスポンス取得
            HttpListenerResponse res = context.Response;

            // リクエスト文字列取得
            string reqStr = req.RawUrl;

            // クエリパラメータ取得
            string tmpToken = req.QueryString.Get("token");
            if (tmpToken != null)
            {
                onetimeToken = tmpToken;
            }
            else
            {
                Console.WriteLine("Can not get new Token");
            }

            // レスポンスは200固定
            res.StatusCode = 200;

            // レスポンスのクローズ
            res.Close();

            // リスナー終了
            listener.Stop();
            isProcessing = false;
            Console.WriteLine("HttpListener Close");
            Console.WriteLine("get OnetimeToken = " + onetimeToken);

        }

        // ログイン
        private void btn_login(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine("--- Start Login ---");

            if (onetimeToken.Equals(""))
            {
                Console.WriteLine("login : no onetimetoken");
                return;
            }

            
            // 証明書チェックCallback
            ServicePointManager.ServerCertificateValidationCallback =
              new RemoteCertificateValidationCallback(OnRemoteCertificateValidationCallback);

            login(onetimeToken);

        }

        // 自己署名証明書回避
        private bool OnRemoteCertificateValidationCallback(
          Object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        // ログイン操作部
        private async void login(String token)
        {
            // NbService の生成
            var service = NbService.GetInstance();
            // テナントID
            service.TenantId = TenantId;
            // アプリケーションID
            service.AppId = AppId;
            // アプリケーションキー
            service.AppKey = AppKey;
            // エンドポイントURI
            service.EndpointUrl = EndpointUrl;

            var param = new NbUser.LoginParam() { Token = token };

            try
            {
                var ret = await NbUser.LoginAsync(param);
                Console.WriteLine("UserID = " + ret.UserId);
                Console.WriteLine("Email = " + ret.Email);
                Console.WriteLine("UserName = " + ret.Username);
                Console.WriteLine("CreatedAt = " + ret.CreatedAt);
                Console.WriteLine("UpdatedAt = " + ret.UpdatedAt);
                Console.WriteLine("Options = " + ret.Options.ToString());
                Console.WriteLine("Groups = " + "[" + String.Join(", ",ret.Groups) + "]");
            }
            catch(Exception ex)
            {
                Console.WriteLine("login : login error = " + ex.ToString());
            }
        }
    }
}
