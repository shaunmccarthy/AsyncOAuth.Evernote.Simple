using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AsyncOAuth.Evernote.Simple;
using AsyncOAuth.Evernote.Simple.SampleMVC.Models;

namespace AysncOAuth.Evernote.Simple.SampleMVC.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// The Simple Authorizer
        /// </summary>
        EvernoteAuthorizer EvernoteAuthorizer = new EvernoteAuthorizer(ConfigurationManager.AppSettings["Evernote.Url"], ConfigurationManager.AppSettings["Evernote.Key"], ConfigurationManager.AppSettings["Evernote.Secret"]);

        /// <summary>
        /// This method makes the original call to Evernote to get a token so
        /// that the user can validate that they want to access this site. 
        /// </summary>
        /// <param name="reauth"></param>
        /// <returns></returns>
        public ActionResult Authorize(bool reauth = false)
        {
            // Allow for reauth
            if (reauth)
                SessionHelper.Clear();

            // First of all, check to see if the user is already registered, in which case tell them that
            if (SessionHelper.EvernoteCredentials != null)
                return Redirect(Url.Action("AlreadyAuthorized"));

            // Evernote will redirect the user to this URL once they have authorized your application
            var callBackUrl = Request.Url.GetLeftPart(UriPartial.Authority) + Url.Action("ObtainTokenCredentials");

            // Generate a request token - this needs to be persisted till the callback
            var requestToken = EvernoteAuthorizer.GetRequestToken(callBackUrl);

            // Persist the token
            SessionHelper.RequestToken = requestToken;

            // Redirect the user to Evernote so they can authorize the app
            var callForwardUrl = EvernoteAuthorizer.BuildAuthorizeUrl(requestToken);
            return Redirect(callForwardUrl);
        }

        /// <summary>
        /// This action is the callback that Evernote will redirect to after 
        /// the call to Authorize above
        /// </summary>
        /// <param name="oauth_verifier"></param>
        /// <returns></returns>
        public ActionResult ObtainTokenCredentials(string oauth_verifier)
        {
            // Use the verifier to get all the user details we need and
            // store them in EvernoteCredentials
            var credentials = EvernoteAuthorizer.ParseAccessToken(oauth_verifier, SessionHelper.RequestToken);
            if (credentials != null)
            {
                SessionHelper.EvernoteCredentials = credentials;
                return Redirect(Url.Action("Authorized"));
            }
            else
            {
                return Redirect(Url.Action("Unauthorized"));
            }
        }

        /// <summary>
        /// Show the user if they are authorized
        /// </summary>
        /// <returns></returns>
        public ActionResult Authorized()
        {
            return View(SessionHelper.EvernoteCredentials);
        }

        public ActionResult Unauthorized()
        {
            return View();
        }

        /// <summary>
        /// A nice little page that the user ends up on if they have 
        /// already authorized the app. Here, we use it to dump out the 
        /// EvernoteCredentials object
        /// </summary>
        /// <returns></returns>
        public ActionResult AlreadyAuthorized()
        {
            return View(SessionHelper.EvernoteCredentials);
        }

        public ActionResult Index()
        {
            return View();
        }

    }
}
