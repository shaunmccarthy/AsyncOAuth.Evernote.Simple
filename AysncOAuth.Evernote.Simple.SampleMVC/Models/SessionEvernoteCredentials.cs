using AsyncOAuth;
using AsyncOAuth.Evernote.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AsyncOAuth.Evernote.Simple.SampleMVC.Models
{
    public static class SessionHelper
    {
        public static EvernoteCredentials EvernoteCredentials { get { return RetrieveFromSession<EvernoteCredentials>("Evernote.Credentials"); } set { StoreInSession("Evernote.Credentials", value); } }
        public static RequestToken RequestToken { get { return RetrieveFromSession<RequestToken>("Evernote.RequestToken"); } set { StoreInSession("Evernote.RequestToken", value); } }

        public static void StoreInSession<T>(string key, T value) where T : class
        {
            HttpContext.Current.Session[key] = value;
        }
        public static T RetrieveFromSession<T>(string key) where T : class
        {
            return HttpContext.Current.Session[key] as T;
        }

        public static void Clear()
        {
            HttpContext.Current.Session.Clear();
        }
    }

}