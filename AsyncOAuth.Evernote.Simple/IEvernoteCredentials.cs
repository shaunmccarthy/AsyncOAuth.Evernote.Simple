using AsyncOAuth;
using System;
namespace AsyncOAuth.Evernote.Simple
{
    /// <summary>
    /// Used to wrap all the default data that is returned from Evernote
    /// once you are authorized to a user's account
    /// </summary>
    public class EvernoteCredentials
    {
        /// <summary>
        /// The token to use when accessing the user's notebooks / notes
        /// </summary>
        public string AuthToken { get; set; }
        /// <summary>
        /// The baes (sharded) url to use when accessing the user's notebooks / notes
        /// </summary>
        public string NotebookUrl { get; set; }
        /// <summary>
        /// The ID of the Shard the user is on
        /// </summary>
        public string Shard { get; set; }
        /// <summary>
        /// The EvernoteID of the user
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The WebAPI URL Prefix for this user
        /// </summary>
        public string WebApiUrlPrefix { get; set; }
        /// <summary>
        /// When the credentials expire
        /// </summary>
        public DateTime Expires { get; set; }
    }
}
