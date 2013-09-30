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
    /// To connect to evernote, you call the consturctor, and then BuildAuthorizeUrl
    /// which will return a URL that you need to redirect the user to. Then once
    /// they come back to your site, call ParseAccessToken with the oauth_validator
    /// 
    /// This will then populate your EvernoteCredentials object
    /// </summary>
    /// <remarks>
    /// This class simplifies the calls to AsyncOAuthAuthorizer so that you don't
    /// need to worry about async calls. Use this to get started, and then if your
    /// controller needs to do any other processing, you can switch to using 
    /// AsyncEvernoteAuthorizer.
    /// 
    /// It wraps all calls to AsyncEverynoteAuthorizer in a Task.Run so that the MVC
    /// controller doesn't return until the Evernote Servers have been talked to. This
    /// is not ideal in a large environment, so you should use AsyncEvernoteAuthorizer
    /// in that situation. More details can be found here:
    /// http://www.asp.net/mvc/tutorials/mvc-4/using-asynchronous-methods-in-aspnet-mvc-4
    /// </remarks>
    public class EvernoteAuthorizer
    {
        /// <summary>
        /// Used to make the calls the evernote service
        /// </summary>
        public AsyncEvernoteAuthorizer AsyncEvernoteAuthorizer { get; set; }

        /// <summary>
        /// This object is a Syncrhonous wrapper around the AsyncEvernoteAuthorizer,
        /// which is turn simplifies the interface to AsyncOAuth when talking to Evernote,
        /// taking care of some of configuration parameters / parsing you normally have
        /// to deal with when working with the OAuth directly. 
        /// 
        /// It will wait until we have heard back from Evernote before finishing the request
        /// Use AsyncEvernoteAuthorizer if you want to remove this dependency
        /// </summary>
        /// <param name="evernoteUrl">The url of the Evernote Service to connection to e.g. https://sandbox.evernote.com for development</param>
        /// <param name="evernoteKey">Customer Key - what you provided evernote when creating API account e.g. "myappname"</param>
        /// <param name="evernoteSecret">Consumer Secret - what you evernote provided you e.g. "badb123b192192"</param>
        /// <param name="initComputeHash">By default OAuthUtility.ComputeHash is not populated. If you have set this elsewhere in your program, then set this to false</param>
        public EvernoteAuthorizer(string evernoteUrl, string evernoteKey, string evernoteSecret, bool initComputeHash = true)
        {
            AsyncEvernoteAuthorizer = new AsyncEvernoteAuthorizer(evernoteUrl, evernoteKey, evernoteSecret, initComputeHash);
        }

        /// <summary>
        /// Creates a web request to evernote to retrieve a request token to pass along
        /// when we redirect the user to the evernote site. You need to keep this 
        /// </summary>
        /// <param name="callbackUri">Where we plan on parsing the token returned to us from Evernote</param>
        /// <returns>A RequestToken which is needed for the BuildAuthorizeUrl and ParseAccessToken methods (will need to be persisted)</returns>
        public RequestToken GetRequestToken(string callbackUri) 
        {
            // This will wait until we hear back from Evernote, rather than finishing this request (which an await would do)
            // For the proper
            return Task.Run(() => AsyncEvernoteAuthorizer.GetRequestToken(callbackUri)).Result;
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
            return AsyncEvernoteAuthorizer.BuildAuthorizeUrl(token);
        }
        
        /// <summary>
        /// This method should be called once you have received the verifier from 
        /// Evernote. It will populate a EvernoteCredentials object with all the 
        /// information you need to authenticate to Evernote as this user
        /// </summary>
        /// <remarks>
        /// This is an asynchronous method
        /// </remarks>
        /// <param name="oauth_verifier">The verifier passed in via the QueryString to your endpoint</param>
        /// <param name="token">The token used to request the authorization - this should be persisted from the call to GetRequestToken</param>
        /// <returns></returns>
        public EvernoteCredentials ParseAccessToken(string oauth_verifier, RequestToken token)
        {
            return Task.Run(() => AsyncEvernoteAuthorizer.ParseAccessToken(oauth_verifier, token)).Result;
        }

    }
}