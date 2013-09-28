using AsyncOAuth;
using AsyncOAuth.Evernote.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Security.Cryptography;

namespace AsyncOAuth.Evernote.Simple
{
    /// <summary>
    /// This class simplifies the calls to OAuthAuthorizer that are required
    /// to get access to a user's evernote information. 
    /// 
    /// To connect to evernote, you call the consturctor, and then BuildAuthorizeUrl
    /// which will return a URL that you need to redirect the user to. Then once
    /// they come back to your site, call ParseAccessToken with the oauth_validator
    /// 
    /// This will then populate your EvernoteCredentials object
    /// </summary>
    /// <remarks>
    /// The async calls are simplified to synchronous calls, and could do with 
    /// work to make them more MVC 4 compliant
    /// http://www.asp.net/mvc/tutorials/mvc-4/using-asynchronous-methods-in-aspnet-mvc-4
    /// </remarks>
    public class EvernoteAuthorizer : OAuthAuthorizer
    {
        /// <summary>
        /// The base URL of the Evernote API Server
        /// </summary>
        public string EvernoteUrl { get; set; }
        /// <summary>
        /// The OAuth url for evernote
        /// </summary>
        public string OAuthUrl { get { return EvernoteUrl + ("/oauth"); } }
        /// <summary>
        /// The OAuth.Action url for evernote
        /// </summary>
        public string OAuthActionUrl { get { return EvernoteUrl + "/OAuth.action"; } }

        /// <summary>
        /// Credentials that will be populated after the user authorizes the app
        /// </summary>
        public EvernoteCredentials EvernoteCredentials { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evernoteUrl">The url of the Evernote Service to connection to e.g. https://sandbox.evernote.com for development</param>
        /// <param name="evernoteKey">Customer Key - what you provided evernote when creating API account e.g. "myappname"</param>
        /// <param name="evernoteSecret">Consumer Secret - what you evernote provided you e.g. "badb123b192192"</param>
        /// <param name="initComputeHash">By default OAuthUtility.ComputeHash is not populated. If you have set this elsewhere in your program, then set this to false</param>
        public EvernoteAuthorizer(string evernoteUrl, string evernoteKey, string evernoteSecret, bool initComputeHash = true)
            : base(evernoteKey, evernoteSecret)
        {
            // Snure that the OAuthUtility ComputeHash function has been set - there's no "get" for this
            // so we have to set it each time - less than ideal. Feel free to set this in your 
            // global.asax once. This is just for ease of use.
            if (initComputeHash)
                OAuthUtility.ComputeHash = (key, buffer) => { using (var hmac = new HMACSHA1(key)) { return hmac.ComputeHash(buffer); } };

            EvernoteUrl = evernoteUrl;
        }

        /// <summary>
        /// Creates a web request to evernote to retrieve a request token to pass along
        /// when we redirect the user to the evernote site. You need to keep this 
        /// </summary>
        /// <param name="callbackUri">Where we plan on parsing the token returned to us from Evernote</param>
        /// <returns>A RequestToken which is needed for the BuildAuthorizeUrl and ParseAccessToken methods (will need to be persisted)</returns>
        public RequestToken GetRequestToken(string callbackUri) 
        {
            return base.GetRequestToken(OAuthUrl, new Dictionary<string, string> { { "oauth_callback", callbackUri } }, null).Result.Token;
        }

        /// <summary>
        /// Returns a URL that you can redirect the user to on the Evernote site that
        /// will prompt them to authroize your app. Once they do this, they will 
        /// be redirected to callbackUri with the oauth_validator parameter
        /// </summary>
        /// <param name="callbackUri">The end point you plan on using to call ParseAccessToken</param>
        /// <returns></returns>
        public string BuildAuthorizeUrl(RequestToken token)
        {
            // Use the existing token, or generate a new one
            var callForwardUrl = base.BuildAuthorizeUrl(OAuthActionUrl, token);

            // Store the token in the IEvernoteAuthorizer
            return callForwardUrl;
        }

        /// <summary>
        /// This method should be called once you have received the verifier from 
        /// Evernote. It will populate a EvernoteCredentials object with all the 
        /// information you need to authenticate to Evernote as this user
        /// </summary>
        /// <param name="oauth_verifier">The verifier passed in via the QueryString to your endpoint</param>
        /// <param name="token">The token used to request the authorization - this should be persisted from the call to GetRequestToken</param>
        /// <returns></returns>
        public EvernoteCredentials ParseAccessToken(string oauth_verifier, RequestToken token)
        {
            // If there is no oauth_verifier parameter, then we failed to authorize :(
            if (oauth_verifier == null)
                return null;

            if (token == null)
                throw new ArgumentNullException("token", "You need to pass in the original token that was generated by BuildAuthorizeUrl");

            var result = base.GetAccessToken(OAuthUrl, token, oauth_verifier, null, null).Result;

            // There is no extra secret for evernote tokens
            EvernoteCredentials credentials = new EvernoteCredentials();
            credentials.AuthToken = result.Token.Key;

            // Parse the extra data
            credentials.Shard = ParseExtraData(result, "edam_shard");
            credentials.UserId = ParseExtraData(result, "edam_userId");
            var expires = ParseExtraData(result, "edam_expires");
            var expiresDateTime = new DateTime(1970, 1, 1).AddTicks(long.Parse(expires) * 10000);
            credentials.Expires = DateTime.SpecifyKind(expiresDateTime, DateTimeKind.Utc);
            credentials.NotebookUrl = ParseExtraData(result, "edam_noteStoreUrl");
            credentials.WebApiUrlPrefix = ParseExtraData(result, "edam_webApiUrlPrefix");
            return credentials;
        }

        /// <summary>
        /// Parses the ExtraData collection
        /// </summary>
        private string ParseExtraData(TokenResponse<AccessToken> result, string key)
        {
            if (!result.ExtraData.Contains(key))
                return null;

            var data = result.ExtraData[key];

            if (data == null)
                return null;
            else
                return Uri.UnescapeDataString(data.FirstOrDefault());
        }
    }
}